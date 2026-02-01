using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    public struct AnimationMoveComponent : IComponentData
    {
        public float Speed;
        public float3 Forward;
        public float TotalTime;
        public float CurTime;
    }
    
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial struct AnimationMoveSystem : ISystem
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
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;
            new EntityMoveAnimationJob
            {
                GlobalEntity = global.Entity,
                DeltaTime = deltaTime, 
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

 
        [BurstCompile]
        private partial struct EntityMoveAnimationJob : IJobEntity
        {
            public Entity GlobalEntity;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            
            [BurstCompile]
            private void Execute(RefRW<AnimationMoveComponent> info, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (info.ValueRO.TotalTime > 0 && info.ValueRO.CurTime >= info.ValueRO.TotalTime)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });
                    return;
                }

                info.ValueRW.CurTime = info.ValueRO.CurTime + DeltaTime;

                var targetPos = localTransform.ValueRO.Position + info.ValueRO.Forward * info.ValueRO.Speed * DeltaTime;
                localTransform.ValueRW.Position = targetPos;
            }
        }
    }
}