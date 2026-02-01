using Deploys;
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
    [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletBehaviourTriggerSystem))]
    public partial struct BulletBehaviourActionSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private ComponentLookup<BulletBombTag> _bombLookup;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<DisableAutoTargetTag> _disableAutoTargetLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<LocalPlayerTag>();
            
            _bombLookup = state.GetComponentLookup<BulletBombTag>(true);
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>(true);
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _summonEntitiesLookup = state.GetBufferLookup<SummonEntities>(true);
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _disableAutoTargetLookup = state.GetComponentLookup<DisableAutoTargetTag>(true);
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
            _creatureTag.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _monsterLookup.Update(ref state);
            _summonEntitiesLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _disableAutoTargetLookup.Update(ref state);

            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            new BehaviourActionJob
            {
                Ecb = ecb.AsParallelWriter(),
                GlobalEntity = global.Entity,
                CacheEntity = cacheEntity,
                LocalPlayer = localPlayer,
                CollisionWorld = collisionWorld,
                TransformLookup = _transformLookup,
                CreatureTag = _creatureTag,
                DeadLookup = _deadLookup,
                CacheLookup = _cacheLookup,
                CreatureLookup = _creatureLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                SummonEntitiesLookup = _summonEntitiesLookup,
                MonsterLookup = _monsterLookup,
                SkillEntitiesLookup = _skillEntitiesLookup,
                SkillTagLookup = _skillTagLookup,
                DisableAutoTargetLookup = _disableAutoTargetLookup
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct BehaviourActionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity LocalPlayer;
            public Entity GlobalEntity;
            public Entity CacheEntity;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTag;
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public BufferLookup<SummonEntities> SummonEntitiesLookup;
            [ReadOnly] public ComponentLookup<MonsterProperties> MonsterLookup;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<DisableAutoTargetTag> DisableAutoTargetLookup;

            [BurstCompile]
            private void Execute(RefRW<BulletProperties> properties, RefRW<BulletAtkValue> atkValue, 
                DynamicBuffer<BulletBehaviourBuffer> behaviourBuffer,
                DynamicBuffer<BulletActionBuffer> actions, RefRW<BulletTriggerData> triggerData, 
                RefRW<RandomSeed> random, DynamicBuffer<BulletHitCreature> hitCreatures,
                RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetBulletConfig(properties.ValueRO.BulletId, CacheEntity, CacheLookup, out var bulletConfig))
                {
                    return;
                }

                for (var i = actions.Length - 1; i >= 0; i--)
                {
                    var info = actions[i];
                    actions.RemoveAt(i);

                    if (!CacheHelper.GetBulletBehaviourConfig(info.BehaviourId, CacheEntity, CacheLookup, out var config))
                    {
                        continue;
                    }

                    switch (config.Action)
                    {
                        //设置速度  
                        case EBulletBehaviourAction.SetVelocity:
                        {
                            var velocity = config.Param1;
                            var acc = config.Param2;
                            properties.ValueRW.V1 = velocity;
                            properties.ValueRW.A1 = acc;
                            break;
                        }
                        case EBulletBehaviourAction.SetVelocityRandom:
                        {
                            var velocityMin = config.Param1;
                            var velocityMax = config.Param2;
                            var accMin = config.Param3;
                            var accMax = config.Param4;
                            properties.ValueRW.V1 = random.ValueRW.Value.NextFloat(velocityMin, velocityMax);
                            properties.ValueRW.A1 = random.ValueRW.Value.NextFloat(accMin, accMax);
                            break;
                        }

                        case EBulletBehaviourAction.AroundRadiusSpeed:
                        {
                            var velocity = config.Param1;
                            properties.ValueRW.AroundRadiusAddSpeed = velocity;
                            break;
                        }
                        //设置ClockWise
                        case EBulletBehaviourAction.SetClockWise:
                        {
                            var turnSide = config.Param1.ToInt();
                            var speed = config.Param2;
                            var acc = config.Param3;
                            var radius = config.Param4;
                            var radiusAddSpeed = config.Param5;

                            properties.ValueRW.V1 = 0;
                            properties.ValueRW.A1 = 0;
                            properties.ValueRW.AroundAngle = MathHelper.Forward2Angle(properties.ValueRO.D1);
                            properties.ValueRW.AroundPos = localTransform.ValueRO.Position;
                            properties.ValueRW.VTurn = speed;
                            properties.ValueRW.ATurn = acc;
                            properties.ValueRW.AroundRadiusX = radius;
                            properties.ValueRW.AroundRadiusY = radius;
                            properties.ValueRW.ShootPos = localTransform.ValueRO.Position;
                            properties.ValueRW.ReverseAroundDirection = turnSide == 1;
                            properties.ValueRW.AroundRadiusAddSpeed = radiusAddSpeed;
                            properties.ValueRW.AroundCenterOffset = float3.zero;
                            break;
                        }
                        case EBulletBehaviourAction.AroundStartPos:
                        {
                            var turnSide = config.Param1.ToInt();
                            var speed = config.Param2;
                            var acc = config.Param3;
                            var radiusAcc = config.Param4;

                            var radius = math.distance(localTransform.ValueRO.Position, properties.ValueRO.ShootPos);
                            var dir = math.normalizesafe(localTransform.ValueRW.Position - properties.ValueRW.ShootPos);

                            properties.ValueRW.AroundAngle = MathHelper.Forward2Angle(dir);
                            properties.ValueRW.AroundPos = properties.ValueRO.ShootPos;
                            properties.ValueRW.VTurn = speed;
                            properties.ValueRW.ATurn = acc;
                            properties.ValueRW.AroundRadiusX = radius;
                            properties.ValueRW.AroundRadiusY = radius;
                            properties.ValueRW.ReverseAroundDirection = turnSide == 1;
                            properties.ValueRW.AroundRadiusAddSpeed = radiusAcc;

                            break;
                        }

                        case EBulletBehaviourAction.RemoveInterval:
                        {
                            for (var j = behaviourBuffer.Length - 1; j >= 0; j--)
                            {
                                var behaviour = behaviourBuffer[j];
                                if (CacheHelper.GetBulletBehaviourConfig(behaviour.Id, CacheEntity, CacheLookup, out var behaviourConfig))
                                {
                                    if (behaviourConfig.TriggerAction == EBulletBehaviourTrigger.Interval)
                                    {
                                        behaviourBuffer.RemoveAt(j);    
                                    }
                                }
                            }
                            break;
                        }
                        case EBulletBehaviourAction.ClearAiming:
                        {
                            var d1 = properties.ValueRW.D1;
                            d1.y = 0;
                            properties.ValueRW.D1 = d1;
                            properties.ValueRW.HeadingToTarget = Entity.Null;
                            properties.ValueRW.HeadingAiming = false;
                            properties.ValueRW.HeadingMethod = EHeadingMethod.None;
                            break;
                        }
                        case EBulletBehaviourAction.AimNearEnemy:
                        {
                            var searchRadius = config.Param1;
                            var bAim = config.Param2.ToInt() == 1;

                            //找到目标了
                            if (PhysicsHelper.GetNearestEnemy(properties.ValueRO.MasterCreature, CollisionWorld, localTransform.ValueRO.Position, searchRadius, CreatureTag, DeadLookup, DisableAutoTargetLookup,
                                    properties.ValueRO.Team, out var nearest, bulletConfig.HitSameTeam))
                            {
                                if (TransformLookup.TryGetComponent(nearest, out var enemyTrans) &&
                                    CreatureLookup.TryGetComponent(nearest, out var enemyCreature))
                                {
                                    //强制结束环绕
                                    properties.ValueRW.AroundPos = float3.zero;
                                    properties.ValueRW.AroundEntity = Entity.Null;

                                    var enemyPos = CreatureHelper.getCenterPos(enemyTrans.Position, enemyCreature, enemyTrans.Value.Scale().x);
                                    var direction = math.normalizesafe(enemyPos - localTransform.ValueRO.Position);
                                    properties.ValueRW.D1 = direction;

                                    if (bAim)
                                    {
                                        properties.ValueRW.HeadingToTarget = nearest;
                                        properties.ValueRW.HeadingAiming = true;
                                    }
                                }
                            }

                            break;
                        }
                        
                        //aimPlayer
                        case EBulletBehaviourAction.AimPlayer:
                        {
                            if (TransformLookup.TryGetComponent(LocalPlayer, out var playerTrans))
                            {
                                //强制结束环绕
                                properties.ValueRW.AroundPos = float3.zero;
                                properties.ValueRW.AroundEntity = Entity.Null;

                                var direction = math.normalizesafe(playerTrans.Position - localTransform.ValueRO.Position);
                                if (MathHelper.IsValid(direction))
                                {
                                    properties.ValueRW.D1 = direction;
                                }
                            }

                            break;
                        }

                        //禁止负数的速度
                        case EBulletBehaviourAction.SetDamageCount:
                        {
                            var value = config.Param1.ToInt();
                            properties.ValueRW.MaxDamageCount = value;
                            break;
                        }

                        case EBulletBehaviourAction.ResetDamageInterval:
                        {
                            hitCreatures.Clear();
                            break;
                        }

                        //禁止负数的速度
                        case EBulletBehaviourAction.SelfExplosion:
                        {
                            Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                            break;
                        }

                        case EBulletBehaviourAction.ClearHeading:
                        {
                            properties.ValueRW.HeadingToTarget = Entity.Null;
                            properties.ValueRW.VTurn = 0f;
                            properties.ValueRW.ATurn = 0f;
                            properties.ValueRW.HeadingMethod = EHeadingMethod.None;
                            properties.ValueRW.HeadBehaviourId = 0;
                            break;
                        }
                        case EBulletBehaviourAction.HeadingToAngleEnemy:
                        { 
                            var allowAngle = config.Param1.ToInt();
                            var searchRadius = config.Param2;
                            var turnSpeed = config.Param3;

                            var enemies = PhysicsHelper.OverlapEnemies(properties.ValueRO.MasterCreature, CollisionWorld, localTransform.ValueRO.Position, searchRadius, CreatureTag, DeadLookup, properties.ValueRO.Team, bulletConfig.HitSameTeam);
                            var minAngle = float.MaxValue;
                            var minAngleEnemy = Entity.Null;
                            var headingOffset = float3.zero;
                            foreach (var enemy in enemies)
                            {
                                if (TransformLookup.TryGetComponent(enemy, out var enemyTrans))
                                {
                                    var dirEnemy = math.normalize(enemyTrans.Position - localTransform.ValueRO.Position);
                                    var dir2 = properties.ValueRO.RunningDirection;
                                    
                                    // 忽略 Y 轴
                                    dirEnemy.y = 0;
                                    dir2.y = 0;
                                    
                                    // 计算夹角（弧度）
                                    var angleRad = math.acos(math.clamp(math.dot(dirEnemy, dir2), -1f, 1f));

                                    // 转换为角度
                                    var angleDeg = math.degrees(angleRad);
                                    if (angleDeg < allowAngle / 2f)
                                    {
                                        if (angleDeg < minAngle)
                                        {
                                            minAngleEnemy = enemy;
                                            minAngle = angleDeg;
                                            
                                            if (properties.ValueRO.Offset != 0)
                                            {
                                                var leftForward = MathHelper.RotateForward(dirEnemy, -90f);
                                                headingOffset = leftForward * properties.ValueRO.Offset;
                                            }
                                        }
                                    }
                                }
                            }

                            enemies.Dispose();

                            if (minAngleEnemy != Entity.Null)
                            {
                                properties.ValueRW.HeadingToTarget = minAngleEnemy;
                                properties.ValueRW.HeadingOffset = headingOffset;
                                properties.ValueRW.VTurn = turnSpeed;
                                properties.ValueRW.ATurn = 0f;

                                //需要强制取消子弹环绕
                                properties.ValueRW.AroundPos = float3.zero;
                                properties.ValueRW.AroundEntity = Entity.Null;
                            }
                            properties.ValueRW.HeadingMethod = EHeadingMethod.AngleEnemy;
                            properties.ValueRW.HeadBehaviourId = config.Id;
                            break;
                        }
                        //寻找附近最近的敌人，将其作为目标转过去
                        case EBulletBehaviourAction.HeadingToNearEnemy:
                        {
                            var searchRadius = config.Param1;
                            var turnSpeed = config.Param2;
                            var noTargetDestroy = config.Param3.ToInt() == 1;

                            //找到目标了
                            var bHasTarget = false;
                            if (PhysicsHelper.GetNearestEnemy(properties.ValueRO.MasterCreature, CollisionWorld, localTransform.ValueRO.Position, 
                                    searchRadius, CreatureTag, DeadLookup, DisableAutoTargetLookup,
                                    properties.ValueRO.Team, out var nearest, bulletConfig.HitSameTeam))
                            {
                                if (TransformLookup.HasComponent(nearest))
                                {
                                    properties.ValueRW.HeadingToTarget = nearest;
                                    properties.ValueRW.VTurn = turnSpeed;
                                    properties.ValueRW.ATurn = 0f;

                                    //需要强制取消子弹环绕
                                    properties.ValueRW.AroundPos = float3.zero;
                                    properties.ValueRW.AroundEntity = Entity.Null;

                                    bHasTarget = true;
                                }
                            }

                            if (!bHasTarget && noTargetDestroy)
                            {
                                Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                            }

                            properties.ValueRW.HeadingMethod = EHeadingMethod.NearEnemy;
                            properties.ValueRW.HeadBehaviourId = config.Id;
                            break;
                        }
                        case EBulletBehaviourAction.HeadingToRandomEnemy:
                        {
                            var searchRadius = config.Param1;
                            var turnSpeed = config.Param2;
                            var noTargetDestroy = config.Param3.ToInt() == 1;

                            //找到目标了
                            var bHasTarget = false;
                            var enemies = PhysicsHelper.OverlapEnemies(properties.ValueRO.MasterCreature, CollisionWorld, localTransform.ValueRO.Position, searchRadius, CreatureTag, DeadLookup,
                                properties.ValueRO.Team, bulletConfig.HitSameTeam);

                            var randomEnemies = new NativeList<Entity>(Allocator.Temp);
                            foreach (var enemy in enemies)
                            {
                                if (!TransformLookup.HasComponent(enemy))
                                {
                                    continue;
                                }

                                if (DisableAutoTargetLookup.HasComponent(enemy) && DisableAutoTargetLookup.IsComponentEnabled(enemy))
                                {
                                    continue;
                                }

                                randomEnemies.Add(enemy);
                            }

                            enemies.Dispose();

                            if (randomEnemies.Length > 0)
                            {
                                var hitEntity = randomEnemies[random.ValueRW.Value.NextInt(0, randomEnemies.Length)];
                                if (TransformLookup.HasComponent(hitEntity))
                                {
                                    properties.ValueRW.HeadingToTarget = hitEntity;
                                    properties.ValueRW.VTurn = turnSpeed;
                                    properties.ValueRW.ATurn = 0f;

                                    //需要强制取消子弹环绕
                                    properties.ValueRW.AroundPos = float3.zero;
                                    properties.ValueRW.AroundEntity = Entity.Null;
                                    bHasTarget = true;
                                }
                            }

                            randomEnemies.Dispose();

                            if (!bHasTarget && noTargetDestroy)
                            {
                                Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                            }

                            properties.ValueRW.HeadingMethod = EHeadingMethod.RandomEnemy;
                            properties.ValueRW.HeadBehaviourId = config.Id;
                            break;
                        }

                        //追踪玩家
                        case EBulletBehaviourAction.HeadingToMaster:
                        {
                            var turnSpeed = config.Param1;
                            properties.ValueRW.HeadingToTarget = properties.ValueRO.MasterCreature;
                            properties.ValueRW.VTurn = turnSpeed;
                            properties.ValueRW.ATurn = 0f;

                            //需要强制取消子弹环绕
                            properties.ValueRW.AroundPos = float3.zero;
                            properties.ValueRW.AroundEntity = Entity.Null;

                            properties.ValueRW.HeadingMethod = EHeadingMethod.Master;
                            properties.ValueRW.HeadBehaviourId = config.Id;
                            break;
                        }
                        //追踪自己的召唤物
                        case EBulletBehaviourAction.HeadingToSummon:
                        {
                            var monsterId = config.Param1.ToInt();
                            var turnSpeed = config.Param2;
                            var randomRadius = config.Param3.ToInt();
                            var findParent = config.Param4.ToInt() == 1;

                            var findEntity = properties.ValueRO.MasterCreature;
                            if (CreatureLookup.TryGetComponent(properties.ValueRO.MasterCreature, out var masterCreature))
                            {
                                if (findParent)
                                {
                                    findEntity = masterCreature.SummonParent;
                                }
                            }

                            var randList = new NativeList<Entity>(Allocator.TempJob);
                            if (SummonEntitiesLookup.TryGetBuffer(findEntity, out var summonBuffers))
                            {
                                foreach (var summonEntity in summonBuffers)
                                {
                                    if (MonsterLookup.TryGetComponent(summonEntity.Value, out var monster))
                                    {
                                        if (monster.Id == monsterId)
                                        {
                                            randList.Add(summonEntity.Value);
                                        }
                                    }
                                }
                            }

                            if (randList.Length > 0)
                            {
                                var result = randList[random.ValueRW.Value.NextInt(0, randList.Length)];
                                if (TransformLookup.HasComponent(result))
                                {
                                    properties.ValueRW.HeadingToTarget = result;
                                    properties.ValueRW.HeadingOffset = MathHelper.RandomRangePos(random, float3.zero, -randomRadius, randomRadius);
                                    properties.ValueRW.VTurn = turnSpeed;
                                    properties.ValueRW.ATurn = 0f;

                                    //需要强制取消子弹环绕
                                    properties.ValueRW.AroundPos = float3.zero;
                                    properties.ValueRW.AroundEntity = Entity.Null;
                                }
                            }

                            randList.Dispose();
                            
                            properties.ValueRW.HeadingMethod = EHeadingMethod.SummonMonster;
                            properties.ValueRW.HeadBehaviourId = config.Id;
                            break;
                        }
                        //增加伤害百分比
                        case EBulletBehaviourAction.AddDamage:
                        {
                            if (properties.ValueRO.From != EBulletFrom.Default)
                            {
                                Debug.LogError($"EBulletBehaviourAction.AddDamage error, 不支持的类型:{properties.ValueRO.From}");
                                break;
                            }
                            var addFactor = config.Param1;
                            triggerData.ValueRW.AtkFactor += addFactor;

                            var oldAtkValue = atkValue.ValueRO;
                            var atk = BuffHelper.CalcFactor(triggerData.ValueRO.SourceAtk, triggerData.ValueRO.AtkFactor);
                            atkValue.ValueRW = new BulletAtkValue
                            {
                                Atk = atk, 
                                Crit = oldAtkValue.Crit, 
                                CritDamage = oldAtkValue.CritDamage
                            };
                            break;
                        }
                        case EBulletBehaviourAction.AddScale:
                        {
                            var scaleFactor = config.Param1;
                            var maxLimit = config.Param2;

                            var targetScale = triggerData.ValueRO.ScaleFactor + scaleFactor;
                            if (maxLimit > 0 && targetScale > maxLimit)
                            {
                                targetScale = maxLimit;
                            }

                            triggerData.ValueRW.ScaleFactor = targetScale;
                            break;
                        }
                        case EBulletBehaviourAction.AddScaleTimer:
                        {
                            var speed = config.Param1;
                            var maxLimit = config.Param2;

                            Ecb.SetComponent(sortKey, entity, new BulletAddScale
                            {
                                Curr = 0,
                                Max = maxLimit,
                                Speed = speed
                            });
                            Ecb.SetComponentEnabled<BulletAddScale>(sortKey, entity, true);
                            break;
                        }
                        case EBulletBehaviourAction.ScreenBounce:
                        {
                            if (MathHelper.IsValid(info.HitNormal) == false)
                            {
                                Debug.LogError($"ScreenBounce HitNormal 无数据, id:{info.BehaviourId}");
                                break;
                            }

                            //强制退出环绕
                            properties.ValueRW.AroundEntity = Entity.Null;
                            properties.ValueRW.AroundPos = float3.zero;

                            var velocityForward = properties.ValueRO.V1 < 0 ? -1 : 1;
                            var bounceForward = MathHelper.ReflectSafe(properties.ValueRO.RunningDirection, info.HitNormal) * velocityForward;

                            //进行弹射
                            properties.ValueRW.CurrBounceCount = properties.ValueRO.CurrBounceCount + 1;
                            properties.ValueRW.D1 = bounceForward;
                            properties.ValueRW.RunningDirection = bounceForward;
                            properties.ValueRW.ShootPos = localTransform.ValueRO.Position;
                            properties.ValueRW.D2 = MathHelper.ReflectSafe(properties.ValueRW.D2, info.HitNormal);
                            properties.ValueRW.Reflected = true;
                            
                            //防止子弹不销毁
                            if (properties.ValueRO.ContTime <= 0)
                            {
                                Debug.LogError($"配置了速度的子弹, 但持续时间为0, 防止不销毁, 强制设置为5s, bulletId:{bulletConfig.Id}");
                                properties.ValueRW.ContTime = 5f;
                            }

                            //触发弹射技能Trigger
                            SkillHelper.DoSkillTrigger(properties.ValueRO.MasterCreature, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnBulletBounce)
                            {
                                Forward = bounceForward,
                                Pos = localTransform.ValueRO.Position,
                                IntValue1 = bulletConfig.Id,
                                IntValue2 = bulletConfig.ClassId,
                            }, Ecb, sortKey);
                            break;
                        }
                        case EBulletBehaviourAction.bh_Explosion:
                        {
                            var bulletId = config.Param1.ToInt();
                            var bulletBuffer = new BulletCreateBuffer
                            {
                                BulletId = bulletId,
                                ShootPos = localTransform.ValueRO.Position,
                                ParentCreature = properties.ValueRO.MasterCreature,
                                AtkValue = new AtkValue(atkValue.ValueRO.Atk, atkValue.ValueRO.Crit, atkValue.ValueRO.CritDamage, properties.ValueRO.Team),
                                SkillEntity = Entity.Null,
                                ImmediatelyBomb = true,
                                DisableSplit = true,
                            };
                            Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);
                            break;
                        }
                         case EBulletBehaviourAction.bh_ShootBulletRandomEnemy:
                        {
                            var bulletId = (int)config.Param1;
                            var min = config.Param2;
                            var max = config.Param3;
                            var addContTime = config.Param4;
                            
                            properties.ValueRW.ContTime += addContTime;
                        
                            if (bulletId <= 0)
                            {
                                Debug.LogError($"BulletBehaviour Action bh_ShootBulletRandomEnemy Error, bulletId <= 0");
                            }

                            var centerPos = localTransform.ValueRO.Position;
                            var enemies = PhysicsHelper.OverlapEnemies(properties.ValueRO.MasterCreature, CollisionWorld, centerPos, max, CreatureTag, DeadLookup, properties.ValueRO.Team);
                            var randomEnemies = new NativeList<Entity>(Allocator.Temp);
                            
                            for (var e = 0; e < enemies.Length; e++)
                            {
                                var enemy = enemies[e];
                                if (!TransformLookup.HasComponent(enemy))
                                {
                                    continue;
                                }

                                if (DisableAutoTargetLookup.HasComponent(enemy) && DisableAutoTargetLookup.IsComponentEnabled(enemy))
                                {
                                    continue;
                                }


                                var enemyPos = TransformLookup.GetRefRO(enemy).ValueRO.Position;
                                if (math.distancesq(enemyPos, centerPos) > min * min)
                                {
                                    randomEnemies.Add(enemy);
                                }
                            }
                            enemies.Dispose();

                            if (randomEnemies.Length > 0)
                            {
                                var idx = random.ValueRW.Value.NextInt(0, randomEnemies.Length);
                                var target = randomEnemies[idx];
                                if (TransformLookup.TryGetComponent(target, out var targetTrans))
                                {
                                    var targetPos = targetTrans.Position;
                                    targetPos.y = localTransform.ValueRO.Position.y;
                                    var dir = math.normalizesafe(targetPos- localTransform.ValueRO.Position);
                                    Ecb.AppendToBuffer(sortKey, properties.ValueRO.MasterCreature, new ShootBulletBuffer
                                    {
                                        BulletId = bulletId,
                                        Direction = dir,
                                        SkillEntity = Entity.Null,
                                        MaxContTime = 1,
                                        ShootInterval = 1,
                                        IntervalShootCount = 1,
                                        RotateAngle = 0,
                                        ForceShootPos = localTransform.ValueRO.Position,
                                    });
                                    Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, properties.ValueRO.MasterCreature, true);
                                }
                            }
                            break;
                        }
                        case EBulletBehaviourAction.bh_ShootBullet:
                        {
                            var bulletId = (int)config.Param1;
                            var count = (int)config.Param2;
                            var interval = config.Param3;
                            var rotateAngle = config.Param4;
                            var startAngle = config.Param5;
                            var useWorld = config.Param6.ToInt() == 1;
                            if (bulletId <= 0)
                            {
                                Debug.LogError($"BulletBehaviour Action ShootBullet Error, bulletId <= 0");
                            }

                            if (count <= 0)
                            {
                                count = 1;
                            }

                            if (startAngle <= -1f)
                            {
                                startAngle = random.ValueRW.Value.NextFloat(0f, 360f);
                            }

                            var contTime = interval * count;
                            var direction = useWorld ? MathHelper.Angle2Forward(startAngle) : MathHelper.RotateForward(properties.ValueRO.RunningDirection, startAngle);
                            var everyCount = contTime <= 0 ? count : 1; //如果无持续时间，则一次性射出所有子弹

                            Ecb.AppendToBuffer(sortKey, properties.ValueRO.MasterCreature, new ShootBulletBuffer
                            {
                                BulletId = bulletId,
                                Direction = direction,
                                SkillEntity = Entity.Null,
                                MaxContTime = contTime,
                                ShootInterval = interval,
                                IntervalShootCount = everyCount,
                                RotateAngle = rotateAngle,
                                ForceShootPos = localTransform.ValueRO.Position,
                            });
                            Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, properties.ValueRO.MasterCreature, true);
                            break;
                        }
                        case EBulletBehaviourAction.AddContTime:
                        {
                            var addFactor = config.Param1;
                            var addValue = config.Param2;

                            var contTime = properties.ValueRW.ContTime + addValue;
                            properties.ValueRW.ContTime = BuffHelper.CalcFactor(contTime, addFactor);
                            break;
                        }
                        case EBulletBehaviourAction.VelocityOffset:
                        {
                            var angle = config.Param1;
                            var speed = config.Param2;
                            var acc = config.Param3;
                            var useWorld = config.Param4.ToInt() == 1;

                            properties.ValueRW.D2 = useWorld ? MathHelper.Angle2Forward(angle) : MathHelper.RotateForward(properties.ValueRO.D1, angle);
                            properties.ValueRW.V2 = speed;
                            properties.ValueRW.A2 = acc;
                            break;
                        }
                        case EBulletBehaviourAction.ReverseDirection:
                        {
                            properties.ValueRW.D1 = -properties.ValueRO.D1;
                            break;
                        }
                        case EBulletBehaviourAction.AbsorbEnemy:
                        {
                            var radius = config.Param1;
                            var speed = config.Param2;
                            var contTime = config.Param3;
                           
                            if (contTime <= 0)
                            {
                                contTime = properties.ValueRO.ContTime - properties.ValueRO.RunningTime;
                                if (contTime <= 0)
                                {
                                    contTime = 0.5f;
                                    Debug.LogError("子弹行为AbsorbEnemy算不出持续时间, 强制设置个0.5秒");
                                }

                            }
                            else
                            {
                                contTime = BuffHelper.CalcContTime(contTime, properties.ValueRO.MasterCreature, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);    
                            }

                            var enemies = PhysicsHelper.OverlapEnemies(properties.ValueRO.MasterCreature, CollisionWorld, 
                                localTransform.ValueRO.Position, radius, CreatureTag, DeadLookup, properties.ValueRO.Team);
                            
                            for (var e = 0; e < enemies.Length; e++)
                            {
                                Ecb.SetComponentEnabled<CreatureAbsorbTag>(sortKey, enemies[e], true);
                                Ecb.SetComponent(sortKey, enemies[e], new CreatureAbsorbTag
                                {
                                    Speed = speed,
                                    Target = localTransform.ValueRO.Position,
                                    DestroyDelay = contTime
                                });
                            }

                            enemies.Dispose();
                            break;
                        }
                        case EBulletBehaviourAction.SetAroundEntityToPlayer:
                        {
                            var offsetY = config.Param1;
                            properties.ValueRW.AroundEntity = LocalPlayer;
                            properties.ValueRW.AroundPos = float3.zero;
                            properties.ValueRW.AroundCenterOffset = new float3(0, offsetY, 0);
                            break;
                        }
                        //子弹将敌人击飞（用pos改变机制)
                        case EBulletBehaviourAction.RepelEnemyFly:
                        {
                            var distance = config.Param1;
                            var maxScale = config.Param2;
                            var time = config.Param3;
                            var cd = config.Param4;
                            if (time <= 0)
                            {
                                time = 0.2f;
                            }

                            Ecb.SetComponent(sortKey, entity, new BulletRepel
                            {
                                Distance = distance,
                                MaxScale = maxScale,
                                Time = time,
                                Cd = cd,
                            });
                            Ecb.SetComponentEnabled<BulletRepel>(sortKey, entity, true);
                            break;
                        }
                        case EBulletBehaviourAction.RotateSelf:
                        {
                            var x = config.Param1;
                            var y = config.Param2;
                            var z = config.Param3;
                            properties.ValueRW.SelfRotate = new float3(x, y, z);
                            break;
                        }
                    }
                }

                Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, false);
            }
        }
    }
}