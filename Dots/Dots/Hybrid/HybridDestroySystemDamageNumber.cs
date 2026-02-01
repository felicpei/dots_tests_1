using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(EntityDestroySystem))]
    public partial struct HybridDestroySystemDamageNumber : ISystem
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
            foreach (var (tag, entity) in SystemAPI.Query<HybridDamageNumberController>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                DamageNumberPool.Recycle(tag.Value);
                ecb.RemoveComponent<HybridDamageNumberController>(entity);
            }
            
            //destroy 
            foreach (var (tag, entity) in SystemAPI.Query<HybridDamageElementController>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                DamageNumberPool.RecycleElement(tag.Value);
                ecb.RemoveComponent<HybridDamageElementController>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}