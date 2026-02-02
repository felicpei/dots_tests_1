using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureAbsorbSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
                
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            if (global.InPause)
            {
                return;
            }

            _monsterLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _summonLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            
            //吸引状态
            new AbsorbJob
            {
                Ecb = ecb.AsParallelWriter(),
                GlobalEntity = global.Entity,
                DeltaTime = deltaTime,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                CacheLookup = _cacheLookup,
                CacheEntity = cacheEntity,
                DeadLookup = _deadLookup,
                MonsterLookup = _monsterLookup,
                SummonLookup = _summonLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct AbsorbJob : IJobEntity
        {
            public float DeltaTime;
            public Entity GlobalEntity;
            public Entity CacheEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<MonsterProperties> MonsterLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            
            [BurstCompile]
            private void Execute(RefRW<CreatureAbsorbTag> tag, RefRW<LocalTransform> local, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (DeadLookup.IsComponentEnabled(entity) || BuffHelper.GetHasBuff(entity, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.Invincible))
                {
                    Ecb.SetComponentEnabled<CreatureAbsorbTag>(sortKey, entity, false);
                    return;
                }

                var destroyTime = tag.ValueRO.DestroyDelay;
                if (tag.ValueRO.DestroyDelay > 0)
                {
                    tag.ValueRW.Timer += DeltaTime;
                    if (tag.ValueRO.Timer > destroyTime)
                    {
                        Ecb.SetComponentEnabled<CreatureAbsorbTag>(sortKey, entity, false);
                    }
                }

                //吸引处理，排除不能推动的怪物
                var bCanAbsorb = false;
                if (MonsterLookup.TryGetComponent(entity, out var monster))
                {
                    if (CacheHelper.GetMonsterConfig(monster.Id, CacheEntity, CacheLookup, out var monsterConfig))
                    {
                        bCanAbsorb = monsterConfig.CollisionType != ECollisionType.None;
                    }
                }

                if (bCanAbsorb)
                {
                    var forward = math.normalizesafe(tag.ValueRO.Target - local.ValueRO.Position);
                    local.ValueRW.Position += tag.ValueRO.Speed * forward * DeltaTime;
                }
            }
        }
    }
}