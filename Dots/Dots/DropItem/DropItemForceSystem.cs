
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(DropItemSystemGroup))]
    public partial struct DropItemForceSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
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

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            
            new ForceJob
            {
                Gravity = -9.8f,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb.AsParallelWriter(),
                CollisionWorld = collisionWorld,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct ForceJob : IJobEntity
        {
            public float DeltaTime;
            public float Gravity;

            [ReadOnly] public CollisionWorld CollisionWorld;
            public EntityCommandBuffer.ParallelWriter Ecb;

            [BurstCompile]
            private void Execute(RefRW<DropItemForceTag> tag, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                //阻力
                tag.ValueRW.Speed -= DeltaTime * 10f;
                tag.ValueRW.VerticalVelocity += Gravity * DeltaTime; // 垂直速度变化
                
                if (tag.ValueRW.Speed <= 0)
                {
                    Ecb.SetComponentEnabled<DropItemForceTag>(sortKey, entity, false);
                    return;
                }

                var targetPos = localTransform.ValueRO.Position + tag.ValueRO.Forward * tag.ValueRO.Speed * DeltaTime + new float3(0, tag.ValueRW.VerticalVelocity * DeltaTime, 0);
                var groundPos = PhysicsHelper.GetGroundPos(targetPos, CollisionWorld);
                if (targetPos.y < groundPos.y)
                {
                    targetPos.y = groundPos.y;
                }
                localTransform.ValueRW.Position = targetPos;
            }
        }
    }
}