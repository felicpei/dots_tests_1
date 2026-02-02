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
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureDashStartSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<InDashingTag> _inDashLookup;
        [ReadOnly] private ComponentLookup<StatusForward> _forwardLookup;
        [ReadOnly] private ComponentLookup<PhysicsMass> _physicsMassLookup;
        [ReadOnly] private ComponentLookup<PhysicsVelocity> _velocityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _inDashLookup = state.GetComponentLookup<InDashingTag>(true);
            _forwardLookup = state.GetComponentLookup<StatusForward>(true);
            _physicsMassLookup = state.GetComponentLookup<PhysicsMass>();
            _velocityLookup = state.GetComponentLookup<PhysicsVelocity>();
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

            _deadLookup.Update(ref state);
            _inDashLookup.Update(ref state);
            _forwardLookup.Update(ref state);
            _physicsMassLookup.Update(ref state);
            _velocityLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            foreach (var (tag, localTransform, creature, entity) 
                     in SystemAPI.Query<DashStartTag, RefRW<LocalTransform>, RefRW<CreatureTag>>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<DashStartTag>(entity, false);
                
                if (_deadLookup.IsComponentEnabled(entity))
                {
                    continue;
                }

                float dist;
                if (tag.ForceDist > 0)
                {
                    dist = tag.ForceDist;
                }
                else
                {
                    dist = math.distance(localTransform.ValueRO.Position, tag.Pos);
                }

                var needTime = dist / tag.Speed;

                var dashInfo = new InDashingTag
                {
                    CurTime = 0,
                    StartPos = localTransform.ValueRO.Position,
                    TotalTime = needTime,
                    Speed = tag.Speed,
                    Forward = math.normalizesafe(tag.Pos - localTransform.ValueRO.Position),
                    AfterSkillId = tag.AfterSkillId,
                    RootSkillId = tag.RootSkillId,
                    SkillAtkValue = tag.SkillAtkValue,
                    SkillRecursionCount = tag.SkillRecursionCount,
                    SkillMaxRecursion = tag.SkillMaxRecursion,
                    PrevTargetPos = tag.PrevTargetPos,
                    PrevTarget = tag.PrevTarget,
                    DisableCollision = tag.DisableCollision,
                    MaxBounceCount = tag.BounceCount,
                };

                //是否禁用碰撞
                /*if (tag.DisableCollision)
                {
                    PhysicsHelper.DisablePhysics(entity, cache, creature.ValueRO, _velocityLookup, ecb);
                }*/

                //绑定特效
                if (tag.EffectId > 0)
                {
                    var effectBuffer = new EffectCreateBuffer
                    {
                        From = EEffectFrom.Dash,
                        FromId = tag.RootSkillId,
                        Loop = true,
                        ResourceId = tag.EffectId,
                        Parent = entity,
                        Rotation = MathHelper.forward2RotationSafe(dashInfo.Forward),
                    };

                    ecb.AppendToBuffer(global.Entity, effectBuffer);
                    dashInfo.HasEffect = true;
                }
                else
                {
                    dashInfo.HasEffect = false;
                }

                //更新方向
                CreatureHelper.UpdateFaceForward(entity, dashInfo.Forward, _inDashLookup, ecb);

                ecb.SetComponent(entity, dashInfo);
                ecb.SetComponentEnabled<InDashingTag>(entity, true);
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}