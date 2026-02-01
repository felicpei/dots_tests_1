using System;
using Deploys;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    public class DamageHelper
    {
        public static float CalcDamage(BulletProperties bulletProperties, Entity hitEntity, Entity attacker,
            EDamageCalc calcType, float damageFactor, float sourceAtk, GlobalAspect global, CacheAspect cache,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            ComponentLookup<CreatureProperties> creatureLookup, ComponentLookup<CreatureMove> creatureMoveLookup, ComponentLookup<ShieldProperties> shieldLookup,
            ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> modifyLookup,
            out bool bCanCrit)
        {
            bCanCrit = true;

            if (!creatureLookup.TryGetComponent(hitEntity, out var hitCreature))
            {
                Debug.LogError("calc damage error, hitEntity is null");
                return 0;
            }

            if (!creatureLookup.TryGetComponent(attacker, out var attackerCreature))
            {
                Debug.LogError("calc damage error, attacker is null");
                return 0;
            }

            if (!cache.GetBulletConfig(bulletProperties.BulletId, out var bulletConfig))
            {
                Debug.LogError($"calc damage error, bullet config is null, bulletId:{bulletProperties.BulletId}");
                return 0;
            }

            //按敌人百分比扣血
            if (calcType == EDamageCalc.MaxHp)
            {
                var maxHp = AttrHelper.GetMaxHp(hitEntity, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                var percentDamage = maxHp * damageFactor;
                if (percentDamage <= 0)
                {
                    Debug.LogError($"[1] 伤害0, from EnemyMaxHp, maxHp:{maxHp} fac:{damageFactor}");
                }

                //判断被击者有无百分比伤害减免buff
                var addFactor = BuffHelper.GetBuffAddFactor(hitEntity, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.PercentDamageFactor);
                percentDamage = BuffHelper.CalcFactor(percentDamage, addFactor);
                if (percentDamage <= 0)
                {
                    Debug.LogError($"[2] 伤害0, from PercentDamageFactor, addFactor:{addFactor}");
                }
                bCanCrit = false;
                return damageFactor;
            }
            
            //计算不同的伤害方式
            var damage = sourceAtk * damageFactor;
            if (damage <= 0)
            {
                Debug.LogError($"[#] 伤害0, from Start sourceAtk:{sourceAtk} damageFac:{damageFactor}");
            }

            //攻击者的伤害加成
            if (damage > 0)
            {
                
                var addFactor = 0f;
                
                //子弹伤害的buff处理
                var buffResult = BuffHelper.GetBuffFactorAndValue(attacker, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletDamage, bulletConfig.Id, bulletConfig.ClassId);
                addFactor += buffResult.AddFactor;    
                
                //血量的buff伤害加成
                var attackerMaxHp = AttrHelper.GetMaxHp(attacker, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                if (attackerCreature.CurHp <= attackerMaxHp)
                {
                    var curHpPercent = attackerCreature.CurHp / attackerMaxHp;
                    addFactor += BuffHelper.GetBuffFactorByTempPercent(EBuffType.DamageBySelfHp, curHpPercent, attacker, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                }

                //查看敌人身上是否存在指定buff类型加伤害的buff
                addFactor += BuffHelper.GetDamageByEnemyBuffFactor(bulletConfig, attacker, hitEntity, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                damage = BuffHelper.CalcFactor(damage, addFactor);
                   
                //根据防御方的移动速度加成，提升造成的伤害
                if (creatureMoveLookup.TryGetComponent(hitEntity, out var hitEntityMove) && hitEntityMove.MoveSpeedSource > 0)
                {
                    var extraSpeedFactor = CreatureHelper.GetMoveSpeedFactor(hitEntity, creatureLookup, shieldLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, attrLookup, modifyLookup);
                    if (extraSpeedFactor < 0)
                    {
                        addFactor += BuffHelper.GetBuffFactorByTempPercent(EBuffType.DamageByEnemyMoveSpeed, extraSpeedFactor, attacker, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                    }
                }

                //根据自己的移动速度加成伤害
                if (creatureMoveLookup.TryGetComponent(attacker, out var attackerMove) && attackerMove.MoveSpeedSource > 0)
                {
                    var addSpeedFactor = CreatureHelper.GetMoveSpeedFactor(attacker, creatureLookup, shieldLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup,attrLookup, modifyLookup);
                    if (addSpeedFactor > 0)
                    {
                        addFactor += BuffHelper.GetBuffFactorByTempPercent(EBuffType.DamageBySelfMoveSpeed, addSpeedFactor, attacker, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                    }
                }

                //buff damage(需要监测是否只对boss、精英生效)
                var monsterCheckType = hitCreature.Type switch
                {
                    ECreatureType.Elite => 1,
                    ECreatureType.Boss => 2,
                    _ => 0
                };
                addFactor += BuffHelper.GetBuffAddFactor(attacker, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.Damage, monsterCheckType);
                
                //attr: boss damage
                if (hitCreature.Type is ECreatureType.Elite or ECreatureType.Boss)
                {
                    addFactor += AttrHelper.GetAttr(attacker, EAttr.BossDamage, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                }
                
                //attr: damage
                addFactor += AttrHelper.GetAttr(attacker, EAttr.Damage, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);

                //attr: element damage
                switch (bulletProperties.ElementId)
                {
                    case EElement.Water:
                        addFactor += AttrHelper.GetAttr(attacker, EAttr.WaterAtk, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                        break;
                    case EElement.Fire:
                        addFactor += AttrHelper.GetAttr(attacker, EAttr.FireAtk, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                        break;
                    case EElement.Ice:
                        addFactor += AttrHelper.GetAttr(attacker, EAttr.IceAtk, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                        break;
                    case EElement.Lighting:
                        addFactor += AttrHelper.GetAttr(attacker, EAttr.LightingAtk, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                        break;
                    case EElement.Stone:
                        addFactor += AttrHelper.GetAttr(attacker, EAttr.StoneAtk, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                        break;
                }
                
                //minValue
                var minAddFactor = AttrHelper.GetMin(EAttr.Damage);
                if (addFactor < minAddFactor)
                {
                    addFactor = minAddFactor;
                }
                
                //攻击方总伤害
                damage = BuffHelper.CalcFactor(damage, addFactor);
            }

            //防御方的伤害加成或衰减
            if (damage > 0)
            {
                //def
                damage -= hitCreature.Def;
                if (damage <= 0)
                {
                    return 1f;
                }
                
                //防御方的：Buff HurtFactor
                var addFactor = BuffHelper.GetBuffAddFactor(hitEntity, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.HurtFactor);
                damage = BuffHelper.CalcFactor(damage, addFactor);
                if (damage <= 0)
                {
                    Debug.LogError($"[12] 伤害0, from HurtFactor, addFactor:{addFactor}");
                }
                     
                //护甲
                var armorPoint = AttrHelper.GetAttr(hitEntity, EAttr.Armor, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                if (armorPoint != 0)
                {
                    var armorPercent = AttrHelper.GetArmorPercent(armorPoint);
                    var fac = 1 - armorPercent; 
                    damage *= fac;
                }
            }

            return damage;
        }


       
        public static bool CheckHitRule(EHitRule rule, Entity attacker, ETeamId attackerTeamId, Entity hit, ETeamId hitTeamId)
        {
            switch (rule)
            {
                case EHitRule.Enemy:
                    break;
                case EHitRule.EnemyAndTeam:
                    return hit != attacker;
                case EHitRule.All:
                    return true;
                case EHitRule.SelfAndTeam:
                    return attackerTeamId == hitTeamId;
            }
            return attackerTeamId != hitTeamId;
        }
    }
}