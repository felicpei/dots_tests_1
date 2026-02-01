using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BuffSystemGroup))]
    public partial struct BuffRemoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
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

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            new BuffRemoveJob
            { 
                CurrTime = global.Time,
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct BuffRemoveJob : IJobEntity
        {
            public float CurrTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            [BurstCompile]
            private void Execute(RefRW<BuffTimeProperty> timeInfo, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                //移除到时间的buff
                if (timeInfo.ValueRO.ContTime > 0 && CurrTime - timeInfo.ValueRO.StartTime > timeInfo.ValueRO.ContTime)
                { 
                    timeInfo.ValueRW.ContTime = 0;
                    
                    Ecb.AppendToBuffer(sortKey, entity, new BuffUpdateBuffer(EBuffUpdate.Remove));
                    Ecb.SetComponentEnabled<BuffUpdateBuffer>(sortKey, entity, true);
                }
            }
        }
    }
}