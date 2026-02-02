using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureRepelStartSystem : ISystem
    {
        private EntityQuery _query;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<InDashingTag> _dashLookup;
        [ReadOnly] private ComponentLookup<CreatureRepelCd> _repelCdLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<CreatureRepelStart>();
            _query = state.GetEntityQuery(queryBuilder);
            queryBuilder.Dispose();

            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _dashLookup = state.GetComponentLookup<InDashingTag>(true);
            _repelCdLookup = state.GetComponentLookup<CreatureRepelCd>(true);
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
            _repelCdLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            //击退力度的处理
            new RepelStartJob
            {
                InDeadLookup = _deadLookup,
                InDashLookup = _dashLookup,
                RepelCdLookup = _repelCdLookup,
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct RepelStartJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<InDashingTag> InDashLookup;
            [ReadOnly] public ComponentLookup<InDeadState> InDeadLookup;
            [ReadOnly] public ComponentLookup<CreatureRepelCd> RepelCdLookup;

            [BurstCompile]
            private void Execute(CreatureRepelStart tag, RefRW<PhysicsVelocity> velocity, CreatureProps props, LocalTransform localTransform, PhysicsMass mass, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                Ecb.SetComponentEnabled<CreatureRepelStart>(sortKey, entity, false);

                //死亡不处理
                if (InDeadLookup.HasComponent(entity) && InDeadLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                //冲刺中不处理
                if (InDashLookup.HasComponent(entity) && InDashLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                if (!MathHelper.IsValid(tag.Forward) || tag.Force <= 0)
                {
                    //Debug.LogError($"Error, added force is NAN:{tag.Forward}");
                    return;
                }
 
                //CD中不处理
                if (RepelCdLookup.HasComponent(entity) && RepelCdLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                //加力度
                if (tag.IsPhysics)
                {
                    //物理方式
                    var force = tag.Force * tag.Forward;
                    velocity.ValueRW.ApplyImpulse(mass, localTransform.Position, localTransform.Rotation, force, localTransform.Position);
                }
                else
                {
                    //传统的坐标方式
                    Ecb.SetComponent(sortKey, entity, new CreatureRepelPosition
                    {
                        Timer = 0,
                        Forward = tag.Forward,
                        Distance = tag.Force,
                        MaxScale = tag.RepelMaxScale,
                        ContTime = tag.RepelTime <= 0 ? 0.1f : tag.RepelTime,
                    });
                    Ecb.SetComponentEnabled<CreatureRepelPosition>(sortKey, entity, true);
                }

                //如果配置了CD，记录CD
                var repelCd = tag.RepelCd > 0 ? tag.RepelCd : props.RepelCd;
                if (repelCd > 0)
                {
                    Ecb.SetComponent(sortKey, entity, new CreatureRepelCd { Timer = repelCd });
                    Ecb.SetComponentEnabled<CreatureRepelCd>(sortKey, entity, true);
                }
            }
        }
    }
}