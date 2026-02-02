using Deploys;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Dots
{
    public static class CreatureHelper
    {
        public static void InitAsCreature(ECreatureType type, float centerY, EElement belongElement, Random randomSeed, ETeamId teamId, FightProps props, float moveSpeed,
            EntityCommandBuffer ecb, Entity entity, float originScale, Entity summonParent, int selfSummonId, float hpPercent = 1f, float atkFactor = 1f)
        {
            //标记为Creature
            var atkValue = new AtkValue(props.Atk * atkFactor, props.Crit, props.CritDamage, teamId);
            ecb.AddComponent(entity, new CreatureTag
            {
                Type = type,
                TeamId = atkValue.Team,
                BelongElementId = belongElement,
            });

            ecb.AddComponent(entity, new CreatureProps
            {
                AtkValue = atkValue,
                RepelCd = props.RepelCd,
                Def = props.Def,
                SpeedFac = props.SpeedFac,
                OriginScale = originScale,
            });
            
            ecb.AddComponent(entity, new StatusCenter
            {
                CenterY = centerY
            });
            
            ecb.AddComponent(entity, new StatusHp
            {
                FullHp = props.Hp,
                CurHp = props.Hp * hpPercent,
            });
            
            ecb.AddComponent(entity, new StatusSummon
            {
                SummonParent = summonParent,
                SelfConfigId = selfSummonId,
                SelfType = type,
            });
            
            ecb.AddComponent(entity, new StatusMove
            {
                MoveSpeedSource = moveSpeed,
                MoveSpeedResult = moveSpeed,
            });

            ecb.AddComponent(entity, new StatusForward
            {
                FaceForward = new float3(0, 0, 1),
                MoveForward = new float3(1, 0, 0),
            });

            ecb.AddComponent(entity, new CreatureMuzzlePos
            {
                Value = float3.zero
            });

            ecb.AddComponent(entity, new CreatureFps
            {
                FpsFactor = 1f,
                FpsFactorZero = false,
            });

            ecb.AddBuffer<SummonEntities>(entity);
            ecb.AddBuffer<SkillEntities>(entity);
            ecb.AddBuffer<BuffEntities>(entity);
            ecb.AddBuffer<BindingBullet>(entity);

            ecb.AddComponent<ShieldProperties>(entity);
            ecb.SetComponentEnabled<ShieldProperties>(entity, false);
            
            //被击闪红模块
            ecb.AddComponent(entity, new MaterialBlend { Color = new float4(1f, 1f, 1f, 1f), Value = 0 });
            ecb.AddComponent(entity, new StatusColor
            {
                Color = new float4(0, 0, 0, 0),
                Alpha = 0f,
            });

            //随机种子
            ecb.AddComponent(entity, new RandomSeed { Value = randomSeed, });
            ecb.AddComponent(entity, new BindingElement());

            //技能 shoot bullet
            ecb.AddBuffer<ShootBulletBuffer>(entity);
            ecb.SetComponentEnabled<ShootBulletBuffer>(entity, false);

            //伤害流
            ecb.AddBuffer<DamageBuffer>(entity);
            ecb.SetComponentEnabled<DamageBuffer>(entity, false);

            //伤害数字缓存设置
            ecb.AddBuffer<DamageNumberBuffer>(entity);

            ecb.AddComponent<InAttackState>(entity);
            ecb.SetComponentEnabled<InAttackState>(entity, false);

            //状态处理
            ecb.AddBuffer<CreatureDataProcess>(entity);
            ecb.SetComponentEnabled<CreatureDataProcess>(entity, false);

            //被击CD
            ecb.AddComponent<CreatureRepelCd>(entity);
            ecb.SetComponentEnabled<CreatureRepelCd>(entity, false);

            ecb.AddComponent<DashStartTag>(entity);
            ecb.SetComponentEnabled<DashStartTag>(entity, false);

            ecb.AddComponent<DashFollowLineTag>(entity);
            ecb.SetComponentEnabled<DashFollowLineTag>(entity, false);

            ecb.AddComponent<EnterDieTag>(entity);
            ecb.SetComponentEnabled<EnterDieTag>(entity, false);

            ecb.AddComponent<EnterHitTag>(entity);
            ecb.SetComponentEnabled<EnterHitTag>(entity, false);

            ecb.AddComponent<InDeadState>(entity);
            ecb.SetComponentEnabled<InDeadState>(entity, false);

            ecb.AddComponent<CollisionDamageCdTag>(entity);
            ecb.SetComponentEnabled<CollisionDamageCdTag>(entity, false);

            ecb.AddComponent<CreatureRepelStart>(entity);
            ecb.SetComponentEnabled<CreatureRepelStart>(entity, false);

            ecb.AddComponent<CreatureRepelPosition>(entity);
            ecb.SetComponentEnabled<CreatureRepelPosition>(entity, false);

            ecb.AddComponent<DisableHurtTag>(entity);
            ecb.SetComponentEnabled<DisableHurtTag>(entity, false);

            ecb.AddComponent<DisableBulletHitTag>(entity);
            ecb.SetComponentEnabled<DisableBulletHitTag>(entity, false);

            //禁止被设置为自动瞄准目标
            ecb.AddComponent<DisableAutoTargetTag>(entity);
            ecb.SetComponentEnabled<DisableAutoTargetTag>(entity, false);

            //禁止buff
            ecb.AddComponent<DisableBuffTag>(entity);
            ecb.SetComponentEnabled<DisableBuffTag>(entity, false);

            ecb.AddComponent<EnterFreezeTag>(entity);
            ecb.SetComponentEnabled<EnterFreezeTag>(entity, false);

            ecb.AddComponent<RemoveFreezeTag>(entity);
            ecb.SetComponentEnabled<RemoveFreezeTag>(entity, false);

            ecb.AddComponent<InFreezeState>(entity);
            ecb.SetComponentEnabled<InFreezeState>(entity, false);

            ecb.AddComponent<InBornTag>(entity);
            ecb.SetComponentEnabled<InBornTag>(entity, false);

            ecb.AddComponent<InRepelState>(entity);
            ecb.SetComponentEnabled<InRepelState>(entity, false);

            ecb.AddComponent<CreatureAbsorbTag>(entity);
            ecb.SetComponentEnabled<CreatureAbsorbTag>(entity, false);

            ecb.AddComponent<CreatureForceTargetTag>(entity);
            ecb.SetComponentEnabled<CreatureForceTargetTag>(entity, false);

            ecb.AddComponent<InDashingTag>(entity);
            ecb.SetComponentEnabled<InDashingTag>(entity, false);

            ecb.AddComponent<DisableMoveTag>(entity);
            ecb.SetComponentEnabled<DisableMoveTag>(entity, false);

            ecb.AddComponent<EnterReviveTag>(entity);
            ecb.SetComponentEnabled<EnterReviveTag>(entity, false);

            //hybrid events
            ecb.AddComponent<HybridEvent_SetActive>(entity);
            ecb.SetComponentEnabled<HybridEvent_SetActive>(entity, false);

            ecb.AddComponent<HybridEvent_SetWeaponActive>(entity);
            ecb.SetComponentEnabled<HybridEvent_SetWeaponActive>(entity, false);

            ecb.AddComponent<HybridEvent_PlayAnimation>(entity);
            ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(entity, false);

            ecb.AddComponent<HybridEvent_PlayMuzzleEffect>(entity);
            ecb.SetComponentEnabled<HybridEvent_PlayMuzzleEffect>(entity, false);
            
            ecb.AddComponent<HybridEvent_StopMuzzleEffect>(entity);
            ecb.SetComponentEnabled<HybridEvent_StopMuzzleEffect>(entity, false);
        }


        public static float GetMoveSpeedFactor(Entity entity, 
            ComponentLookup<StatusSummon> summonLookup,
            ComponentLookup<CreatureProps> propLookup, 
            ComponentLookup<ShieldProperties> shieldLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup,
            ComponentLookup<BuffTag> buffTagLookup, 
            ComponentLookup<BuffCommonData> buffCommonLookup,
            ComponentLookup<PlayerAttrData> attrLookup, 
            BufferLookup<PlayerAttrModify> attrModifyLookup)
        {
            var addFactor = 0f;

            //base
            if (propLookup.TryGetComponent(entity, out var creature) && creature.SpeedFac != 0)
            {
                addFactor = creature.SpeedFac;
            }
             
            //buff
            var hasShield = CheckHasShield(entity, shieldLookup);
            {
                var resSpeed = BuffHelper.GetBuffFactorAndValue(entity, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.MoveSpeed, checkBool: hasShield);
                addFactor += resSpeed.AddFactor;
            }
            
            //attr
            addFactor += AttrHelper.GetAttr(entity, EAttr.MoveSpeed, attrLookup, attrModifyLookup, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);

            return addFactor;
        }

        //怪物阵型：椭圆
        public static void SpawnFormationOval(CollisionWorld world, GlobalAspect global, CacheAspect cache, int monsterId, SpawnMonsterProperties spawn, FightProps props, ETeamId team, float3 centerPos, SpawnMonsterFormation data, EntityCommandBuffer ecb)
        {
            var formationMinDist = math.min(data.Width, data.Height);

            //随机一个方向
            for (var angle = 0f; angle < 360f; angle += data.SplitInterval)
            {
                var pos = centerPos + MathHelper.GetEllipsePosition(angle, data.Width, data.Height);
                SpawnFormationMonster(world, global, cache, monsterId, spawn, props, team, pos, centerPos, formationMinDist, data, ecb);
            }
        }

        //怪物阵型：矩形
        public static void SpawnFormationRect(CollisionWorld world, GlobalAspect global, CacheAspect cache, int monsterId, SpawnMonsterProperties spawn, FightProps props, ETeamId team,
            float3 centerPos, SpawnMonsterFormation data, EntityCommandBuffer ecb, bool allowVert = true, bool allowHoriz = true)
        {
            var startX = centerPos.x - data.Width / 2f;
            var startY = centerPos.y - data.Height / 2f;
            var endX = centerPos.x + data.Width / 2f;
            var endY = centerPos.y + data.Height / 2f;

            var formationMinDist = math.min(data.Width, data.Height);

            if (allowVert)
            {
                //下
                for (var x = startX; x <= endX; x += data.SplitInterval)
                {
                    var pos = new float3(x, startY - data.SplitInterval / 2f, 0);
                    SpawnFormationMonster(world, global, cache, monsterId, spawn, props, team, pos, centerPos, formationMinDist, data, ecb);
                }

                //上
                for (var x = startX; x <= endX; x += data.SplitInterval)
                {
                    var pos = new float3(x, endY + data.SplitInterval / 2f, 0);
                    SpawnFormationMonster(world, global, cache, monsterId, spawn, props, team, pos, centerPos, formationMinDist, data, ecb);
                }
            }

            if (allowHoriz)
            {
                //左
                for (var y = startY; y <= endY; y += data.SplitInterval)
                {
                    var pos = new float3(startX - data.SplitInterval / 2f, y, 0);
                    SpawnFormationMonster(world, global, cache, monsterId, spawn, props, team, pos, centerPos, formationMinDist, data, ecb);
                }

                //右
                for (var y = startY; y <= endY; y += data.SplitInterval)
                {
                    var pos = new float3(endX + data.SplitInterval / 2f, y, 0);
                    SpawnFormationMonster(world, global, cache, monsterId, spawn, props, team, pos, centerPos, formationMinDist, data, ecb);
                }
            }
        }

        private static void SpawnFormationMonster(CollisionWorld world, GlobalAspect global, CacheAspect cache, int monsterId, SpawnMonsterProperties spawn, FightProps props, ETeamId team,
            float3 bornPos, float3 centerPos, float formationMinDist, SpawnMonsterFormation data, EntityCommandBuffer ecb)
        {
            FactoryHelper.CreateMonster(world, global, cache, monsterId, spawn, Entity.Null, props, team, bornPos, centerPos, ecb, data.DelayDestroy, formationMinDist: formationMinDist);
        }

        public static int GetFormationMonsterCount(ESpawnShape shape, float splitInterval, float width, float height)
        {
            var totalCount = 0;
            switch (shape)
            {
                case ESpawnShape.Ellipse:
                    totalCount = (int)(360f / splitInterval);
                    break;
                case ESpawnShape.Rectangle:
                    totalCount = (int)(width / splitInterval) * 2 + (int)(height / splitInterval) * 2;
                    break;
                case ESpawnShape.Horiz:
                    totalCount = (int)(height / splitInterval) * 2;
                    break;
                case ESpawnShape.Vert:
                    totalCount = (int)(width / splitInterval) * 2;
                    break;
            }

            return totalCount;
        }

        //根据Shape获得随机点的位置
        public static float3 GetRandomShapePosition(ESpawnShape shape, float3 startPosition, RefRW<RandomSeed> random, float width, float height, float randomRange)
        {
            float3 bornPos;
            switch (shape)
            {
                case ESpawnShape.Ellipse:
                {
                    bornPos = RandomEllipsePosition(random, startPosition, width, height, randomRange);
                    break;
                }
                case ESpawnShape.Rectangle:
                {
                    bornPos = RandomRectanglePosition(random, startPosition, width, height, randomRange);
                    break;
                }
                case ESpawnShape.Horiz:
                {
                    bornPos = RandomHorizPosition(random, startPosition, width, height, randomRange);
                    break;
                }
                case ESpawnShape.Vert:
                {
                    bornPos = RandomVertPosition(random, startPosition, width, height, randomRange);
                    break;
                }
                case ESpawnShape.Top:
                {
                    bornPos = RandomScreenTopPosition(random, startPosition, width, height, randomRange);
                    break;  
                }
                default:
                {
                    Debug.LogError($"刷怪失败, 获取出生点，不支持的Shape类型:{shape}");
                    bornPos = startPosition;
                    break;
                }
            }

            return bornPos;
        }

        //随机椭圆上的点
        private static float3 RandomEllipsePosition(RefRW<RandomSeed> random, float3 pos, float width, float height, float randomRange)
        {
            var randomAngle = random.ValueRW.Value.NextFloat(0f, 360f);
            var result = pos + MathHelper.GetEllipsePosition(randomAngle, width, height);
            return MathHelper.RandomRangePos(random, result, 0, randomRange);
        }

        //随机左右两边的点
        private static float3 RandomHorizPosition(RefRW<RandomSeed> random, float3 pos, float width, float height, float randomRange)
        {
            var lrFlag = random.ValueRW.Value.NextFloat(0f, 1f) < 0.5f ? -1 : 1;
            var x = lrFlag * width / 2;
            var y = random.ValueRW.Value.NextFloat(-height / 2f, height / 2f);
            var result = pos + new float3(x, y, 0);
            return MathHelper.RandomRangePos(random, result, 0, randomRange);
        }

        //随机上下两边的点
        private static float3 RandomVertPosition(RefRW<RandomSeed> random, float3 pos, float width, float height, float randomRange)
        {
            var udFlag = random.ValueRW.Value.NextFloat(0f, 1f) < 0.5f ? -1 : 1;
            var x = random.ValueRW.Value.NextFloat(-width / 2f, width / 2f);
            var y = udFlag * height / 2;
            var result = pos + new float3(x, y, 0);
            return MathHelper.RandomRangePos(random, result, 0, randomRange);
        }

        //随机屏幕上方
        private static float3 RandomScreenTopPosition(RefRW<RandomSeed> random, float3 pos, float width, float height, float randomRange)
        {
            var x = random.ValueRW.Value.NextFloat(-width / 2f, width / 2f);
            var result = pos + new float3(x, 0, height);
            return result + MathHelper.RandomRange(random, randomRange);
        }
        
        //随机矩形边框上的点
        private static float3 RandomRectanglePosition(RefRW<RandomSeed> random, float3 pos, float width, float height, float randomRange)
        {
            //看用垂直还是左右
            if (random.ValueRW.Value.NextFloat(0f, 1f) < 0.5f)
            {
                return RandomHorizPosition(random, pos, width, height, randomRange);
            }

            return RandomVertPosition(random, pos, width, height, randomRange);
        }

        public static void UpdateFaceForward(Entity entity, float3 dir, ComponentLookup<InDashingTag> inDashLookup, EntityCommandBuffer ecb)
        {
            //看是否允许改变方向(改变左右转向)
            if (inDashLookup.HasComponent(entity) && inDashLookup.IsComponentEnabled(entity))
            {
                return;
            }

            ecb.SetComponent(entity, new StatusForward
            {
                FaceForward = dir,
                MoveForward = dir,
            });
        }

        public static void UpdateFaceForward(Entity entity, float3 dir, ComponentLookup<InDashingTag> inDashLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            //看是否允许改变方向(改变左右转向)
            if (inDashLookup.HasComponent(entity) && inDashLookup.IsComponentEnabled(entity))
            {
                return;
            }

            ecb.SetComponent(sortKey, entity, new StatusForward
            {
                FaceForward = dir,
                MoveForward = dir,
            });
        }

        public static void CountMoveDist(Entity entity, RefRW<StatusMove> creatureMove, float moveDist, BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup, EntityCommandBuffer ecb)
        {
            //每移动1M的技能触发
            creatureMove.ValueRW.MoveDistTemp = creatureMove.ValueRO.MoveDistTemp + moveDist;

            if (creatureMove.ValueRO.MoveDistTemp >= 1f)
            {
                creatureMove.ValueRW.MoveDistTemp = 0f;

                //SkillTrigger
                SkillHelper.DoSkillTrigger(entity, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnMove1M), ecb);
            }
        }

        public static void CountMoveDist(Entity entity, RefRW<StatusMove> creatureMove, float moveDist, BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup,
            EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            //每移动1M的技能触发
            creatureMove.ValueRW.MoveDistTemp = creatureMove.ValueRO.MoveDistTemp + moveDist;

            if (creatureMove.ValueRO.MoveDistTemp >= 1f)
            {
                creatureMove.ValueRW.MoveDistTemp = 0f;

                //SkillTrigger
                SkillHelper.DoSkillTrigger(entity, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnMove1M), ecb, sortKey);
            }
        }


        public static bool CheckHasShield(Entity master, ComponentLookup<ShieldProperties> shieldLookup)
        {
            if (shieldLookup.TryGetComponent(master, out var shield) && shieldLookup.IsComponentEnabled(master))
            {
                if (shield.Value > 0)
                {
                    return true;
                }
            }

            return false;
        }

        //自动检测是否超出地图范围，并修正坐标
        public static float3 ResetPositionByMapAllow(RefRW<RandomSeed> random, float3 pos, float2 mapAllowHoriz, float2 mapAllowVert)
        {
            var fixRange = 2f;

            //宽度是否越界
            var bOver = false;
            if (pos.x < mapAllowHoriz.x)
            {
                pos.x = mapAllowHoriz.x + fixRange;
                bOver = true;
            }

            if (pos.x > mapAllowHoriz.y)
            {
                pos.x = mapAllowHoriz.x - fixRange;
                bOver = true;
            }

            //高度是否越界
            if (pos.y < mapAllowVert.x)
            {
                pos.y = mapAllowVert.x + fixRange;
                bOver = true;
            }

            if (pos.y > mapAllowVert.y)
            {
                pos.y = mapAllowVert.y - fixRange;
                bOver = true;
            }

            if (bOver)
            {
                pos = MathHelper.RandomRangePos(random, pos, 0, fixRange);
            }

            return pos;
        }

        public static void ProcessMasterActive(bool bActive, Entity entity, ComponentLookup<HybridEvent_SetActive> eventSetActive, EntityCommandBuffer ecb)
        {
            if (bActive)
            {
                if (eventSetActive.HasComponent(entity))
                {
                    ecb.SetComponent(entity, new HybridEvent_SetActive { Value = true });
                    ecb.SetComponentEnabled<HybridEvent_SetActive>(entity, true);
                }

                //PhysicsHelper.EnablePhysics(entity, cache, creature, velocityLookup, ecb);
                ecb.SetComponentEnabled<DisableAutoTargetTag>(entity, false);
                ecb.SetComponentEnabled<DisableHurtTag>(entity, false);
            }
            else
            {
                if (eventSetActive.HasComponent(entity))
                {
                    ecb.SetComponent(entity, new HybridEvent_SetActive { Value = false });
                    ecb.SetComponentEnabled<HybridEvent_SetActive>(entity, true);
                }

                //PhysicsHelper.DisablePhysics(entity, cache, creature, velocityLookup, ecb);
                ecb.SetComponentEnabled<DisableAutoTargetTag>(entity, true);
                ecb.SetComponentEnabled<DisableHurtTag>(entity, true);
            }
        }

        //处理全局风对坐标的影响
        public static void CalcWindPos(Entity localPlayer, Entity creatureEntity, float deltaTime, 
            RefRW<LocalTransform> localTransform, 
            ComponentLookup<CreatureTag> creatureLookup,
            ComponentLookup<StatusSummon> summonLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, 
            ComponentLookup<BuffTag> buffTagLookup, 
            ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            if (creatureLookup.TryGetComponent(creatureEntity, out var creature))
            {
                if (creature.Type == ECreatureType.Boss)
                {
                    return;
                }
            }

            //风速影响
            var windData = BuffHelper.GetBuffFactorAndValue(localPlayer, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.MissionBuff_Wind);
            if (windData.TempData != 0)
            {
                var windForward = MathHelper.Angle2Forward(windData.TempData);
                var posOffset = windData.AddFactor * windForward * deltaTime;
                localTransform.ValueRW.Position += posOffset;
            }
        }

        public static float3 GetCenterPos(float3 pos, StatusCenter center, float scale)
        {
            pos.y += center.CenterY * scale;
            return pos;
        }
        
        public static float3 GetHeadPos(float3 pos, StatusCenter center, float scale)
        {
            pos.y += center.CenterY * 2 * scale;
            return pos;
        }
        
        public static Entity GetMasterRoot(Entity entity, ComponentLookup<StatusSummon> summonLookup)
        {
            //往上找召唤者，任意一个召唤者拥有，则都计算
            var findEntity = entity;
            while (summonLookup.TryGetComponent(findEntity, out var creature) && summonLookup.HasComponent(creature.SummonParent))
            {
                findEntity = creature.SummonParent;
            } 
            return findEntity;
        }

        public static float3 GetServantBornPos(LocalToWorld playerTrans, GlobalAspect global, int idx, Entity mainServantEntity,
            ComponentLookup<LocalTransform> transformLookup, ComponentLookup<StatusForward> forwardLookup)
        {
            //召唤英灵
            var bornPos = playerTrans.Position;
            bornPos.z += 5;
                        
            if (global.GetSubServantPos(idx, out var targetPos))
            {
                bornPos = targetPos;
            }
            else
            {
                
                if (transformLookup.TryGetComponent(mainServantEntity, out var mainServantPos)
                    && forwardLookup.TryGetComponent(mainServantEntity, out var mainServantForward))
                {
                    bornPos = mainServantPos.Position - mainServantForward.MoveForward * 1.5f * idx;
                }
            }
            return bornPos;
        }
        
        public static void UnbindServant(DynamicBuffer<ServantList> servantLists, GlobalAspect global, Entity entity, ComponentLookup<LocalTransform> transformLookup,
            ComponentLookup<ServantProperties> servantLookup, ComponentLookup<MainServantTag> mainServantLookup, 
            BufferLookup<Child> childLookup, ComponentLookup<EffectProperties> effectLookup, BufferLookup<BindingBullet> bindingBulletLookup,
            ComponentLookup<BulletProperties> bulletLookup, EntityCommandBuffer ecb, float delayDestroy)
        {
            if (!servantLookup.TryGetComponent(entity, out var unbindServant))
            {
                return;
            }

            if (!transformLookup.TryGetComponent(entity, out var unbindTransform))
            {
                return;
            }
            
            var mainServantDead = false;
            if (mainServantLookup.HasComponent(entity))
            {
                mainServantDead = true;
                ecb.RemoveComponent<MainServantTag>(entity);  
            }
            
            ecb.RemoveComponent<ServantProperties>(entity);
            ecb.SetComponent(entity, new ServantDestroyTag
            {
                Timer = 0,
                DestroyDelay = delayDestroy,
            });
            ecb.SetComponentEnabled<ServantDestroyTag>(entity, true);

            //remove all effect
            FactoryHelper.RemoveAllEffect(entity, global.Entity, childLookup, effectLookup, ecb);

            //remove bind bullet
            BulletHelper.ClearAllBindingBullet(entity, bindingBulletLookup, bulletLookup, ecb);
            
            for (var i = servantLists.Length - 1; i >= 0; i--)
            {
                var buffer = servantLists[i];
                if (buffer.Value == entity)
                {
                    servantLists.RemoveAt(i);
                }
                 
                if (servantLookup.TryGetComponent(buffer.Value, out var servantProperties))
                {
                    if (servantProperties.Idx > unbindServant.Idx)
                    {
                        var afterIdx = servantProperties.Idx - 1;
                        servantLookup.GetRefRW(buffer.Value).ValueRW.Idx = afterIdx;
                        if (mainServantDead && afterIdx == 0)
                        {
                            if (transformLookup.TryGetComponent(buffer.Value, out var trans))
                            {
                                ecb.SetComponent(buffer.Value, new LocalTransform
                                {
                                    Scale = trans.Scale,
                                    Position = unbindTransform.Position,
                                    Rotation = trans.Rotation
                                });
                            }
                            
                            ecb.AddComponent(buffer.Value, new MainServantTag
                            {
                                initTempPos = unbindTransform.Position,
                            });

                            //拾取buffer
                            ecb.AddBuffer<PickupBuffer>(buffer.Value);
                            ecb.SetComponentEnabled<PickupBuffer>(buffer.Value, false);

                            ecb.AddComponent<PickupMagnetTag>(buffer.Value);
                            ecb.SetComponentEnabled<PickupMagnetTag>(buffer.Value, false);

                            ecb.AddComponent<PickupSkillTag>(buffer.Value);
                            ecb.SetComponentEnabled<PickupSkillTag>(buffer.Value, false);
                        }
                    }
                }
            }
        }
    }
}