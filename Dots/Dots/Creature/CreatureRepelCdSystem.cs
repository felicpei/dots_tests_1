using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureRepelCdSystem : ISystem
    {
        private EntityQuery _query;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<CreatureRepelCd>();
            _query = state.GetEntityQuery(queryBuilder);
            queryBuilder.Dispose();

            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
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

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            new RepelCdJob
            {
                DeltaTime = deltaTime, 
                DeadLookup = _deadLookup,
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct RepelCdJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;

            [BurstCompile]
            private void Execute(RefRW<CreatureRepelCd> hitCd, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (DeadLookup.IsComponentEnabled(entity))
                {
                    Ecb.SetComponentEnabled<CreatureRepelCd>(sortKey, entity, false);
                    return;
                }

                if (hitCd.ValueRO.Timer > 0)
                {
                    hitCd.ValueRW.Timer = hitCd.ValueRO.Timer - DeltaTime;
                }
                else
                {
                    Ecb.SetComponentEnabled<CreatureRepelCd>(sortKey, entity, false);
                }
            }
        }
    }
}