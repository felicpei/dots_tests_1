using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureFreezeSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<InBornTag> _inBornLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _inBornLookup = state.GetComponentLookup<InBornTag>(true);
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

            _deadLookup.Update(ref state);
            _inBornLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            //enter freeze
            foreach (var (tag, creatureFps, entity) in SystemAPI.Query<EnterFreezeTag, RefRW<CreatureFps>>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<EnterFreezeTag>(entity, false);

                if (_deadLookup.IsComponentEnabled(entity))
                {
                    continue;
                }

                ecb.SetComponentEnabled<InFreezeState>(entity, true);

                if (!_inBornLookup.IsComponentEnabled(entity))
                {
                    creatureFps.ValueRW.FpsFactorZero = true;
                }
            }

            //exit freeze
            foreach (var (tag, creatureFps, entity) in
                     SystemAPI.Query<InFreezeState, RefRW<CreatureFps>>().WithAll<RemoveFreezeTag>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<RemoveFreezeTag>(entity, false);
                ecb.SetComponentEnabled<InFreezeState>(entity, false);

                if (_deadLookup.IsComponentEnabled(entity))
                {
                    continue;
                }

                if (!_inBornLookup.IsComponentEnabled(entity))
                {
                    creatureFps.ValueRW.FpsFactorZero = false;
                }
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}