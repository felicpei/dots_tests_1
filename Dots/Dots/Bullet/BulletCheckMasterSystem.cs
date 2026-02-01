using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BulletSystemGroup))]
    public partial struct BulletCheckMasterSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
         
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>();
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
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

            _destroyLookup.Update(ref state);
            _creatureTag.Update(ref state);
            _transformLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            //检查MasterCreature是否还存在
            new CheckMasterCreatureJob 
            {
                Ecb = ecb.AsParallelWriter(), 
                TransformLookup = _transformLookup,
                DestroyLookup = _destroyLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct CheckMasterCreatureJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<BulletDestroyTag> DestroyLookup;
            
            [BurstCompile]
            private void Execute(BulletProperties properties, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (DestroyLookup.IsComponentEnabled(entity))
                {
                    return;
                }
                
                if (!TransformLookup.HasComponent(properties.MasterCreature))
                {
                    Ecb.SetComponentEnabled<BulletDestroyTag>(sortKey, entity, true);
                }
            }
        }
    }
}