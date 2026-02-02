using System;
using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletCheckMasterSystem))]
    public partial struct BulletMoveSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private ComponentLookup<BulletBombTag> _bombLookup;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        [ReadOnly] private BufferLookup<BulletTriggerBuffer> _bulletTriggerBuffer;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<StatusCenter> _centerLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _bombLookup = state.GetComponentLookup<BulletBombTag>(true);
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _bulletTriggerBuffer = state.GetBufferLookup<BulletTriggerBuffer>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _centerLookup = state.GetComponentLookup<StatusCenter>(true);
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

            _bombLookup.Update(ref state);
            _destroyLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _localToWorldLookup.Update(ref state);
            _bulletTriggerBuffer.Update(ref state);
            _cacheLookup.Update(ref state);
            _centerLookup.Update(ref state);

            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

            //直线运动
            new BulletMoveJob
            {
                DeltaTime = deltaTime,
                Ecb = ecb.AsParallelWriter(),
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                TransformLookup = _localToWorldLookup,
                DeadLookup = _deadLookup,
                BombLookup = _bombLookup,
                DestroyLookup = _destroyLookup,
                BulletTriggerBuffer = _bulletTriggerBuffer,
                CenterLookup = _centerLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct BulletMoveJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public Entity CacheEntity;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<BulletBombTag> BombLookup;
            [ReadOnly] public ComponentLookup<BulletDestroyTag> DestroyLookup;
            [ReadOnly] public BufferLookup<BulletTriggerBuffer> BulletTriggerBuffer;
            [ReadOnly] public ComponentLookup<StatusCenter> CenterLookup;

            [BurstCompile]
            private void Execute(RefRW<LocalTransform> localTransform, RefRW<BulletProperties> properties, RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetBulletConfig(properties.ValueRO.BulletId, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                if (BombLookup.IsComponentEnabled(entity) || DestroyLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                //技能追踪目标
                var bTraceSkillTarget = false;
                if (TransformLookup.TryGetComponent(properties.ValueRO.SkillTraceTarget, out var traceTrans))
                {
                    var traceDead = DeadLookup.HasComponent(properties.ValueRO.SkillTraceTarget) && DeadLookup.IsComponentEnabled(properties.ValueRO.SkillTraceTarget);
                    if (!traceDead)
                    {
                        bTraceSkillTarget = true;
                        
                        var angleToTarget = math.normalizesafe(traceTrans.Position - localTransform.ValueRO.Position);
                        properties.ValueRW.D1 = angleToTarget;

                        if (math.distance(traceTrans.Position, localTransform.ValueRO.Position) < 0.25f)
                        {
                            Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                            return;
                        }
                    }
                }
        
                //是否强制距离
                if (!bTraceSkillTarget && properties.ValueRO.ForceDistance > 0)
                {
                    if (properties.ValueRO.CurDist >= properties.ValueRO.ForceDistance)
                    {
                        Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                        return;
                    }
                }

                //判断是否为环绕子弹
                var aroundCenterPos = float3.zero;
                var bAround = false;
                if (MathHelper.IsValid(properties.ValueRO.AroundPos))
                {
                    aroundCenterPos = properties.ValueRO.AroundPos + properties.ValueRO.AroundCenterOffset;
                    bAround = true;
                }
                else if (TransformLookup.TryGetComponent(properties.ValueRO.AroundEntity, out var aroundEntityTrans))
                {
                    if (CenterLookup.TryGetComponent(properties.ValueRO.AroundEntity, out var aroundCreature))
                    {
                        aroundCenterPos = CreatureHelper.GetCenterPos(aroundEntityTrans.Position, aroundCreature, aroundEntityTrans.Value.Scale().x) + properties.ValueRO.AroundCenterOffset;
                    }
                    else
                    {
                        aroundCenterPos = aroundEntityTrans.Position + properties.ValueRO.AroundCenterOffset;
                    }
                    bAround = true;
                }

                //是否已到时间
                if (properties.ValueRO.ContTime > 0 && properties.ValueRO.RunningTime >= properties.ValueRO.ContTime)
                {
                    //直线，尝试弹射
                    var bHasBounce = false;
                    if (!bAround)
                    {
                        //达到最大飞行距离,反向180°随机
                        var newDir = MathHelper.RotateForward(-properties.ValueRO.RunningDirection, random.ValueRW.Value.NextFloat(-90f, 90f));
                        var bBounce = BulletHelper.TryBounce(newDir, localTransform.ValueRO.Position, properties);
                        if (bBounce)
                        {
                            //空地弹射后的子弹不要击退
                            properties.ValueRW.HitForce = 0;
                            bHasBounce = true;
                        }
                    }

                    if (!bHasBounce)
                    {
                        Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                    }

                    return;
                }

                //累加运行时间
                properties.ValueRW.RunningTime = properties.ValueRO.RunningTime + DeltaTime;

                //如果绑定了TransformParent
                if (TransformLookup.TryGetComponent(properties.ValueRO.TransformParent, out var transformParent))
                {
                    localTransform.ValueRW.Position = transformParent.Position;
                    return;
                }


                var startPos = localTransform.ValueRO.Position;
                var vOffset = CalcV1(properties, DeltaTime) + CalcV2(properties, DeltaTime);

                //环绕子弹路线预处理
                if (bAround)
                {
                    var reverse = properties.ValueRO.ReverseAroundDirection ? -1 : 1;
                    var aroundAngle = properties.ValueRO.AroundAngle + reverse * (math.radians(properties.ValueRO.VTurn) * math.PI * 360f * DeltaTime % 360f);
                    properties.ValueRW.AroundAngle = aroundAngle;
                    properties.ValueRW.VTurn = properties.ValueRO.VTurn + properties.ValueRO.ATurn * DeltaTime;

                    properties.ValueRW.AroundRadiusX += properties.ValueRO.AroundRadiusAddSpeed * DeltaTime;
                    properties.ValueRW.AroundRadiusY += properties.ValueRO.AroundRadiusAddSpeed * DeltaTime;

                    float3 aroundOffset;
                    if (Math.Abs(properties.ValueRO.AroundRadiusX - properties.ValueRO.AroundRadiusY) < 0.01f)
                    {
                        aroundOffset = MathHelper.GetCirclePos(aroundAngle, properties.ValueRO.AroundRadiusX);
                    }
                    else
                    {
                        aroundOffset = MathHelper.GetEllipsePosition(aroundAngle, properties.ValueRO.AroundRadiusX, properties.ValueRO.AroundRadiusY);
                    }

                    //累加环绕时间
                    properties.ValueRW.AroundTimer += DeltaTime;

                    //环绕中心偏移
                    properties.ValueRW.AroundCenterOffset += vOffset;

                    startPos = aroundCenterPos + aroundOffset;
                }

                var targetPos = startPos + vOffset;
                DoMove(entity, properties, localTransform, config, targetPos, DeltaTime, BulletTriggerBuffer, TransformLookup, Ecb, sortKey);

                //追踪目标设置(只有未反弹过的子弹会追踪）
                if (!properties.ValueRO.Reflected)
                {
                    if (properties.ValueRO.HeadingMethod != EHeadingMethod.None)
                    {
                        var targetDead = DeadLookup.HasComponent(properties.ValueRO.HeadingToTarget) && DeadLookup.IsComponentEnabled(properties.ValueRO.HeadingToTarget);
                        if (!targetDead && TransformLookup.TryGetComponent(properties.ValueRO.HeadingToTarget, out var targetTrans) && 
                            CenterLookup.TryGetComponent(properties.ValueRO.HeadingToTarget, out var headingCreature))
                        {
                            var tar =  CreatureHelper.GetCenterPos(targetTrans.Position, headingCreature, targetTrans.Value.Scale().x) + properties.ValueRO.HeadingOffset;
                            var angleToTarget = math.normalizesafe(tar - localTransform.ValueRO.Position);
                            properties.ValueRW.D1 = math.lerp(properties.ValueRO.D1, angleToTarget, properties.ValueRO.VTurn * DeltaTime);
                        }
                        else
                        {
                            if (properties.ValueRO.HeadingMethod is EHeadingMethod.NearEnemy or EHeadingMethod.RandomEnemy && properties.ValueRO.HeadBehaviourId > 0)
                            {
                                Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer
                                {
                                    BehaviourId = properties.ValueRO.HeadBehaviourId,
                                });
                                Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                            }
                        }
                    }
                }
            }

            private static void DoMove(Entity entity, RefRW<BulletProperties> properties, RefRW<LocalTransform> localTransform, BulletConfig config, float3 targetPos, float deltaTime,
                BufferLookup<BulletTriggerBuffer> bulletTriggerBuffer,ComponentLookup<LocalToWorld> transformLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
            {
                //do move
                var runningDir = math.normalizesafe(targetPos - localTransform.ValueRO.Position);
                if (MathHelper.IsValid(runningDir))
                {
                    properties.ValueRW.RunningDirection = runningDir;
                }

                var moveDist = math.distance(targetPos, localTransform.ValueRO.Position);

                //改变位置
                localTransform.ValueRW.Position = targetPos;

                //累加距离
                properties.ValueRW.CurDist = properties.ValueRO.CurDist + moveDist;

                //fly dist trigger
                if (bulletTriggerBuffer.HasBuffer(entity))
                {
                    ecb.AppendToBuffer(sortKey, entity, new BulletTriggerBuffer { FlyDist = moveDist, Type = EBulletBehaviourTrigger.OnFlyDist });
                }

                if (properties.ValueRO.HeadingAiming && transformLookup.TryGetComponent(properties.ValueRO.HeadingToTarget, out var headingTransform))
                {
                    var pos2 = headingTransform.Position;
                    var pos1 = localTransform.ValueRO.Position;
                    pos2.y = pos1.y;
                    var dir = math.normalizesafe(pos2 - pos1);
                    localTransform.ValueRW.Rotation = MathHelper.forward2RotationSafe(dir);
                }
                /*else if (!config.BanTurn)
                {
                    if (MathHelper.IsValid(properties.ValueRO.RunningDirection))
                    {
                        localTransform.ValueRW.Rotation = MathHelper.forward2RotationSafe(properties.ValueRO.RunningDirection);
                    }  
                }*/
             
                if (MathHelper.IsValid(properties.ValueRO.SelfRotate))
                {
                    var deltaEuler = math.radians(properties.ValueRO.SelfRotate * deltaTime);
                    var currentRotation = localTransform.ValueRO.Rotation;

                    var localX = math.mul(currentRotation, new float3(1, 0, 0));
                    var localY = math.mul(currentRotation, new float3(0, 1, 0));
                    var localZ = math.mul(currentRotation, new float3(0, 0, 1));

                    var rotX = quaternion.AxisAngle(localX, deltaEuler.x);
                    var rotY = quaternion.AxisAngle(localY, deltaEuler.y);
                    var rotZ = quaternion.AxisAngle(localZ, deltaEuler.z);

                    var deltaRotation = math.mul(rotZ, math.mul(rotY, rotX));
                    localTransform.ValueRW.Rotation = math.normalize(math.mul(deltaRotation, currentRotation));

                }
            }

            private static float3 CalcV1(RefRW<BulletProperties> properties, float deltaTime)
            {
                if (MathHelper.IsValid(properties.ValueRO.D1))
                {
                    if (math.abs(properties.ValueRO.V1) > 0 || math.abs(properties.ValueRO.A1) > 0)
                    {
                        var offset = properties.ValueRO.D1 * properties.ValueRO.V1 * deltaTime;
                        properties.ValueRW.V1 = properties.ValueRO.V1 + properties.ValueRO.A1 * deltaTime;
                        return offset;
                    }
                }

                return float3.zero;
            }

            private static float3 CalcV2(RefRW<BulletProperties> properties, float deltaTime)
            {
                if (MathHelper.IsValid(properties.ValueRO.D2))
                {
                    if (math.abs(properties.ValueRO.V2) > 0 || math.abs(properties.ValueRO.A2) > 0)
                    {
                        var offset = properties.ValueRO.D2 * properties.ValueRO.V2 * deltaTime;
                        properties.ValueRW.V2 = properties.ValueRO.V2 + properties.ValueRO.A2 * deltaTime;
                        return offset;
                    }
                }

                return float3.zero;
            }
        }
    }
}