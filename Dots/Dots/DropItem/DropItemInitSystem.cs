using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(DropItemSystemGroup))]
    [UpdateAfter(typeof(DropItemPickupSystem))]
    public partial struct DropItemInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
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
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new DropItemInitJob
            {
                DeltaTime = deltaTime,
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct DropItemInitJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            [BurstCompile]
            private void Execute(DropItemInitTag tag, RefRW<LocalTransform> localTransform, RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                Ecb.SetComponentEnabled<DropItemInitTag>(sortKey, entity, false);

                var hitForward= MathHelper.RotateForward(MathHelper.Up, random.ValueRW.Value.NextFloat(-60f, 60f));
                var randForceY = new float3(0, random.ValueRW.Value.NextFloat(0.6f, 0.8f), 0);
                var forward = math.normalizesafe(randForceY + hitForward);
                var speed = random.ValueRW.Value.NextFloat(6, 9);

                Ecb.SetComponentEnabled<DropItemForceTag>(sortKey, entity, true);
                Ecb.SetComponent(sortKey, entity, new DropItemForceTag
                {
                    Forward = forward,
                    Speed = speed
                });
            }
        }
    }
}