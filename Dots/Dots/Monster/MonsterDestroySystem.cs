using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    [UpdateAfter(typeof(MonsterMoveSystem))]
    public partial struct MonsterDestroySystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillProperties> _skillLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillLookup = state.GetComponentLookup<SkillProperties>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
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

            _skillEntitiesLookup.Update(ref state);
            _skillLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //销毁前处理
            new MonsterDestroyJob
            {
                DeltaTime = deltaTime,
                GlobalEntity = global.Entity,
                Ecb = ecb.AsParallelWriter(),
                SkillEntitiesLookup = _skillEntitiesLookup,
                SkillLookup = _skillLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct MonsterDestroyJob : IJobEntity
        {
            public float DeltaTime;
            public Entity GlobalEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillProperties> SkillLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            
            [BurstCompile]
            private void Execute(RefRW<MonsterDestroyTag> tag, MonsterProperties _, InDeadState inDeadState, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                tag.ValueRW.Timer = tag.ValueRO.Timer + DeltaTime;

                if (tag.ValueRO.Timer >= tag.ValueRO.DestroyDelay)
                {
                    //remove all skill
                    SkillHelper.RemoveAllSkill(GlobalEntity, entity, SkillEntitiesLookup, SkillLookup, Ecb, sortKey);

                    //remove all buffs
                    BuffHelper.RemoveAllBuff(entity, BuffEntitiesLookup, BuffTagLookup, Ecb, sortKey);
                    
                    Ecb.SetComponentEnabled<MonsterDestroyTag>(sortKey, entity, false);
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });
                }
            }
        }
    }
}