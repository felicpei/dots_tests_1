using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureInitSystemGroup))]
    public partial struct SpawnLocalPlayerSystem : ISystem
    {
        private EntityQuery _query;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            //state.RequireForUpdate<MapProperties>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            
            _query = state.GetEntityQuery(ComponentType.ReadOnly<LocalPlayerTag>());
            _transformLookup = state.GetComponentLookup<LocalTransform>();
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
                _transformLookup.Update(ref state);

                var ecb = new EntityCommandBuffer(Allocator.Temp);
                var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
                var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

                //创建本地玩家
                var localPlayerId = global.LocalPlayerId;
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                FactoryHelper.CreatePlayer(collisionWorld, localPlayerId, global, cache, ecb);
             
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }
    }
}