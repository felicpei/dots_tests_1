using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureDisableMoveSystem : ISystem
    {
        private EntityQuery _query;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<DisableMoveTag>();
            _query = state.GetEntityQuery(queryBuilder);
            queryBuilder.Dispose();
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


            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //禁止移动状态
            new DisableMoveTimeJob { Ecb = ecb.AsParallelWriter(), DeltaTime = deltaTime }.ScheduleParallel();
            state.Dependency.Complete();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct DisableMoveTimeJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            [BurstCompile]
            private void Execute(RefRW<DisableMoveTag> tag, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (tag.ValueRO.DestroyDelay > 0)
                {
                    tag.ValueRW.Timer += DeltaTime;
                    if (tag.ValueRO.Timer > tag.ValueRO.DestroyDelay)
                    {
                        Ecb.SetComponentEnabled<DisableMoveTag>(sortKey, entity, false);
                    }
                }
            }
        }
    }
}