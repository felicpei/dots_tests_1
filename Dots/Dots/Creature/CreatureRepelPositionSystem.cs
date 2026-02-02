using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureRepelPositionSystem : ISystem
    {
        private EntityQuery _query;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<InDashingTag> _dashLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<CreatureRepelPosition>();
            _query = state.GetEntityQuery(queryBuilder);
            queryBuilder.Dispose();

            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _dashLookup = state.GetComponentLookup<InDashingTag>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_query.IsEmpty)
            {
                return;
            }

            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            if (global.InPause)
            {
                return;
            }

            _deadLookup.Update(ref state);
            _dashLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            //击退力度的处理
            new RepelJob
            {
                World = collisionWorld,
                DeltaTime = deltaTime,
                DeadLookup = _deadLookup,
                DashLookup = _dashLookup,
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);  
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct RepelJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public CollisionWorld World;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<InDashingTag> DashLookup;

            [BurstCompile]
            private void Execute(RefRW<CreatureRepelPosition> tag, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                //死亡、冲刺中不处理
                if (DeadLookup.HasComponent(entity) && DeadLookup.IsComponentEnabled(entity) ||
                    DashLookup.HasComponent(entity) && DashLookup.IsComponentEnabled(entity))
                {
                    Ecb.SetComponentEnabled<InRepelState>(sortKey, entity, false);
                    Ecb.SetComponentEnabled<CreatureRepelPosition>(sortKey, entity, false);
                    return;
                }

                //时间到了结束
                if (tag.ValueRO.Timer <= 0)
                {
                    //init
                    Ecb.SetComponentEnabled<InRepelState>(sortKey, entity, true);

                    //target pos 要判断下有没有障碍
                    if (PhysicsHelper.RayCast(World, PhysicsLayers.DontMove, localTransform.ValueRO.Position, tag.ValueRO.Forward, tag.ValueRO.Distance, out var hitInfo))
                    {
                        tag.ValueRW.TargetPos = hitInfo.Position - tag.ValueRO.Forward * 0.3f;
                    }
                    else
                    {
                        tag.ValueRW.TargetPos = localTransform.ValueRO.Position + tag.ValueRO.Forward * tag.ValueRO.Distance;
                    }
                }
                else if (tag.ValueRO.Timer >= tag.ValueRO.ContTime)
                {
                    Ecb.SetComponentEnabled<CreatureRepelPosition>(sortKey, entity, false);
                    Ecb.SetComponentEnabled<InRepelState>(sortKey, entity, false);
                    return;
                }

                var scaleSpeed = (tag.ValueRO.MaxScale - 1f) / (tag.ValueRO.ContTime / 2f);
                if (tag.ValueRO.Timer < tag.ValueRO.ContTime / 2f)
                {
                    //逐渐变大
                    tag.ValueRW.ExtraScale += DeltaTime * scaleSpeed;
                }
                else
                {
                    tag.ValueRW.ExtraScale -= DeltaTime * scaleSpeed;
                }

                var speed = tag.ValueRO.Distance / tag.ValueRO.ContTime;
                localTransform.ValueRW.Position = math.lerp(localTransform.ValueRO.Position, tag.ValueRO.TargetPos, DeltaTime * speed);


                tag.ValueRW.Timer += DeltaTime;
            }
        }
    }
}