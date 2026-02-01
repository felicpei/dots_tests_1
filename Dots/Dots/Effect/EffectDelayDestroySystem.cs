using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(EffectSystemGroup))]
    public partial struct EffectDelayDestroySystem : ISystem
    {
        private EntityQuery _query;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<EffectDelayDestroy>();
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
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            
            new EffectDelayDestroyJob
            {
                GlobalEntity = global.Entity,
                DeltaTime = deltaTime, 
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct EffectDelayDestroyJob : IJobEntity
        {
            public Entity GlobalEntity;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            
            [BurstCompile]
            private void Execute(RefRW<EffectDelayDestroy> delayDestroy,  Entity entity, [EntityIndexInQuery] int sortKey)
            {
                
                //时间判断
                if (delayDestroy.ValueRO.DelayDestroy > 0)
                {
                    delayDestroy.ValueRW.AliveTime = delayDestroy.ValueRO.AliveTime + DeltaTime;

                    if (delayDestroy.ValueRO.AliveTime >= delayDestroy.ValueRO.DelayDestroy)
                    {
                        Ecb.SetComponentEnabled<EffectDelayDestroy>(sortKey, entity, false);
                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });
                    }
                }
            }
        }
    }
}