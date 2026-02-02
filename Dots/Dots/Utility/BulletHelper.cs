using Deploys;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    public static class BulletHelper
    {
        //怪物伤害检测
        public static void HitCreature(BulletProperties properties, Entity bulletEntity, ComponentLookup<BulletRepel> bulletRepelLookup,
            Entity globalEntity, Entity cacheEntity, 
            ComponentLookup<CacheProperties> cacheLookup, 
            RefRW<RandomSeed> random,
            DynamicBuffer<BulletHitCreature> hitCreatures,
            EntityCommandBuffer.ParallelWriter ecb,
            int sortKey,
            Entity hitCreature, 
            bool fromExplosion, 
            BufferLookup<SkillEntities> skillEntitiesLookup,
            ComponentLookup<SkillTag> skillTagLookup, 
            BufferLookup<BuffEntities> buffEntitiesLookup,
            ComponentLookup<BuffTag> buffTagLookup, 
            ComponentLookup<StatusSummon> summonLookup, 
            ComponentLookup<BuffCommonData> buffCommonLookup, 
            ComponentLookup<BuffAppendRandomIds> buffAppendBuffToBullet,
            ComponentLookup<PlayerAttrData> attrLookup, 
            BufferLookup<PlayerAttrModify> modifyLookup)
        {
            if (!CacheHelper.GetBulletConfig(properties.BulletId, cacheEntity, cacheLookup, out var config))
            {
                return;
            }
  
            //触发master自己的
            SkillHelper.DoSkillTrigger(properties.MasterCreature, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.BulletHitEnemy)
            {
                Entity = hitCreature,
                IntValue1 = properties.BulletId,
                IntValue2 = config.ClassId,
            }, ecb, sortKey);

            //如果master是召唤物，触发master的trigger
            if (summonLookup.TryGetComponent(properties.MasterCreature, out var masterCreature) && summonLookup.HasComponent(masterCreature.SummonParent))
            {
                SkillHelper.DoSkillTrigger(masterCreature.SummonParent, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.BulletHitEnemy)
                {
                    Entity = hitCreature,
                    BoolValue1 = true,
                    IntValue1 = properties.BulletId,
                    IntValue2 = config.ClassId,
                }, ecb, sortKey);
            }

            //击退力度的buff
            var buffResult = BuffHelper.GetBuffFactorAndValue(properties.MasterCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletHitForce, config.Id, config.ClassId);

            //计算击退、击飞数据
            var hitForward = MathHelper.Up;
            float hitForce;
            float repelTime;
            float repelMaxScale;
            float repelCd;
            if (bulletRepelLookup.TryGetComponent(bulletEntity, out var repelData) && bulletRepelLookup.IsComponentEnabled(bulletEntity))
            {
                //坐标击飞情况
                hitForce = repelData.Distance;
                repelTime = repelData.Time;
                repelMaxScale = repelData.MaxScale;
                repelCd = repelData.Cd;
            }
            else
            {
                //物理击飞情况
                hitForce = BuffHelper.CalcFactor(properties.HitForce + buffResult.AddValue, buffResult.AddFactor);
                repelTime = 0;
                repelMaxScale = 0;
                repelCd = 0;
            }
           
            //计算伤害
            var damageFactor = config.DamageFactor;
            {
                //buff bullet damage
                var damageBuffResult = BuffHelper.GetBuffFactorAndValue(properties.MasterCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletDamage, config.Id, config.ClassId);
                damageFactor += damageBuffResult.AddValue;
            }
            
            var damageBuffer = new DamageBuffer
            {
                Bullet = bulletEntity,
                DamageFactor = damageFactor,
                RepelForward = hitForward,
                RepelForce = hitForce,
                RepelMaxScale = repelMaxScale,
                RepelTime = repelTime,
                RepelCd = repelCd,
                FromExplosion = fromExplosion,
            };
         
            ecb.AppendToBuffer(sortKey, hitCreature, damageBuffer);
            ecb.SetComponentEnabled<DamageBuffer>(sortKey, hitCreature, true);

            //Buff处理
            var deployBuffId = config.AttachBuffId;
            if (deployBuffId > 0)
            {
                BuffHelper.AppendCreateBuffData(globalEntity, deployBuffId, hitCreature, properties.MasterCreature, 0, EBuffFrom.Bullet, config.Id, ecb, sortKey);
            }

            //buff AppendBuffToBullet
            var appendBuffList = BuffHelper.GetAppendBuffList(properties.MasterCreature, EBuffType.AppendBuffToBullet, config.Id, config.ClassId, summonLookup, buffAppendBuffToBullet, buffEntitiesLookup, buffTagLookup);
            if (appendBuffList.Length > 0)
            {
                var idx = random.ValueRW.Value.NextInt(0, appendBuffList.Length);
                var randomBuffId = appendBuffList[idx];
                BuffHelper.AppendCreateBuffData(globalEntity, randomBuffId, hitCreature, properties.MasterCreature, 0, EBuffFrom.Bullet, config.Id, ecb, sortKey);
            }
            appendBuffList.Dispose();

            //记录命中过哪些怪
            var damageInterval = BuffHelper.CalcDamageInterval(config.DamageInterval, config, properties.MasterCreature, summonLookup, attrLookup, modifyLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            hitCreatures.Add(new BulletHitCreature { Value = hitCreature, Timer = damageInterval });
        }

        public static float CalcBulletVelocity(Entity parentCreature, BulletConfig config, ComponentLookup<StatusSummon> summonLookup, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup, out float acc)
        {
            acc = config.Acceleration;
            var velocity = config.Velocity;
            
            //子弹飞行速度的Buff(在parent身上找)
            var bulletSpeedFactor = BuffHelper.GetBuffAddFactor(parentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletSpeed, config.Id, config.ClassId);
            
            //attr
            bulletSpeedFactor += AttrHelper.GetAttr(parentCreature, EAttr.BulletSpeed, attrLookup, attrModifyLookup, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);

            var minValue = AttrHelper.GetMin(EAttr.BulletSpeed);
            if (bulletSpeedFactor < minValue)
            {
                bulletSpeedFactor = minValue;
            }
            
            velocity = BuffHelper.CalcFactor(velocity, bulletSpeedFactor);
            acc = BuffHelper.CalcFactor(acc, bulletSpeedFactor);
            return velocity;
        }

        public static void CalcSplitInfo(BulletConfig bulletConfig, Entity parentCreature, ComponentLookup<StatusSummon> summonLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            out int splitCount, out float splitAngle)
        {
            splitAngle = bulletConfig.SplitAngle;
            splitCount = bulletConfig.SplitCount;
            if (splitCount <= 0)
            {
                splitCount = 1;
            }

            //分裂buff处理
            var spiltAddValue = 0;
            var splitAddFactor = 0f;

            var buffResult = BuffHelper.GetBuffFactorAndValue(parentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletSplit, bulletConfig.Id, bulletConfig.ClassId);
            spiltAddValue += (int)buffResult.AddValue;
            splitAddFactor += buffResult.AddFactor;

            //先加上value, 再处理Factor
            splitCount += spiltAddValue;
            splitCount = (int)BuffHelper.CalcFactor(splitCount, splitAddFactor);
            if (buffResult.TempData > 0)
            {
                splitAngle = buffResult.TempData;
            }
        }

        public static float CalcBulletBombRadius(float bombRadius, BulletConfig bulletConfig, Entity masterCreature, bool immediatelyBomb, Entity forceHitCreature,
            ComponentLookup<StatusSummon> summonLookup, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup, ComponentLookup<LocalTransform> transformLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            //防错，如果是立即爆炸了，但爆炸范围忘记配，则给个0.1范围
            if (immediatelyBomb && bombRadius <= 0)
            {
                Debug.LogError($"calc bullet bomb radius error, imm bomb but radius <= 0, buleltId:{bulletConfig.Id}");
                return 0.1f;
            }

            if (transformLookup.HasComponent(forceHitCreature))
            {
                return 0f;
            }

            if (bombRadius > 0f)
            {
                //爆炸范围的buff处理
                var addFactor = BuffHelper.GetBuffAddFactor(masterCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletExplosionRange, bulletConfig.Id, bulletConfig.ClassId);
             
                //attr:damage range
                addFactor += AttrHelper.GetDamageRangeFactor(masterCreature, summonLookup, attrLookup, attrModifyLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, false);
                
                bombRadius = BuffHelper.CalcFactor(bombRadius, addFactor);
            }

            return bombRadius;
        }
        
        public static void SplitHoriz(BulletSplit splitInfo, BulletProperties properties, BulletAtkValue atkValue,
            float3 shootForward, float3 shootPos, Entity factory, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            var horizDist = splitInfo.HorizDist;
            for (var h = 0; h < splitInfo.HorizCount; h++)
            {
                var horiz = MathHelper.CalcHorizSplitDist(splitInfo.HorizCount, h, horizDist);

                var startPos = shootPos + math.mul(quaternion.RotateY(math.radians(90)), shootForward) * horiz;
                //水平分裂只分裂自己
                var createBuffer = new BulletCreateBuffer
                {
                    BulletId = properties.BulletId,
                    DisableSplit = true,
                    Direction = shootForward,
                    ShootPos = startPos,
                    AtkValue = new AtkValue(atkValue.Atk, atkValue.Crit, atkValue.CritDamage, properties.Team),
                    ParentCreature = properties.MasterCreature,
                    SkillEntity = properties.SkillEntity,
                };
                ecb.AppendToBuffer(sortKey, factory, createBuffer);
            }
        }

        //尝试弹射
        public static bool TryBounce(float3 bounceForward, float3 bulletPos, RefRW<BulletProperties> properties)
        {
            if (properties.ValueRO.MaxBounceCount > 0 && properties.ValueRO.CurrBounceCount < properties.ValueRO.MaxBounceCount)
            {
                properties.ValueRW.CurrBounceCount = properties.ValueRO.CurrBounceCount + 1;

                //进行弹射：重置方向，起始坐标
                properties.ValueRW.D1 = bounceForward;
                properties.ValueRW.RunningDirection = bounceForward;
                properties.ValueRW.ShootPos = bulletPos;
                properties.ValueRW.RunningTime = 0;
                return true;
            }

            return false;
        }

        public static void AddBehaviour(GlobalAspect global, CacheAspect cache, int id, Entity bulletEntity, EntityCommandBuffer ecb)
        {
            if (cache.GetBulletBehaviour(id, out var behaviourConfig))
            {
                ecb.AppendToBuffer(bulletEntity, new BulletBehaviourBuffer { Id = id });

                //得到所有同组的
                if (behaviourConfig.BindingGroup > 0)
                {
                    for (var i = 0; i < cache.CacheProperties.ValueRO.BulletBehaviourConfig.Value.Value.Length; i++)
                    {
                        var cfg = cache.CacheProperties.ValueRO.BulletBehaviourConfig.Value.Value[i];
                        if (cfg.BindingGroup == behaviourConfig.BindingGroup && cfg.Id != behaviourConfig.Id)
                        {
                            ecb.AppendToBuffer(bulletEntity, new BulletBehaviourBuffer { Id = cfg.Id });
                        }
                    }
                }
            }
        }

        public static bool CheckCanReflect(BulletProperties properties)
        {
            return properties.V1 > 0 || properties.V2 > 0 || properties.VTurn > 0;
        }
        
        
        public static void BindCollisionBullet(GlobalAspect global, Entity creatureEntity, CreatureProps creatureProps, 
            BufferLookup<BindingBullet> bindBulletLookup, ComponentLookup<LocalTransform> transformLookup, int bulletId)
        {
            if (bindBulletLookup.TryGetBuffer(creatureEntity, out var bindingBullets))
            {
                foreach (var info in bindingBullets)
                {
                    if (info.From == EBulletFrom.Collision)
                    {
                        Debug.LogError($"重复bind bullet creature, bulletId:{bulletId} entity:{creatureEntity.Index}");
                        return;
                    }
                }
            }

            if (transformLookup.TryGetComponent(creatureEntity, out var creatureTransform))
            {
                if (bulletId > 0)
                {
                    var bulletBuffer = new BulletCreateBuffer
                    {
                        From = EBulletFrom.Collision,
                        AtkValue = new AtkValue(creatureProps.AtkValue.Atk, creatureProps.AtkValue.Crit, creatureProps.AtkValue.CritDamage, creatureProps.AtkValue.Team),
                        BulletId = bulletId,
                        TransformParent = creatureEntity,
                        ParentCreature = creatureEntity,
                        ShootPos = creatureTransform.Position,
                        DisableSplit = true,
                        DisableBounce = true,
                        DisableHitForce = true,
                        Direction = MathHelper.Down,
                        BombRadius = 0,
                        ScaleFactor = creatureTransform.Scale,
                    };
                    global.BulletCreateBuffer.Add(bulletBuffer);
                }
            }
        }

        public static bool CheckAlreadyHit(DynamicBuffer<BulletHitCreature> hitCreatures, Entity hitEntity)
        {
            var bAlreadyHit = false;
            for (var j = 0; j < hitCreatures.Length; j++)
            {
                if (hitCreatures[j].Value == hitEntity)
                {
                    bAlreadyHit = true;
                    break;
                }
            }

            return bAlreadyHit;
        }
        
        
        public static void BindBuffBullet(GlobalAspect global, Entity buffEntity, 
            ComponentLookup<BuffTag> buffLookup, 
            ComponentLookup<CreatureProps> propsLookup, 
            BufferLookup<BindingBullet> bindingBulletLookup,
            ComponentLookup<LocalTransform> localToWorldLookup, 
            ComponentLookup<BulletProperties> bulletLookup, 
            int bulletId, int layer, EntityCommandBuffer ecb)
        {
            if (bulletId <= 0)
            {
                return;
            }

            if (buffLookup.TryGetComponent(buffEntity, out var buffProperties) &&
                propsLookup.TryGetComponent(buffProperties.Attacker, out var attacker) &&
                localToWorldLookup.TryGetComponent(buffProperties.Master, out var masterTrans))
            {
                var bFindOld = false;
                var oldBulletEntity = Entity.Null;
                if(bindingBulletLookup.TryGetBuffer(buffProperties.Master, out var bindingBullets))
                {
                    foreach (var bullet in bindingBullets)
                    {
                        if (bulletLookup.TryGetComponent(bullet.Value, out var bulletProperties))
                        {
                            if (bulletProperties.From == EBulletFrom.Buff && bulletProperties.FromId == buffProperties.BuffId && bulletProperties.MasterCreature == buffProperties.Attacker)
                            {
                                bFindOld = true;
                                oldBulletEntity = bullet.Value;
                                break;
                            }
                        }
                    }
                }

                var atkValue = new AtkValue(attacker.AtkValue.Atk, attacker.AtkValue.Crit, attacker.AtkValue.CritDamage, attacker.AtkValue.Team);
                if (!bFindOld)
                {
                    //Debug.LogWarning($"bind buff bullet:{bulletId}");
                    //新增子弹
                    var bulletBuffer = new BulletCreateBuffer
                    {
                        From = EBulletFrom.Buff,
                        FromId = buffProperties.BuffId,
                        AtkValue = atkValue,
                        BulletId = bulletId,
                        TransformParent = buffProperties.Master,
                        ParentCreature = buffProperties.Attacker,
                        ShootPos = masterTrans.Position,
                        DisableSplit = true,
                        DisableBounce = true,
                        DisableHitForce = true,
                        Direction = MathHelper.Down,
                    };
                    global.BulletCreateBuffer.Add(bulletBuffer);
                }
                else
                {
                    //叠加层数
                    ecb.SetComponent(oldBulletEntity, new BulletAtkValue
                    {
                        Atk = atkValue.Atk * layer,
                        Crit = atkValue.Crit,
                        CritDamage = atkValue.CritDamage,
                    });
                }
            }
        }

        public static void ClearBuffBindBullet(Entity buffEntity, ComponentLookup<BuffTag> buffTagLookup,
            BufferLookup<BindingBullet> bindBulletLookup, ComponentLookup<BulletProperties> bulletLookup, EntityCommandBuffer ecb)
        {
            if (buffTagLookup.TryGetComponent(buffEntity, out var buffTag) && 
                bindBulletLookup.TryGetBuffer(buffTag.Master, out var bindingBullets))
            {
                foreach (var bullet in bindingBullets)
                {
                    if (bulletLookup.TryGetComponent(bullet.Value, out var bulletProperties))
                    {
                        if (bulletProperties.From == EBulletFrom.Buff && bulletProperties.FromId == buffTag.BuffId)
                        {
                            ecb.SetComponentEnabled<BulletDestroyTag>(bullet.Value, true);
                        }
                    }
                }   
            }
        }


        public static void ClearAllBindingBullet(Entity entity, BufferLookup<BindingBullet> bindBulletLookup, ComponentLookup<BulletProperties> bulletLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            if (bindBulletLookup.TryGetBuffer(entity, out var bindingBullets))
            {
                foreach (var info in bindingBullets)
                {
                    if (bulletLookup.TryGetComponent(info.Value, out var bullet))
                    {
                        /*if (bullet.BombWhenParentDead)
                        {
                            ecb.SetComponentEnabled<BulletBombTag>(sortKey, info.Value, true);
                        }
                        else*/
                        {
                            ecb.SetComponentEnabled<BulletDestroyTag>(sortKey, info.Value, true);
                        }
                    }
                }
            }
        }
        
        public static void ClearAllBindingBullet(Entity entity, BufferLookup<BindingBullet> bindBulletLookup, ComponentLookup<BulletProperties> bulletLookup, EntityCommandBuffer ecb)
        {
            if (bindBulletLookup.TryGetBuffer(entity, out var bindingBullets))
            {
                foreach (var info in bindingBullets)
                {
                    if (bulletLookup.TryGetComponent(info.Value, out var bullet))
                    {
                        /*if (bullet.BombWhenParentDead)
                        {
                            ecb.SetComponentEnabled<BulletBombTag>(sortKey, info.Value, true);
                        }
                        else*/
                        {
                            ecb.SetComponentEnabled<BulletDestroyTag>(info.Value, true);
                        }
                    }
                }
            }
        }

    }
}