using Dots;
using Dots;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSystemGroup))]
    public partial struct CreatureChildrenSyncSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<BuffTag> _buffLookup;
        [ReadOnly] private ComponentLookup<SkillProperties> _skillLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _buffLookup = state.GetComponentLookup<BuffTag>(true);
            _skillLookup = state.GetComponentLookup<SkillProperties>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _buffLookup.Update(ref state);
            _skillLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            new BuffEntitiesSyncJob
            {
                Ecb = ecb.AsParallelWriter(), 
                BuffPropertiesLookup = _buffLookup
            }.ScheduleParallel();
            state.Dependency.Complete(); 
            
            new SkillEntitiesSyncJob
            {
                Ecb = ecb.AsParallelWriter(), 
                SkillPropertiesLookup = _skillLookup
            }.ScheduleParallel();
            state.Dependency.Complete(); 

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

      
        [BurstCompile]
        private partial struct BuffEntitiesSyncJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<BuffTag> BuffPropertiesLookup;

            [BurstCompile]
            private void Execute(DynamicBuffer<BuffEntities> buffEntities, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                for (var i = buffEntities.Length - 1; i >= 0; i--)
                {
                    if (!BuffPropertiesLookup.HasComponent(buffEntities[i].Value))
                    {
                        buffEntities.RemoveAt(i);
                    }
                }

                if (buffEntities.Length <= 0)
                {
                    Ecb.SetComponentEnabled<BuffEntities>(sortKey, entity, false);
                }
            }
        }
        
        [BurstCompile]
        private partial struct SkillEntitiesSyncJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<SkillProperties> SkillPropertiesLookup;

            [BurstCompile]
            private void Execute(DynamicBuffer<SkillEntities> skillEntities, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                for (var i = skillEntities.Length - 1; i >= 0; i--)
                {
                    if (!SkillPropertiesLookup.HasComponent(skillEntities[i].Value))
                    {
                        skillEntities.RemoveAt(i);
                    }
                }

                if (skillEntities.Length <= 0)
                {
                    Ecb.SetComponentEnabled<SkillEntities>(sortKey, entity, false);
                }
            }
        }
    }
}