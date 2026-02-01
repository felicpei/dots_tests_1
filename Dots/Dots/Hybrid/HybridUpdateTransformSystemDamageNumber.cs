using Dots;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [UpdateInGroup(typeof(HybridSystemGroup))]
    [UpdateAfter(typeof(HybridLinkSystem))]
    public partial struct HybridUpdateTransformSystemDamageNumber : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            //update
            foreach (var (tag, transform) in SystemAPI.Query<HybridDamageNumberController, LocalToWorld>())
            {
                var scale = new float3(transform.Value.Scale());
                var hybridTransform = tag.Value.transform;
                hybridTransform.localScale = scale;
                hybridTransform.rotation = transform.Rotation;
                hybridTransform.position = transform.Position;
            }
            
            foreach (var (tag, transform) in SystemAPI.Query<HybridDamageElementController, LocalToWorld>())
            {
                var scale = new float3(transform.Value.Scale());
                var hybridTransform = tag.Value.transform;
                hybridTransform.localScale = scale;
                hybridTransform.rotation = transform.Rotation;
                hybridTransform.position = transform.Position;
            }
        }
    }
}