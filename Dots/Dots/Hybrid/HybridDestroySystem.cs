using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(EntityDestroySystem))]
    public partial struct HybridDestroySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            //destroy 
            foreach (var (goTransform, entity) in SystemAPI.Query<HybridTransform>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                if (goTransform.Value != null)
                {
                    Object.Destroy(goTransform.Value.gameObject);
                }
                ecb.RemoveComponent<HybridTransform>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}