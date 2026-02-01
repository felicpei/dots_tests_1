using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    public partial struct ServantMoveSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<InDashingTag> _dashLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<DisableMoveTag> _banMoveLookup;
        [ReadOnly] private ComponentLookup<InFreezeTag> _inFreezeLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<ShieldProperties> _shieldLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private ComponentLookup<ServantLockForward> _servantLockForwardLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<InputProperties>();

            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _dashLookup = state.GetComponentLookup<InDashingTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _banMoveLookup = state.GetComponentLookup<DisableMoveTag>(true);
            _inFreezeLookup = state.GetComponentLookup<InFreezeTag>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _shieldLookup = state.GetComponentLookup<ShieldProperties>(true);
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _servantLockForwardLookup = state.GetComponentLookup<ServantLockForward>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var global = GetAspect<GlobalAspect>(GetSingletonEntity<GlobalInitialized>());


            _skillEntitiesLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _dashLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _banMoveLookup.Update(ref state);
            _inFreezeLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _shieldLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _servantLockForwardLookup.Update(ref state);

            var localPlayer = GetSingletonEntity<LocalPlayerTag>();
            if (!_transformLookup.TryGetComponent(localPlayer, out var localPlayerTrans))
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var input = GetSingleton<InputProperties>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var mainSpeed = 3f;

            if (!global.InPause)
            {
                foreach (var (tag, creatureMove, creatureForward, localTransform, entity)
                         in Query<RefRW<MainServantTag>, RefRW<CreatureMove>, RefRW<CreatureForward>, RefRW<LocalTransform>>().WithEntityAccess())
                {
                    if (_deadLookup.HasComponent(entity) && _deadLookup.IsComponentEnabled(entity))
                    {
                        continue;
                    }

                    //是否有定身等操作禁止移动
                    var bCanMove = !_dashLookup.IsComponentEnabled(entity) && !_inFreezeLookup.IsComponentEnabled(entity) && !_banMoveLookup.IsComponentEnabled(entity);

                    //move
                    if (input.MoveMode == EMoveJoyMode.Walk && !MathHelper.IsZero(input.MoveDirection) && bCanMove)
                    {
                        //calc moveSpeed
                        var addFactor = CreatureHelper.GetMoveSpeedFactor(entity, _creatureLookup, _shieldLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, _attrLookup, _attrModifyLookup);
                        var speed = creatureMove.ValueRO.MoveSpeedSource;
                        speed = BuffHelper.CalcFactor(speed, addFactor);

                        creatureMove.ValueRW.MoveSpeedResult = speed;

                        var movePercent = input.MovePercent;
                        var dir = math.normalizesafe(new float3(input.MoveDirection.x, 0, input.MoveDirection.y));

                        speed *= movePercent;

                        //移动距离
                        var moveDist = deltaTime * speed;

                        //判断是否有碰撞直接的
                        var targetPos = localTransform.ValueRO.Position + dir * moveDist;

                        //限制Z
                        var limitZ = localPlayerTrans.Position.z - 4f;
                        if (targetPos.z < limitZ)
                        {
                            targetPos.z = limitZ;
                        }

                        //todo 东方磁铁
                        if (targetPos.z > 8f)
                        {
                            ecb.SetComponent(entity, new PickupMagnetTag { Radius = 30 });
                            ecb.SetComponentEnabled<PickupMagnetTag>(entity, true);
                        }

                        localTransform.ValueRW.Position = targetPos;

                        //统计移动距离, 触发技能Trigger
                        CreatureHelper.CountMoveDist(entity, creatureMove, moveDist, _skillEntitiesLookup, _skillTagLookup, ecb);

                        //更新状态
                        creatureMove.ValueRW.InMove = true;
                        mainSpeed = speed;

                        var lookForward = _servantLockForwardLookup.HasComponent(entity) && _servantLockForwardLookup.IsComponentEnabled(entity);
                        creatureForward.ValueRW.MoveForward = dir;
                        creatureForward.ValueRW.FaceForward = lookForward ? MathHelper.Up : dir;


                        //save move history
                        tag.ValueRW.recordTimer += deltaTime;
                        if (tag.ValueRO.recordTimer >= 0.0666f * (creatureMove.ValueRO.MoveSpeedSource / speed))
                        {
                            tag.ValueRW.recordTimer = 0;
                            global.MoveHistory.Add(new MoveHistory { Value = localTransform.ValueRO.Position, });
                            if (global.MoveHistory.Length > 1024)
                            {
                                global.MoveHistory.RemoveAt(0);
                            }
                        }
                    }
                    else
                    {
                        if (creatureMove.ValueRO.InMove)
                        {
                            //do something once  
                            creatureMove.ValueRW.InMove = false;
                        }

                        creatureForward.ValueRW.FaceForward = MathHelper.Up;

                        if (global.MoveHistory.Length < 32)
                        {
                            tag.ValueRW.recordTimer += deltaTime;
                            if (tag.ValueRO.recordTimer >= 0.0666f)
                            {
                                tag.ValueRW.recordTimer = 0;

                                //calc moveSpeed
                                var speed = creatureMove.ValueRO.MoveSpeedSource;
                                var targetPos = tag.ValueRO.initTempPos - creatureForward.ValueRO.MoveForward * 0.0666f * speed;
                                tag.ValueRW.initTempPos = targetPos;

                                global.MoveHistory.Insert(0, new MoveHistory
                                {
                                    Value = targetPos,
                                });
                            }
                        }
                    }

                    //风速影响
                    CreatureHelper.CalcWindPos(localPlayer, entity, deltaTime, localTransform, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                }
            }

            foreach (var (servant, creatureMove, creatureForward, localTransform, entity)
                     in Query<ServantProperties, RefRW<CreatureMove>, RefRW<CreatureForward>, RefRW<LocalTransform>>().WithNone<MainServantTag>().WithEntityAccess())
            {
                var bInMove = false;
                var idx = 1;
                if (servant.Idx > 1)
                {
                    idx = servant.Idx;
                }

                if (global.GetSubServantPos(idx, out var targetPos))
                {
                    var moveDist = math.distance(targetPos, localTransform.ValueRO.Position);
                    var dir = math.normalizesafe(targetPos - localTransform.ValueRO.Position);
                    dir.y = 0;

                    var moveStep = deltaTime * mainSpeed;
                    if (moveDist > moveStep)
                    {
                        localTransform.ValueRW.Position += dir * moveStep;
                    }
                    else
                    {
                        // Snap to target if within move step
                        localTransform.ValueRW.Position = targetPos;
                    }

                    //localTransform.ValueRW.Position = math.lerp(localTransform.ValueRW.Position, targetPos, deltaTime * 10);

                    //统计移动距离, 触发技能Trigger
                    CreatureHelper.CountMoveDist(entity, creatureMove, moveDist, _skillEntitiesLookup, _skillTagLookup, ecb);

                    //更新状态
                    creatureMove.ValueRW.InMove = true;

                    //更新方向
                    var lookForward = _servantLockForwardLookup.HasComponent(entity) && _servantLockForwardLookup.IsComponentEnabled(entity);
                    creatureForward.ValueRW.MoveForward = dir;
                    creatureForward.ValueRW.FaceForward = lookForward ? MathHelper.Up : dir;
                    bInMove = moveDist > 0.05f;
                }

                if (!bInMove)
                {
                    if (creatureMove.ValueRO.InMove)
                    {
                        //do something once  
                        creatureMove.ValueRW.InMove = false;
                    }

                    creatureForward.ValueRW.FaceForward = MathHelper.Up;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}