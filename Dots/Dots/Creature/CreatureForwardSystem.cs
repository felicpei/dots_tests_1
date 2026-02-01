using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureForwardSystem : ISystem
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

            var deltaTime = SystemAPI.Time.DeltaTime;
            new ForwardJobs
            {
                DeltaTime = deltaTime,
            }.ScheduleParallel();
        }


        [BurstCompile]
        private partial struct ForwardJobs : IJobEntity
        {
            public float DeltaTime;

            [BurstCompile]
            private void Execute(RefRW<LocalTransform> localTransform, CreatureProperties creature, RefRW<CreatureForward> tag, Entity entity)
            {
                //15fps立即转
                if (creature.Type == ECreatureType.Player || DeltaTime > 0.066f)
                {
                    localTransform.ValueRW.Rotation = MathHelper.forward2RotationSafe(tag.ValueRO.FaceForward);
                }
                else
                {
                    var t = DeltaTime * 15f;
                    var target = MathHelper.forward2RotationSafe(tag.ValueRO.FaceForward);
                    tag.ValueRW.Result = math.nlerp(tag.ValueRO.Result, target, t);
                    localTransform.ValueRW.Rotation = tag.ValueRO.Result;
                }
            }
        }
    }
}