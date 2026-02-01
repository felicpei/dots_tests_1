using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct MissionStartSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<MissionStartTag>();
            state.RequireForUpdate<CacheProperties>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<MissionStartTag>(out var initTag))
            {
                return;
            }

            //wait 1s
            if (initTag.ValueRO.Timer <= 0.5f)
            {
                initTag.ValueRW.Timer += SystemAPI.Time.DeltaTime;
                return;
            }
            
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.RemoveComponent<MissionStartTag>(global.Entity);
            
            //开始刷怪
            ecb.AddComponent<SpawnWaveInitTag>(global.Entity);
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}