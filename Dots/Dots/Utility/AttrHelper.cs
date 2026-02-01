using System;
using Deploys;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    public class AttrHelper
    {
        public static float GetMin(EAttr attr)
        {
            switch (attr)
            {
                case EAttr.Damage:
                case EAttr.BulletSpeed:
                case EAttr.DamageRange:
                case EAttr.DamageInterval:
                    return -0.95f;
                case EAttr.CritDamage:
                    return 1.25f;
                case EAttr.EnemySpeed:
                    return -0.9f;
                case EAttr.EnemyCount:
                    return -0.7f;
            }

            return int.MinValue;
        }

        public static float GetMax(EAttr attr, Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            switch (attr)
            {
                case EAttr.Dodge:
                {
                    return 0.7f;
                }
                case EAttr.Crit:
                {
                    return 0.7f;
                }
                case EAttr.ShootCount:
                {
                    return 10;
                }
                case EAttr.DamageRange:
                {
                    return 4f;
                }
            }

            return int.MaxValue;
        }

        public static float GetAttr(Entity entity, EAttr attr, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> modifyLookup, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            //find master root
            //如果master root 是player, 属性才生效
            var masterRoot = CreatureHelper.GetMasterRoot(entity, creatureLookup);
            if (attrLookup.TryGetComponent(masterRoot, out var attrData))
            {
                var modifyFactor = 1f;
                if (modifyLookup.TryGetBuffer(masterRoot, out var attrBuffer))
                {
                    foreach (var modify in attrBuffer)
                    {
                        if (modify.Type == attr)
                        {
                            modifyFactor = modify.Value;
                            break;
                        }
                    }
                }

                switch (attr)
                {
                    case EAttr.MaxHp:
                        return attrData.MaxHp * modifyFactor;
                    case EAttr.Recovery:
                        return attrData.Recovery * modifyFactor;
                    case EAttr.MoveSpeed:
                        return attrData.MoveSpeed * modifyFactor;
                    case EAttr.Armor:
                        return attrData.Armor * modifyFactor;
                    case EAttr.Dodge:
                        return attrData.Dodge * modifyFactor;
                    case EAttr.Crit:
                        return attrData.Crit * modifyFactor;
                    case EAttr.CritDamage:
                        return attrData.CritDamage * modifyFactor;
                    case EAttr.Damage:
                        return attrData.Damage * modifyFactor;
                    case EAttr.PhysicsAtk:
                        return attrData.PhysicsAtk * modifyFactor;
                    case EAttr.FireAtk:
                        return attrData.FireAtk * modifyFactor;
                    case EAttr.WaterAtk:
                        return attrData.WaterAtk * modifyFactor;
                    case EAttr.IceAtk:
                        return attrData.IceAtk * modifyFactor;
                    case EAttr.LightingAtk:
                        return attrData.LightingAtk * modifyFactor;
                    case EAttr.StoneAtk:
                        return attrData.StoneAtk * modifyFactor;
                    case EAttr.SkillSpeed:
                        return attrData.SkillSpeed * modifyFactor;
                    case EAttr.BulletSpeed:
                        return attrData.BulletSpeed * modifyFactor;
                    case EAttr.DamageInterval:
                        return attrData.DamageInterval * modifyFactor;
                    case EAttr.ShootCount:
                        return attrData.ShootCount * modifyFactor;
                    case EAttr.DamageRange:
                    {
                        var buffAdd = BuffHelper.GetBuffAddFactor(entity, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.AtkRange);
                        return attrData.DamageRange * modifyFactor + buffAdd;
                    }
                    case EAttr.ExpFactor:
                        return attrData.ExpFactor * modifyFactor;
                    case EAttr.Lucky:
                        return attrData.Lucky * modifyFactor;
                    case EAttr.Interest:
                        return attrData.Interest * modifyFactor;
                    case EAttr.BossDamage:
                        return attrData.BossDamage * modifyFactor;
                    case EAttr.EnemySpeed:
                        return attrData.EnemySpeed * modifyFactor;
                    case EAttr.EnemyCount:
                        return attrData.EnemyCount * modifyFactor;
                    case EAttr.PickupRange:
                        return attrData.PickupRange * modifyFactor;
                    default:
                    {
                        Debug.LogError($"get player attr error, type:{(int)attr}");
                        break;
                    }
                }
            }

            return 0;
        }

        public static float GetMaxHp(Entity entity, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> modifyLookup, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            if (creatureLookup.TryGetComponent(entity, out var creatureProperties))
            {
                var factor = creatureProperties.FullHpFactor + GetAttr(entity, EAttr.MaxHp, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                var maxHp = creatureProperties.FullHp * (1 + factor);
                if (maxHp < 1)
                {
                    maxHp = 1f;
                }

                return maxHp;
            }

            Debug.LogError("get max hp error");
            return 1;
        }

        public static void AddAttr(PlayerAspect player, EAttr type, float addValue, EntityCommandBuffer ecb)
        {
            switch (type)
            {
                case EAttr.MaxHp:
                {
                    player.Attr.ValueRW.MaxHp += addValue;

                    if (addValue > 0)
                    {
                        //local player
                        {
                            ecb.AppendToBuffer(player.Entity, new CreatureDataProcess
                            {
                                Type = ECreatureDataProcess.AddCurHp,
                                AddValue = addValue,
                            });
                            ecb.SetComponentEnabled<CreatureDataProcess>(player.Entity, true);
                        }

                        //all servants
                        foreach (var servant in player.Servants)
                        {
                            ecb.AppendToBuffer(servant.Value, new CreatureDataProcess
                            {
                                Type = ECreatureDataProcess.AddCurHp,
                                AddValue = addValue,
                            });
                            ecb.SetComponentEnabled<CreatureDataProcess>(servant.Value, true);
                        }
                    }

                    break;
                }
                case EAttr.Recovery:
                    player.Attr.ValueRW.Recovery += addValue;
                    break;
                case EAttr.MoveSpeed:
                    player.Attr.ValueRW.MoveSpeed += addValue;
                    break;
                case EAttr.Armor:
                    player.Attr.ValueRW.Armor += addValue;
                    break;
                case EAttr.Dodge:
                    player.Attr.ValueRW.Dodge += addValue;
                    break;
                case EAttr.Crit:
                    player.Attr.ValueRW.Crit += addValue;
                    break;
                case EAttr.CritDamage:
                    player.Attr.ValueRW.CritDamage += addValue;
                    break;
                case EAttr.Damage:
                    player.Attr.ValueRW.Damage += addValue;
                    break;
                case EAttr.PhysicsAtk:
                    player.Attr.ValueRW.PhysicsAtk += addValue;
                    break;
                case EAttr.FireAtk:
                    player.Attr.ValueRW.FireAtk += addValue;
                    break;
                case EAttr.WaterAtk:
                    player.Attr.ValueRW.WaterAtk += addValue;
                    break;
                case EAttr.IceAtk:
                    player.Attr.ValueRW.IceAtk += addValue;
                    break;
                case EAttr.LightingAtk:
                    player.Attr.ValueRW.LightingAtk += addValue;
                    break;
                case EAttr.StoneAtk:
                    player.Attr.ValueRW.StoneAtk += addValue;
                    break;
                case EAttr.SkillSpeed:
                    player.Attr.ValueRW.SkillSpeed += addValue;
                    break;
                case EAttr.BulletSpeed:
                    player.Attr.ValueRW.BulletSpeed += addValue;
                    break;
                case EAttr.DamageInterval:
                    player.Attr.ValueRW.DamageInterval += addValue;
                    break;
                case EAttr.ShootCount:
                    player.Attr.ValueRW.ShootCount += addValue;
                    break;
                case EAttr.DamageRange:
                    player.Attr.ValueRW.DamageRange += addValue;
                    break;
                case EAttr.ExpFactor:
                    player.Attr.ValueRW.ExpFactor += addValue;
                    break;
                case EAttr.Lucky:
                    player.Attr.ValueRW.Lucky += addValue;
                    break;
                case EAttr.Interest:
                    player.Attr.ValueRW.Interest += addValue;
                    break;
                case EAttr.BossDamage:
                    player.Attr.ValueRW.BossDamage += addValue;
                    break;
                case EAttr.EnemySpeed:
                    player.Attr.ValueRW.EnemySpeed += addValue;
                    break;
                case EAttr.EnemyCount:
                    player.Attr.ValueRW.EnemyCount += addValue;
                    break;
                case EAttr.PickupRange:
                    player.Attr.ValueRW.PickupRange += addValue;
                    break;
                default:
                {
                    Debug.LogError($"add player attr error, type:{type}");
                    break;
                }
            }
        }

        public static float GetArmorPercent(float armor)
        {
            //减伤百分比=X/(X+15)
            return armor / (armor + 15f);
        }

        public static float GetHpRecoveryInterval(float point)
        {
            if (point > 0)
            {
                float baseInterval = 6f;
                float factor = 0.6f;
                var interval = baseInterval / Mathf.Pow(1 + point, factor);
                if (interval < 0.05)
                {
                    interval = 0.05f;
                }

                return interval;
            }

            return 10000;
        }

        public static float GetHpRecoveryPercent()
        {
            return 0.01f;
        }

        public static float GetDamageRangeFactor(Entity entity,ComponentLookup<CreatureProperties> creatureLookup, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup, 
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup, bool useMax)
        {
            var value = GetAttr(entity, EAttr.DamageRange, attrLookup, attrModifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            var minValue = GetMin(EAttr.DamageRange);
            if (value < minValue)
            {
                value = minValue;
            }

            var maxValue = GetMax(EAttr.DamageRange, entity, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            if (value > maxValue)
            {
                value = maxValue;
            }

            //from bullet scale
            if (useMax)
            {
                if (value > 1.5f)
                {
                    value = 1.5f;
                }
            }
            return value;
        }
    }
}