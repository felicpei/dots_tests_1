using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSystemGroup))]
    public partial struct BulletDestroySystem : ISystem
    {
        [ReadOnly] private BufferLookup<BindingBullet> _bindBulletLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _bindBulletLookup = state.GetBufferLookup<BindingBullet>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());

            _bindBulletLookup.Update(ref state);

            foreach (var (tag, properties, entity) in SystemAPI.Query<RefRW<BulletDestroyTag>, BulletProperties>().WithEntityAccess())
            {
                tag.ValueRW.FrameCounter++;
                if (tag.ValueRO.FrameCounter >= 3)
                {
                    //clear binding bullet
                    if (_bindBulletLookup.TryGetBuffer(properties.TransformParent, out var bindingBullets))
                    {
                        for (var i = bindingBullets.Length - 1; i >= 0; i--)
                        {
                            if (bindingBullets[i].Value == entity)
                            {
                                bindingBullets.RemoveAt(i);
                            }
                        }
                    }

                    ecb.SetComponentEnabled<BulletDestroyTag>(entity, false);
                    ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                }
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}