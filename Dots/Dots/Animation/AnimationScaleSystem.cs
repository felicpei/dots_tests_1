using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    public struct AnimationScaleComponent : IComponentData
    {
        public float OriginScale;
        public float ScaleMultiple;
        public float MaxTime;
        public float Timer;
    }

    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial struct AnimationScaleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
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

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (tag, localTransform, entity) in 
                     SystemAPI.Query<RefRW<AnimationScaleComponent>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                if (tag.ValueRO.Timer >= tag.ValueRO.MaxTime)
                {
                    ecb.RemoveComponent<AnimationScaleComponent>(entity); 
                    continue;
                }

                tag.ValueRW.Timer = tag.ValueRO.Timer + deltaTime;

                //更新特效
                var multiple = tag.ValueRO.Timer / tag.ValueRO.MaxTime * tag.ValueRO.ScaleMultiple;
                localTransform.ValueRW.Scale = tag.ValueRO.OriginScale * multiple;
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}