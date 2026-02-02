using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureForceTargetSystem : ISystem
    {
        private EntityQuery _query;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<CreatureForceTargetTag>();
            _query = state.GetEntityQuery(queryBuilder);
            queryBuilder.Dispose();
            
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
            if (_query.IsEmpty)
            {
                return;
            }
            
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            if (global.InPause)
            {
                return;
            }

            _deadLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _summonLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //吸引状态
            new ForceTargetTimerJob
            {
                Ecb = ecb.AsParallelWriter(),
                DeltaTime = deltaTime,
                DeadLookup = _deadLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                SummonLookup = _summonLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();
            

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        [BurstCompile]
        private partial struct ForceTargetTimerJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            
            [BurstCompile]
            private void Execute(RefRW<CreatureForceTargetTag> tag, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (DeadLookup.IsComponentEnabled(entity) || 
                    BuffHelper.GetHasBuff(entity, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.Invincible))
                {
                    Ecb.SetComponentEnabled<CreatureForceTargetTag>(sortKey, entity, false);
                    return;
                }

                var destroyTime = tag.ValueRO.DestroyDelay;
                if (tag.ValueRO.DestroyDelay > 0)
                {
                    tag.ValueRW.Timer += DeltaTime;
                    if (tag.ValueRO.Timer > destroyTime)
                    {
                        Ecb.SetComponentEnabled<CreatureForceTargetTag>(sortKey, entity, false);
                    }
                }
            }
        }
    }
}