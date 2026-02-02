using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct FactoryEffectDestroySystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localToWorldLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());

            foreach (var (effect, entity) in  SystemAPI.Query<EffectProperties>().WithEntityAccess())
            {
                if (effect.Parent != Entity.Null && !_localToWorldLookup.HasComponent(effect.Parent))
                {
                    ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                }
            }
           

            for (var i = global.EffectDestroyBuffer.Length - 1; i >= 0; i--)
            {
                var buffer = global.EffectDestroyBuffer[i];
                global.EffectDestroyBuffer.RemoveAt(i);

                foreach (var (effect, effectEntity) in SystemAPI.Query<EffectProperties>().WithEntityAccess())
                {
                    if (effect.Parent == buffer.Parent && buffer.From != EEffectFrom.None)
                    {
                        if (effect.From == buffer.From && effect.FromId == buffer.FromId)
                        {
                            ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = effectEntity });
                        }
                    }
                }
            }
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}