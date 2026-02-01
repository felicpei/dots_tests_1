using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    public struct AnimationRotateComponent : IComponentData
    {
        public float Speed;
        public float3 CenterPos;
        public float Radius;
        public bool UseAtkRange;
        public float SourceScale;
        public Entity MasterCreature;
    }
    
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial struct AnimationRotateSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
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
            
            _creatureLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (tag, localTransform) in SystemAPI.Query<AnimationRotateComponent, RefRW<LocalTransform>>())
            {
                var direction = math.rotate(localTransform.ValueRO.Rotation, new float3(0, 0, 1));
                var centerPos = localTransform.ValueRO.Position + -direction * tag.Radius;
                
                var newRot  = MathHelper.RotateQuaternion(localTransform.ValueRO.Rotation, tag.Speed * deltaTime);
                var newDirection = math.rotate(newRot, new float3(0, 0, 1));
                var newPos = centerPos + newDirection * tag.Radius;
                
                localTransform.ValueRW.Rotation = newRot;
                localTransform.ValueRW.Position = newPos;
                
                if (tag.UseAtkRange && _creatureLookup.HasComponent(tag.MasterCreature))
                {
                    var factor = AttrHelper.GetDamageRangeFactor(tag.MasterCreature, _creatureLookup, _attrLookup, _attrModifyLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, true);
                    localTransform.ValueRW.Scale = BuffHelper.CalcFactor(tag.SourceScale, factor);
                }
            }
            
            state.Dependency.Complete();
        }
    }
}