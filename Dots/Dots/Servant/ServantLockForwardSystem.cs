using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    [UpdateAfter(typeof(MonsterMoveSystem))]
    public partial struct ServantLockForwardSystem : ISystem
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
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (tag, entity) in SystemAPI.Query<RefRW<ServantLockForward>>().WithEntityAccess())
            {
                tag.ValueRW.ContTime -= deltaTime;  

                if (tag.ValueRW.ContTime <= 0)
                {
                    ecb.SetComponentEnabled<ServantLockForward>(entity, false);
                }
            }
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);  
            ecb.Dispose(); 
        } 
    }
}