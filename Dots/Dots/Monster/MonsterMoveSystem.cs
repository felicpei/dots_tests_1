using Deploys;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    public partial struct MonsterMoveSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<InDashingTag> _inDashLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<DisableMoveTag> _disableMoveLookup;
        [ReadOnly] private ComponentLookup<InFreezeState> _inFreezeLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<MonsterTarget> _monsterTargetLookup;
        [ReadOnly] private ComponentLookup<ShieldProperties> _shieldLookup;
        [ReadOnly] private ComponentLookup<CreatureProps> _propsLookup;
        [ReadOnly] private ComponentLookup<DisableAutoTargetTag> _disableAutoTargetLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<CreatureFps> _creatureFpsLookup;
        [ReadOnly] private ComponentLookup<InRepelState> _inRepelLookup;
        [ReadOnly] private ComponentLookup<InBornTag> _inBornLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CacheProperties>();

            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _inDashLookup = state.GetComponentLookup<InDashingTag>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _disableMoveLookup = state.GetComponentLookup<DisableMoveTag>(true);
            _inFreezeLookup = state.GetComponentLookup<InFreezeState>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _monsterTargetLookup = state.GetComponentLookup<MonsterTarget>(true);
            _shieldLookup = state.GetComponentLookup<ShieldProperties>(true);
            _propsLookup = state.GetComponentLookup<CreatureProps>(true);
            _disableAutoTargetLookup = state.GetComponentLookup<DisableAutoTargetTag>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _creatureFpsLookup = state.GetComponentLookup<CreatureFps>(true);
            _inRepelLookup = state.GetComponentLookup<InRepelState>(true);
            _inBornLookup = state.GetComponentLookup<InBornTag>(true);
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

            _creatureTag.Update(ref state);
            _localToWorldLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _inDashLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _disableMoveLookup.Update(ref state);
            _inFreezeLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _monsterTargetLookup.Update(ref state);
            _shieldLookup.Update(ref state);
            _propsLookup.Update(ref state);
            _disableAutoTargetLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _creatureFpsLookup.Update(ref state);
            _inRepelLookup.Update(ref state);
            _inBornLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

            //复活状态
            new MonsterMoveJob
            {
                ScreenBound = global.ScreenBound,
                DeltaTime = deltaTime,
                Ecb = ecb.AsParallelWriter(),
                InMonsterPause = global.InMonsterPause,
                LocalPlayerEntity = localPlayer,
                CacheEntity = cacheEntity,
                CollisionWorld = collisionWorld,
                CacheLookup = _cacheLookup,
                CreatureTag = _creatureTag,
                InDashLookup = _inDashLookup,
                InDeadLookup = _deadLookup,
                InFreezeLookup = _inFreezeLookup,
                DisableMoveLookup = _disableMoveLookup,
                SummonLookup = _summonLookup,
                ShieldLookup = _shieldLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                MonsterTargetLookup = _monsterTargetLookup,
                LocalToWorldLookup = _localToWorldLookup,
                PropsLookup = _propsLookup,
                CreatureFpsLookup = _creatureFpsLookup,
                DisableAutoTargetLookup = _disableAutoTargetLookup,
                SkillEntitiesLookup = _skillEntitiesLookup,
                SkillTagLookup = _skillTagLookup,
                InRepelLookup = _inRepelLookup,
                InBornLookup = _inBornLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct MonsterMoveJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public bool InMonsterPause;
            public Entity LocalPlayerEntity;
            public Entity CacheEntity;
            public float4 ScreenBound;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTag;
            [ReadOnly] public ComponentLookup<InDashingTag> InDashLookup;
            [ReadOnly] public ComponentLookup<InDeadState> InDeadLookup;
            [ReadOnly] public ComponentLookup<InFreezeState> InFreezeLookup;
            [ReadOnly] public ComponentLookup<DisableMoveTag> DisableMoveLookup;
            [ReadOnly] public ComponentLookup<InRepelState> InRepelLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<ShieldProperties> ShieldLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<MonsterTarget> MonsterTargetLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentLookup<CreatureProps> PropsLookup;
            [ReadOnly] public ComponentLookup<DisableAutoTargetTag> DisableAutoTargetLookup;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<CreatureFps> CreatureFpsLookup;
            [ReadOnly] public ComponentLookup<InBornTag> InBornLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            
            [BurstCompile]
            private void Execute(RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove,
                RefRW<LocalTransform> localTransform, RefRW<RandomSeed> random, RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
                MonsterProperties monsterProperties, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                //dash状态不处理
                if (InDashLookup.HasComponent(entity) && InDashLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                //击退状态不处理
                if (InRepelLookup.HasComponent(entity) && InRepelLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                //死亡，冻结，禁止移动，出生状态不处理
                if (InDeadLookup.IsComponentEnabled(entity) || InFreezeLookup.IsComponentEnabled(entity) || DisableMoveLookup.IsComponentEnabled(entity) || InBornLookup.IsComponentEnabled(entity))
                {
                    StopMove(agentBody, creatureMove);
                    return;
                }

                if (!CreatureTag.TryGetComponent(entity, out var creature))
                {
                    return;
                }

                if (InMonsterPause && creature.TeamId == ETeamId.Monster)
                {
                    StopMove(agentBody, creatureMove);
                    return;
                }

                var bMoved = false;
                var forward = float3.zero;
                var speed = 0f;

                //计算移动速度
                var addSpeedFactor = CreatureHelper.GetMoveSpeedFactor(entity, SummonLookup, PropsLookup, ShieldLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, AttrLookup, AttrModifyLookup);

                //attr enemy speed
                addSpeedFactor += AttrHelper.GetAttr(LocalPlayerEntity, EAttr.EnemySpeed, AttrLookup, AttrModifyLookup, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);

                var minSpeedFac = AttrHelper.GetMin(EAttr.EnemySpeed);
                if (addSpeedFactor < minSpeedFac)
                {
                    addSpeedFactor = minSpeedFac;
                }
                
                switch (tag.ValueRO.Mode)
                {
                    case EMonsterMoveMode.Direct:
                    {
                        bMoved = DirectMove(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, MonsterTargetLookup, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.KeepDist:
                    {
                        bMoved = KeepDist(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, MonsterTargetLookup, CollisionWorld, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.Round:
                    {
                        bMoved = AroundMove(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, monsterProperties, MonsterTargetLookup, SummonLookup, LocalToWorldLookup, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.RandomEnemy:
                    {
                        bMoved = RandomEnemy(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, random, CollisionWorld, SummonLookup, CreatureTag, InDeadLookup, LocalToWorldLookup, DisableAutoTargetLookup, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.ToFormationCenter:
                    {
                        bMoved = ToCenter(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.PatrolRandomPoint:
                    {
                        bMoved = PatrolRandomPoint(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, random, CollisionWorld, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.PatrolEnemyDirection:
                    {
                        bMoved = PatrolDir(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, MonsterTargetLookup, Ecb, sortKey, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.PatrolLR:
                    {
                        bMoved = PatrolLR(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, MonsterTargetLookup, out forward, out speed);
                        break;
                    }
                    case EMonsterMoveMode.MoveLR:
                    {
                        bMoved = MoveLR(DeltaTime, entity, addSpeedFactor, tag, creatureMove, localTransform, agentBody, locomotion, ScreenBound, Ecb, sortKey, out forward, out speed);
                        break;
                    }
                }

                //更新方向
                if (bMoved)
                {
                    //skill trigger
                    if (speed > 0f)
                    {
                        CreatureHelper.CountMoveDist(entity, creatureMove, speed * DeltaTime, SkillEntitiesLookup, SkillTagLookup, Ecb, sortKey);
                    }

                    if (CacheHelper.GetMonsterConfig(monsterProperties.Id, CacheEntity, CacheLookup, out var config))
                    {
                        //update face forward
                        if (!config.BanTurn && MathHelper.IsValid(forward))
                        {
                            CreatureHelper.UpdateFaceForward(entity, forward, InDashLookup, Ecb, sortKey);
                        }
                    }

                    if (CreatureFpsLookup.TryGetComponent(entity, out var creatureFps))
                    {
                        if (creatureFps.FpsFactorZero == false && creatureMove.ValueRO.MoveSpeedSource > 0)
                        {
                            var fpsFactor = creatureMove.ValueRO.MoveSpeedResult / creatureMove.ValueRO.MoveSpeedSource;
                            Ecb.SetComponent(sortKey, entity, new CreatureFps { FpsFactor = fpsFactor, FpsFactorZero = false });
                        }
                    }
                }

                //风速影响
                CreatureHelper.CalcWindPos(LocalPlayerEntity, entity, DeltaTime, localTransform, CreatureTag, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
            }
        }

        private static void MoveToPos(float3 targetPos, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform)
        {
            localTransform.ValueRW.Position = targetPos;
            creatureMove.ValueRW.InMove = true;
        }

        private static void StopMove(RefRW<AgentBody> agentBody, RefRW<StatusMove> creature)
        {
            agentBody.ValueRW.Stop();
            if (creature.ValueRO.InMove)
            {
                creature.ValueRW.InMove = false;
            }
        }

        private static bool DirectMove(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            ComponentLookup<MonsterTarget> monsterTargetLookup, out float3 forward, out float speed)
        {
            forward = float3.zero;

            var configSpeed = tag.ValueRO.Param1;
            var stopDist = tag.ValueRO.Param2;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            if (!monsterTargetLookup.TryGetComponent(entity, out var monsterTarget) || !monsterTarget.HasTarget)
            {
                StopMove(agentBody, creatureMove);
                return false;
            }

            var destination = monsterTarget.Pos;

            //move
            locomotion.ValueRW.Speed = speed;
            locomotion.ValueRW.StoppingDistance = stopDist;
            locomotion.ValueRW.Acceleration = 10;
            agentBody.ValueRW.IsStopped = false;
            agentBody.ValueRW.Destination = destination;

            forward = math.normalizesafe(agentBody.ValueRO.Velocity);
            forward.y = 0;

            /*if (distSq > stopDist * stopDist)
            {
                forward = math.normalizesafe(monsterTarget.Pos - localTransform.ValueRO.Position);
                var targetPos = localTransform.ValueRO.Position + forward * speed * deltaTime;
                MoveToPos(targetPos, creatureMove, localTransform);
            }
            else
            {
                StopMove(creatureMove);
            }*/

            return true;
        }

        private static bool PatrolRandomPoint(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            RefRW<RandomSeed> random, CollisionWorld world, out float3 forward, out float speed)
        {
            forward = float3.zero;

            var configSpeed = tag.ValueRO.Param1;
            var min = tag.ValueRO.Param2;
            var max = tag.ValueRO.Param3;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            //第一次决定目标点
            if (!tag.ValueRW.IsMoving)
            {
                var targetPos = MathHelper.RandomRangePos(random, tag.ValueRO.BornPos, min, max);
                tag.ValueRW.TargetPos = targetPos;
                tag.ValueRW.IsMoving = true;
            }
            else
            {
                var dist = math.distance(localTransform.ValueRO.Position, tag.ValueRO.TargetPos);
                forward = math.normalizesafe(tag.ValueRO.TargetPos - localTransform.ValueRO.Position);

                //碰到围栏立刻转向
                var bHitFence = false;
                var allHits = new NativeList<Unity.Physics.RaycastHit>(Allocator.Temp);
                if (PhysicsHelper.RayCastAll(world, PhysicsLayers.DontMove, localTransform.ValueRO.Position, forward, 0.2f, allHits))
                {
                    foreach (var hit in allHits)
                    {
                        if (hit.Entity != entity)
                        {
                            bHitFence = true;
                            break;
                        }
                    }
                }

                allHits.Dispose();

                if (dist > 0.2f && !bHitFence)
                {
                    var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;
                    MoveToPos(moveTo, creatureMove, localTransform);
                    return true;
                }

                tag.ValueRW.IsMoving = false;
            }

            return false;
        }

        private static bool KeepDist(float deltaTime, Entity entity, float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            ComponentLookup<MonsterTarget> monsterTargetLookup, CollisionWorld collisionWorld, out float3 forward, out float speed)
        {
            forward = float3.zero;

            var configSpeed = tag.ValueRO.Param1;
            var stopDist = tag.ValueRO.Param2;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            if (!monsterTargetLookup.TryGetComponent(entity, out var monsterTarget) || !monsterTarget.HasTarget)
            {
                StopMove(agentBody, creatureMove);
                return false;
            }

            var dist = math.distance(localTransform.ValueRO.Position, monsterTarget.Pos);
            var minDist = stopDist * 0.9f;
            var maxDist = stopDist * 1.1f;


            var dirTarget = math.normalizesafe(monsterTarget.Pos - localTransform.ValueRO.Position);
            if (dist > maxDist)
            {
                forward = math.normalizesafe(monsterTarget.Pos - localTransform.ValueRO.Position);
                var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;
                MoveToPos(moveTo, creatureMove, localTransform);

                tag.ValueRW.AroundAngle = MathHelper.Forward2Angle(-dirTarget);
                tag.ValueRW.CenterPos = monsterTarget.Pos;
                tag.ValueRW.ToCenter = true;
            }
            else
            {
                if (dist < minDist)
                {
                    //如果碰到围栏了，则不处理
                    var bHitFence = PhysicsHelper.RayCast(collisionWorld, PhysicsLayers.DontMove, localTransform.ValueRO.Position, -dirTarget, 0.2f, out _);
                    if (!bHitFence)
                    {
                        //向max dist移动
                        var targetPos = monsterTarget.Pos - dirTarget * stopDist;
                        forward = math.normalizesafe(targetPos - localTransform.ValueRO.Position);
                        var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;
                        MoveToPos(moveTo, creatureMove, localTransform);

                        tag.ValueRW.AroundAngle = MathHelper.Forward2Angle(-dirTarget);
                        tag.ValueRW.CenterPos = monsterTarget.Pos;
                        tag.ValueRW.ToCenter = true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    //moving to circle
                    if (tag.ValueRW.ToCenter)
                    {
                        var targetPos = tag.ValueRO.CenterPos + MathHelper.GetCirclePos(tag.ValueRO.AroundAngle, stopDist);

                        var bHitFence = PhysicsHelper.RayCast(collisionWorld, PhysicsLayers.DontMove, localTransform.ValueRO.Position, math.normalizesafe(targetPos - localTransform.ValueRO.Position), 0.2f, out _);
                        if (!bHitFence)
                        {
                            forward = math.normalizesafe(targetPos - localTransform.ValueRO.Position);
                            var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;

                            MoveToPos(moveTo, creatureMove, localTransform);
                            if (math.distance(localTransform.ValueRO.Position, targetPos) < 0.1f)
                            {
                                tag.ValueRW.ToCenter = false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        //绕圈移动
                        var newAngle = tag.ValueRO.AroundAngle + MathHelper.SpeedToCircleAngle(speed, deltaTime, stopDist);
                        var targetPos = tag.ValueRO.CenterPos + MathHelper.GetCirclePos(newAngle, stopDist);

                        var bHitFence = PhysicsHelper.RayCast(collisionWorld, PhysicsLayers.DontMove, localTransform.ValueRO.Position, math.normalizesafe(targetPos - localTransform.ValueRO.Position), 0.2f, out _);
                        if (!bHitFence)
                        {
                            tag.ValueRW.AroundAngle = newAngle;
                            MoveToPos(targetPos, creatureMove, localTransform);

                            //始终看向玩家
                            forward = dirTarget;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool AroundMove(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            MonsterProperties monsterProperties, ComponentLookup<MonsterTarget> monsterTargetLookup,
            ComponentLookup<StatusSummon> summonLookup, ComponentLookup<LocalToWorld> localToWorldLookup,
            out float3 forward, out float speed)
        {
            var configSpeed = tag.ValueRO.Param2;
            var radius = tag.ValueRO.Param3;
            var clockWise = tag.ValueRO.Param4;
            var isLerpMove = (int)tag.ValueRO.Param5 == 1;

            if (clockWise == 0)
            {
                clockWise = 1;
            }


            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            var centerPos = monsterProperties.BornPos;
            if (summonLookup.TryGetComponent(entity, out var creature))
            {
                if (summonLookup.HasComponent(creature.SummonParent) && localToWorldLookup.TryGetComponent(creature.SummonParent, out var parentTransform))
                {
                    centerPos = parentTransform.Position;
                }
            }

            //根据速度计算位置
            var newAngle = tag.ValueRO.AroundAngle + clockWise * MathHelper.SpeedToCircleAngle(speed, deltaTime, radius);
            tag.ValueRW.AroundAngle = newAngle;

            var targetPos = centerPos + MathHelper.GetCirclePos(tag.ValueRO.AroundAngle, radius);
            forward = math.normalizesafe(targetPos - localTransform.ValueRO.Position);

            if (isLerpMove)
            {
                localTransform.ValueRW.Position = math.lerp(localTransform.ValueRO.Position, targetPos, deltaTime * 0.5f);
                creatureMove.ValueRW.InMove = true;
            }
            else
            {
                MoveToPos(targetPos, creatureMove, localTransform);
            }

            if (monsterTargetLookup.TryGetComponent(entity, out var monsterTarget))
            {
                forward = math.normalizesafe(monsterTarget.Pos - targetPos);
            }

            return true;
        }

        private static bool ToCenter(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            out float3 forward, out float speed)
        {
            forward = float3.zero;
            speed = 0;

            if (tag.ValueRO.FormationMinDist <= 0)
            {
                StopMove(agentBody, creatureMove);
                return false;
            }

            var configSpeed = tag.ValueRO.Param1;
            var stopDist = tag.ValueRO.Param2;
            var loop = (int)tag.ValueRO.Param3 == 1;

            var maxMoveTime = (tag.ValueRO.FormationMinDist - stopDist) / configSpeed;
            var bornDistToCenter = math.distance(tag.ValueRO.BornPos, tag.ValueRO.CenterPos);
            var speedFactor = bornDistToCenter / tag.ValueRO.FormationMinDist;
            configSpeed *= speedFactor;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            var targetPos = tag.ValueRO.ToCenter ? tag.ValueRO.CenterPos : tag.ValueRO.BornPos;

            //为了保持整齐，统一按时间移动，该移动方式速度不受buff影响
            tag.ValueRW.Timer = tag.ValueRO.Timer + deltaTime;

            if (tag.ValueRO.Timer < maxMoveTime)
            {
                //无障碍，直接向目标移动
                forward = math.normalizesafe(targetPos - localTransform.ValueRO.Position);
                var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;
                MoveToPos(moveTo, creatureMove, localTransform);
            }
            else
            {
                StopMove(agentBody, creatureMove);

                if (loop)
                {
                    tag.ValueRW.Timer = 0;
                    tag.ValueRW.ToCenter = !tag.ValueRO.ToCenter;
                }
            }

            return true;
        }

        private static bool RandomEnemy(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            RefRW<RandomSeed> random, CollisionWorld collisionWorld, ComponentLookup<StatusSummon> summonLookup,
            ComponentLookup<CreatureTag> creatureTag, ComponentLookup<InDeadState> deadLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup, ComponentLookup<DisableAutoTargetTag> disableAutoTargetLookup, out float3 faceForward, out float speed)
        {
            var minDist = tag.ValueRO.Param1;
            var maxDist = tag.ValueRO.Param2;
            var parentMinDist = tag.ValueRO.Param3;
            var parentMaxDist = tag.ValueRO.Param4;
            var configSpeed = tag.ValueRO.Param5;
            var turnSpeed = tag.ValueRO.Param6;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            var bHasParent = false;
            var parentPos = float3.zero;
            if (summonLookup.TryGetComponent(entity, out var creature))
            {
                if (summonLookup.HasComponent(creature.SummonParent) && localToWorldLookup.TryGetComponent(creature.SummonParent, out var parentTransform))
                {
                    parentPos = parentTransform.Position;
                    bHasParent = true;
                }
            }

            //寻找目标
            if (tag.ValueRO.CurrentTarget == Entity.Null && creatureTag.TryGetComponent(entity, out var entityCreature))
            {
                var enemies = PhysicsHelper.OverlapEnemies(entity, collisionWorld, localTransform.ValueRO.Position, maxDist, creatureTag, deadLookup, entityCreature.TeamId);
                var randomEnemies = new NativeList<Entity>(Allocator.Temp);

                var totalCount = 0;
                for (var e = 0; e < enemies.Length; e++)
                {
                    var enemy = enemies[e];
                    if (localToWorldLookup.TryGetComponent(enemy, out var enemyTrans))
                    {
                        var enemyPos = enemyTrans.Position;

                        //最小距离判断
                        if (math.distancesq(enemyPos, localTransform.ValueRO.Position) < minDist * minDist)
                        {
                            continue;
                        }

                        if (disableAutoTargetLookup.HasComponent(enemy) && disableAutoTargetLookup.IsComponentEnabled(enemy))
                        {
                            continue;
                        }

                        //有parent的情况, 需要盘点parent的距离是否符合param3
                        if (bHasParent)
                        {
                            var enemyParentDistSq = math.distancesq(enemyPos, parentPos);
                            if (enemyParentDistSq < parentMinDist * parentMinDist || enemyParentDistSq > parentMaxDist * parentMaxDist)
                            {
                                continue;
                            }
                        }

                        //排除上一个目标
                        if (enemy == tag.ValueRO.PrevTarget)
                        {
                            continue;
                        }

                        randomEnemies.Add(enemy);
                        totalCount++;
                    }
                }

                enemies.Dispose();

                if (totalCount > 0)
                {
                    var idx = random.ValueRW.Value.NextInt(0, totalCount);
                    var target = randomEnemies[idx];
                    tag.ValueRW.CurrentTarget = target;
                }
                else
                {
                    //没有目标取上一个目标
                    tag.ValueRW.CurrentTarget = tag.ValueRO.PrevTarget;
                }

                randomEnemies.Dispose();
            }

            //朝目标移动
            float3 targetPos;
            if (tag.ValueRO.CurrentTarget != Entity.Null)
            {
                if (localToWorldLookup.TryGetComponent(tag.ValueRO.CurrentTarget, out var trans))
                {
                    //延迟转向
                    targetPos = trans.Position;
                }
                else
                {
                    //找不到Transform了，更换目标
                    tag.ValueRW.CurrentTarget = Entity.Null;
                    faceForward = float3.zero;
                    return false;
                }
            }
            else
            {
                //随机范围内的一个点
                var forward = math.normalizesafe(MathHelper.Angle2Forward(-90f));
                targetPos = (bHasParent ? parentPos : localTransform.ValueRO.Position) + forward * 1.5f;
            }

            //计算方向
            var targetDir = math.normalizesafe(targetPos - localTransform.ValueRO.Position);
            faceForward = math.lerp(tag.ValueRO.Dir, targetDir, deltaTime * turnSpeed);
            tag.ValueRW.Dir = faceForward;

            //更新位置
            var endPos = localTransform.ValueRO.Position + faceForward * deltaTime * speed;

            //统计移动距离, 触发技能Trigger
            MoveToPos(endPos, creatureMove, localTransform);

            if (math.distancesq(localTransform.ValueRO.Position, targetPos) < 0.5f)
            {
                //寻找新的目标
                tag.ValueRW.PrevTarget = tag.ValueRO.CurrentTarget;
                tag.ValueRW.CurrentTarget = Entity.Null;
            }

            return true;
        }

        private static bool MoveLR(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            float4 screenBound, EntityCommandBuffer.ParallelWriter ecb, int sortKey, out float3 forward, out float speed)
        {
            forward = float3.zero;
            speed = 0;

            var configSpeed = tag.ValueRO.Param1;
            var moveCount = (int)tag.ValueRO.Param2;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            var screenWidth = math.abs(screenBound.z - screenBound.x);

            //init
            if (!tag.ValueRO.Init)
            {
                tag.ValueRW.Init = true;
                tag.ValueRW.DistCounter = 0;

                //看在左侧还是右侧出生
                float centerX;
                if (math.abs(tag.ValueRO.BornPos.x - screenBound.x) < screenWidth / 2f)
                {
                    //在屏幕左边，往右移动
                    tag.ValueRW.MoveLeft = false;
                    centerX = tag.ValueRO.BornPos.x + screenWidth / 2f;
                }
                else
                {
                    //在屏幕右边，往左移动
                    tag.ValueRW.MoveLeft = true;
                    centerX = tag.ValueRO.BornPos.x - screenWidth / 2f;
                }

                tag.ValueRW.CenterPos = new float3(centerX, tag.ValueRO.BornPos.y, 0);
            }

            var moveDirX = tag.ValueRW.MoveLeft ? -1f : 1f;
            var targetPos = tag.ValueRO.CenterPos + new float3(moveDirX, 0, 0) * screenWidth / 2f;
            forward = math.normalizesafe(targetPos - localTransform.ValueRO.Position);

            var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;
            MoveToPos(moveTo, creatureMove, localTransform);

            if (math.distance(localTransform.ValueRO.Position, targetPos) < 0.2f)
            {
                //累计次数
                tag.ValueRW.DistCounter = tag.ValueRO.DistCounter + 1;
                tag.ValueRW.MoveLeft = !tag.ValueRO.MoveLeft;

                //超出次数销毁
                if (tag.ValueRO.DistCounter >= moveCount)
                {
                    ecb.SetComponent(sortKey, entity, new EnterDieTag { BanDrop = true });
                    ecb.SetComponentEnabled<EnterDieTag>(sortKey, entity, true);
                    return false;
                }
            }

            return true;
        }


        private static bool PatrolLR(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            ComponentLookup<MonsterTarget> monsterTargetLookup, out float3 forward, out float speed)
        {
            forward = float3.zero;
            speed = 0;

            if (!monsterTargetLookup.TryGetComponent(entity, out var monsterTarget) || !monsterTarget.HasTarget)
            {
                StopMove(agentBody, creatureMove);
                return false;
            }


            var configSpeed = tag.ValueRO.Param1;
            var maxDist = tag.ValueRO.Param2;
            var amplitude = tag.ValueRO.Param3;
            var frequency = tag.ValueRO.Param4;
            var localCenterPos = tag.ValueRO.CenterPos - tag.ValueRO.BornPos;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            //如果没有目标点，先设置目标点
            if (!tag.ValueRO.Init || tag.ValueRO.DistCounter >= maxDist)
            {
                tag.ValueRW.Init = true;
                tag.ValueRW.MoveLeft = !tag.ValueRW.MoveLeft;
                tag.ValueRW.DistCounter = 0;

                var dirX = !tag.ValueRW.MoveLeft ? -1f : 1f;
                var startPos = monsterTarget.Pos + new float3(dirX, 0, 0) * maxDist / 2f + localCenterPos;
                localTransform.ValueRW.Position = startPos;
            }

            var yOffset = math.sin(tag.ValueRO.Timer * frequency + math.PI / 2f) * amplitude;
            yOffset *= deltaTime;
            var moveDirX = tag.ValueRW.MoveLeft ? -1f : 1f;

            var targetPos = localTransform.ValueRO.Position + new float3(moveDirX * speed * deltaTime, yOffset, 0);
            forward = math.normalizesafe(targetPos - localTransform.ValueRO.Position);
            var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;
            MoveToPos(moveTo, creatureMove, localTransform);

            tag.ValueRW.DistCounter = tag.ValueRO.DistCounter + speed * deltaTime;
            tag.ValueRW.Timer = tag.ValueRO.Timer + deltaTime;

            //更新方向
            var moveDir = new float3(tag.ValueRO.MoveLeft ? -1 : 1, 0, 0);
            var lookTargetPos = localTransform.ValueRO.Position + moveDir * 5000;
            forward = math.normalizesafe(lookTargetPos - localTransform.ValueRO.Position);
            return true;
        }

        private static bool PatrolDir(float deltaTime, Entity entity,  float addSpeedFactor,
            RefRW<MonsterMove> tag, RefRW<StatusMove> creatureMove, RefRW<LocalTransform> localTransform,
            RefRW<AgentBody> agentBody, RefRW<AgentLocomotion> locomotion,
            ComponentLookup<MonsterTarget> monsterTargetLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey, out float3 forward, out float speed)
        {
            speed = 0f;
            forward = float3.zero;
            if (!monsterTargetLookup.TryGetComponent(entity, out var monsterTarget) || !monsterTarget.HasTarget)
            {
                StopMove(agentBody, creatureMove);
                return false;
            }

            var configSpeed = tag.ValueRO.Param1;
            var maxDist = tag.ValueRO.Param2;
            var loop = (int)tag.ValueRO.Param3 == 1;
            var endDestroy = (int)tag.ValueRO.Param4 == 1;
            var localCenterPos = tag.ValueRO.CenterPos - tag.ValueRO.BornPos;

            speed = configSpeed;
            speed = BuffHelper.CalcFactor2(speed, addSpeedFactor);

            creatureMove.ValueRW.MoveSpeedSource = configSpeed;
            creatureMove.ValueRW.MoveSpeedResult = speed;

            //如果没有目标点，先设置目标点
            if (!tag.ValueRO.Init)
            {
                tag.ValueRW.Init = true;

                var dir = math.normalizesafe(monsterTarget.Pos - (localTransform.ValueRO.Position + localCenterPos));
                tag.ValueRW.TargetPos = localTransform.ValueRO.Position + dir * maxDist;
            }

            var distSq = math.distancesq(localTransform.ValueRO.Position, tag.ValueRO.TargetPos);
            if (distSq > 0.25F)
            {
                forward = math.normalizesafe(tag.ValueRO.TargetPos - localTransform.ValueRO.Position);
                var moveTo = localTransform.ValueRO.Position + forward * speed * deltaTime;
                MoveToPos(moveTo, creatureMove, localTransform);
            }
            else
            {
                StopMove(agentBody, creatureMove);

                //是否结束销毁
                if (endDestroy)
                {
                    ecb.SetComponent(sortKey, entity, new EnterDieTag { BanDrop = true });
                    ecb.SetComponentEnabled<EnterDieTag>(sortKey, entity, true);
                    return false;
                }

                //看是否循环, 循环则重算目标点
                if (loop)
                {
                    var dir = math.normalizesafe(monsterTarget.Pos - (localTransform.ValueRO.Position + localCenterPos));
                    tag.ValueRW.TargetPos = localTransform.ValueRO.Position + dir * maxDist;
                }
            }

            forward = math.normalizesafe(tag.ValueRO.TargetPos - localTransform.ValueRO.Position);
            return true;
        }
    }
}

