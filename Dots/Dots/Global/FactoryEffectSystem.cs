using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct FactoryEffectSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<StatusCenter> _centerLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();

            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _centerLookup = state.GetComponentLookup<StatusCenter>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _creatureTag.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _centerLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            if (global.InPause)
            {
                return;
            }

            //特效（一帧最大10个）
            for (var i = global.EffectCreateBuffer.Length - 1; i >= 0; i--)
            {
                var buffer = global.EffectCreateBuffer[i];
                global.EffectCreateBuffer.RemoveAt(i);

                if (buffer.ResourceId > 0 && _creatureTag.HasComponent(buffer.Parent))
                {
                    if (_deadLookup.HasComponent(buffer.Parent) && _deadLookup.IsComponentEnabled(buffer.Parent))
                    {
                        continue;
                    }
                        
                    var bDestroyed = false;
                    foreach (var destroyInfo in global.EffectDestroyBuffer)
                    {
                        if (destroyInfo.Parent == buffer.Parent && destroyInfo.From != EEffectFrom.None)
                        {
                            if (destroyInfo.From == buffer.From && destroyInfo.FromId == buffer.FromId)
                            {
                                bDestroyed = true;
                                break;
                            }
                        }
                    }
                    if (bDestroyed)
                    {
                        continue;
                    }
                }
                
                FactoryHelper.CreateEffects(global, cache, buffer, ecb, _transformLookup, _centerLookup);
               
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}