using Dots;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    /*[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]*/
    
    [UpdateInGroup(typeof(HybridSystemGroup))]
    [UpdateAfter(typeof(HybridLinkSystem))]
    
    public partial struct HybridUpdateTransformSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<ScaleXZData> _scaleXZLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();

            _scaleXZLookup = state.GetComponentLookup<ScaleXZData>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _scaleXZLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
          
            //update
            foreach (var (hybridTransform, transform, entity) in SystemAPI.Query<HybridTransform, LocalToWorld>().WithEntityAccess())
            {
                var scale = hybridTransform.PrefabScale * new float3(transform.Value.Scale());
                if (_scaleXZLookup.TryGetComponent(entity, out var scaleXZ))
                {
                    scale.x *= scaleXZ.X;
                    scale.z *= scaleXZ.Z;
                }
                
                hybridTransform.Value.localScale = scale;    
                hybridTransform.Value.rotation = transform.Rotation;
                hybridTransform.Value.position = transform.Position;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}