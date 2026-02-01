using Deploys;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    public static class BuffHelper
    {
        public static float CalcFactor(float sourceValue, float addFactor)
        {
            var afterFactor = 1 + addFactor;
            if (afterFactor < 0)
            {
                afterFactor = 0.01f;
            }

            return sourceValue * afterFactor;
        }

        public static float CalcFactor2(float sourceValue, float addFactor)
        {
            if (addFactor >= 0)
            {
                sourceValue *= 1 + addFactor;
            }
            else
            {
                sourceValue *= 1f / (1 - addFactor);
            }
            return sourceValue;
        }
        
        public static float CalcFactorSpeed(float sourceValue, float addFactor)
        {
            var speed = 1f / sourceValue;
            if (addFactor >= 0)
            {
                speed *= 1 + addFactor;
            }
            else
            {
                speed *= 1f / (1 - addFactor);
            }
            return 1f / speed;
        }

        public static void AppendCreateBuffData(Entity globalEntity, int addBuffId, Entity hitEntity, Entity attacker, float contTime, EBuffFrom from, int fromId, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            var createBuffer = new CreateBuffData(hitEntity, addBuffId, attacker, contTime, from, fromId);
            ecb.AppendToBuffer(sortKey, globalEntity, createBuffer);
        }

        public static void AppendCreateBuffData(Entity globalEntity, int addBuffId, Entity hitEntity, Entity attacker, float contTime, EBuffFrom from, int fromId, EntityCommandBuffer ecb)
        {
            var createBuffer = new CreateBuffData(hitEntity, addBuffId, attacker, contTime, from, fromId);
            ecb.AppendToBuffer(globalEntity, createBuffer);
        }

        public static void AddBuff(GlobalAspect global, CacheAspect cache, CreateBuffData buffer, 
            ComponentLookup<CreatureProperties> creatureLookup, ComponentLookup<DisableBuffTag> disableBuffTagLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup,
            ComponentLookup<LocalTransform> transformLookup, ComponentLookup<BuffCommonData> buffCommonLookup, EntityCommandBuffer ecb)
        {
            //看是否免疫buff
            if (disableBuffTagLookup.HasComponent(buffer.Master) && disableBuffTagLookup.IsComponentEnabled(buffer.Master))
            {
                return;
            }

            if (cache.GetBuffConfig(buffer.BuffId, out var buffConfig))
            {
                //遍历已经存在的buff
                var bOverlap = false;
                if (buffEntitiesLookup.TryGetBuffer(buffer.Master, out var buffs))
                {
                    for (var i = buffs.Length - 1; i >= 0; i--)
                    {
                        if (buffTagLookup.TryGetComponent(buffs[i].Value, out var oldBuff))
                        {
                            //尝试移除同group的buff
                            if (buffConfig.Group != 0)
                            {
                                if (oldBuff.BuffId != buffConfig.Id && oldBuff.Group == buffConfig.Group)
                                {
                                    ecb.AppendToBuffer(buffs[i].Value, new BuffUpdateBuffer(EBuffUpdate.Remove));
                                    ecb.SetComponentEnabled<BuffUpdateBuffer>(buffs[i].Value, true);
                                }
                            }

                            //看是否需要OverLap
                            if (oldBuff.BuffId == buffConfig.Id)
                            {
                                ecb.AppendToBuffer(buffs[i].Value, new BuffUpdateBuffer(EBuffUpdate.Overlap));
                                ecb.SetComponentEnabled<BuffUpdateBuffer>(buffs[i].Value, true);
                                bOverlap = true;
                            }
                        }
                    }
                }

                if (!bOverlap)
                {
                    FactoryHelper.CreateBuff(buffer.Master, buffer, global, cache, creatureLookup, transformLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, ecb);
                }
            }
        }

        public static void RemoveAllBuff(Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    if (buffLookup.HasComponent(buff.Value))
                    {
                        ecb.AppendToBuffer(sortKey, buff.Value, new BuffUpdateBuffer(EBuffUpdate.Remove));
                        ecb.SetComponentEnabled<BuffUpdateBuffer>(sortKey, buff.Value, true);
                    }
                }
            }
        }
        
        public static void RemoveAllBuff(Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffLookup, EntityCommandBuffer ecb)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    if (buffLookup.HasComponent(buff.Value))
                    {
                        ecb.AppendToBuffer(buff.Value, new BuffUpdateBuffer(EBuffUpdate.Remove));
                        ecb.SetComponentEnabled<BuffUpdateBuffer>(buff.Value, true);
                    }
                }
            }
        }

        public static void RemoveBuffByFrom(Entity master, EBuffFrom from, int fromId, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, EntityCommandBuffer ecb)
        {
            if (from == EBuffFrom.None || fromId == 0)
            {
                return;
            }

            if (buffEntitiesLookup.TryGetBuffer(master, out var buffs))
            {
                foreach (var buffEntity in buffs)
                {
                    if (buffTagLookup.TryGetComponent(buffEntity.Value, out var buffProperties))
                    {
                        if (buffProperties.From == from && buffProperties.FromId == fromId)
                        {
                            ecb.AppendToBuffer(buffEntity.Value, new BuffUpdateBuffer(EBuffUpdate.Remove));
                            ecb.SetComponentEnabled<BuffUpdateBuffer>(buffEntity.Value, true);
                        }
                    }
                }
            }
        }

        public static void RemoveBuffByFrom(Entity master, EBuffFrom from, int fromId, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            if (from == EBuffFrom.None || fromId == 0)
            {
                return;
            }

            if (buffEntitiesLookup.TryGetBuffer(master, out var buffs))
            {
                foreach (var buffEntity in buffs)
                {
                    if (buffTagLookup.TryGetComponent(buffEntity.Value, out var buffProperties))
                    {
                        if (buffProperties.From == from && buffProperties.FromId == fromId)
                        {
                            ecb.AppendToBuffer(sortKey, buffEntity.Value, new BuffUpdateBuffer(EBuffUpdate.Remove));
                            ecb.SetComponentEnabled<BuffUpdateBuffer>(sortKey, buffEntity.Value, true);
                        }
                    }
                }
            }
        }

        public static void RemoveBuffById(int buffId, Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffLookup, EntityCommandBuffer.ParallelWriter ecb,
            int sortKey)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    if (buffLookup.TryGetComponent(buff.Value, out var buffTag) && buffTag.BuffId == buffId)
                    {
                        ecb.AppendToBuffer(sortKey, buff.Value, new BuffUpdateBuffer(EBuffUpdate.Remove));
                        ecb.SetComponentEnabled<BuffUpdateBuffer>(sortKey, buff.Value, true);
                    }
                }
            }
        }
        
        public static void RemoveBuffById(int buffId, Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffLookup, EntityCommandBuffer ecb)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    if (buffLookup.TryGetComponent(buff.Value, out var buffTag) && buffTag.BuffId == buffId)
                    {
                        ecb.AppendToBuffer(buff.Value, new BuffUpdateBuffer(EBuffUpdate.Remove));
                        ecb.SetComponentEnabled<BuffUpdateBuffer>(buff.Value, true);
                    }
                }
            }
        }

        public static int GetBuffLayerCount(int buffId, Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffLookup)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    if (buffLookup.TryGetComponent(buff.Value, out var buffProperties) && buffProperties.BuffId == buffId)
                    {
                        return buffProperties.Layer;
                    }
                }
            }

            return 0;
        }

        public static bool CheckBuffTag(EBuffType buffType, Entity buffEntity, ComponentLookup<BuffTag> buffTagLookup, bool checkingParent, int fromMonsterId)
        {
            if (buffTagLookup.TryGetComponent(buffEntity, out var buffTag) && buffTag.BuffType == buffType)
            {
                //检查自己的情况， 如果该buff只影响召唤物不影响自己, 则跳过
                if (!checkingParent)
                {
                    if (buffTag.InfluenceSummon == EInfluenceSummon.Summon)
                    {
                        return false;
                    }
                }
                else
                {
                    //检查自己的 parent的情况
                    if (buffTag.InfluenceSummon == EInfluenceSummon.Self)
                    {
                        return false;
                    }

                    //检查SummonId是否匹配
                    if (buffTag.InfluenceSummonId != 0 && buffTag.InfluenceSummonId != fromMonsterId)
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        public static bool CheckHasBuffType(EBuffType buffType, Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    if (buffTagLookup.TryGetComponent(buff.Value, out var buffTag))
                    {
                        if (buffTag.BuffType == buffType)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool CheckHasBuffId(int buffId, Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    if (buffTagLookup.TryGetComponent(buff.Value, out var buffProperties) && buffProperties.BuffId == buffId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static float CalcContTime(float source, Entity master, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            //buff contTime
            var addFactor = GetBuffAddFactor(master, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.ContTime);
            source = CalcFactor(source, addFactor);
            return source;
        }

        public static float CalcDamageInterval(float damageInterval, BulletConfig bulletConfig, Entity master, ComponentLookup<CreatureProperties> creatureLookup, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> modifyLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            var addFactor = GetBuffAddFactor(master, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletDamageInterval, bulletConfig.Id, bulletConfig.ClassId);
            addFactor += AttrHelper.GetAttr(master, EAttr.DamageInterval, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            
            return CalcFactorSpeed(damageInterval, addFactor);
        }

        public static NativeList<int> GetBuffAttachInt(Entity master, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            EBuffType buffType, int checkTargetInt1 = 0, int checkTargetInt2 = 0, int checkTargetInt3 = 0, bool checkingParent = false, int fromMonsterId = 0)
        {
            var result = new NativeList<int>(Allocator.Temp);
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(buffType, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var commonData))
                        {
                            if (commonData.CheckInt1 != 0 && commonData.CheckInt1 != checkTargetInt1)
                            {
                                continue;
                            }

                            if (commonData.CheckInt2 != 0 && commonData.CheckInt2 != checkTargetInt2)
                            {
                                continue;
                            }

                            if (commonData.CheckInt3 != 0 && commonData.CheckInt3 != checkTargetInt3)
                            {
                                continue;
                            }

                            result.Add(commonData.AttachInt);
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                var resultParent = GetBuffAttachInt(creature.SummonParent, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, buffType,
                    checkTargetInt1, checkTargetInt2, checkTargetInt3, true, creature.SelfConfigId);
                
                for (var i = 0; i < resultParent.Length; i++)
                {
                    result.Add(resultParent[i]);
                }

                resultParent.Dispose();
            }

            return result;
        }

        public static float GetBuffAddFactor(Entity master, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            EBuffType buffType, int checkTargetInt1 = 0, int checkTargetInt2 = 0, bool checkTargetBool = false, float checkFloat = 0, bool checkingParent = false, int fromMonsterId = 0)
        {
            //interval buff
            var addFactor = 0f;
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(buffType, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var commonData))
                        {
                            if (commonData.CheckInt1 != 0 && commonData.CheckInt1 != checkTargetInt1)
                            {
                                continue;
                            }

                            if (commonData.CheckInt2 != 0 && commonData.CheckInt2 != checkTargetInt2)
                            {
                                continue;
                            }

                            //例如：buff需要护盾，但当前无护盾, buff需要是boss但当前不是boss
                            if (commonData.CheckBool1 && !checkTargetBool)
                            {
                                continue;
                            }

                            //例如：配置而来血量必须 > 50%, 但当前血量 < 50%则不处理
                            if (commonData.CheckFloatMin > 0 && checkFloat < commonData.CheckFloatMin)
                            {
                                continue;
                            }

                            if (commonData.CheckFloatMax > 0 && checkFloat > commonData.CheckFloatMax)
                            {
                                continue;
                            }

                            addFactor += commonData.AddFactor;
                        }
                    }
                }
            }


            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                addFactor += GetBuffAddFactor(creature.SummonParent, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, buffType,
                    checkTargetInt1, checkTargetInt2, checkTargetBool, checkFloat, true, creature.SelfConfigId);
            }

            return addFactor;
        }

        public static float GetBuffAddValue(Entity master, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            EBuffType buffType, int checkTargetInt1 = 0, int checkTargetInt2 = 0, bool checkingParent = false, int fromMonsterId = 0)
        {
            //interval buff
            var addValue = 0f;
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(buffType, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var commonData))
                        {
                            if (commonData.CheckInt1 != 0 && commonData.CheckInt1 != checkTargetInt1)
                            {
                                continue;
                            }

                            if (commonData.CheckInt2 != 0 && commonData.CheckInt2 != checkTargetInt2)
                            {
                                continue;
                            }

                            addValue += commonData.AddValue;
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                addValue += GetBuffAddValue(creature.SummonParent, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, buffType,
                    checkTargetInt1, checkTargetInt2, true, creature.SelfConfigId);
            }

            return addValue;
        }

        public static bool GetHasBuff(Entity master, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            EBuffType buffType, int checkTargetInt1 = 0, bool checkingParent = false, int fromMonsterId = 0)
        {
            //检查自己身上的buff
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(buffType, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var commonData))
                        {
                            if (commonData.CheckInt1 != 0 && commonData.CheckInt1 != checkTargetInt1)
                            {
                                continue;
                            }

                            return true;
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                if (GetHasBuff(creature.SummonParent, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, buffType, checkTargetInt1, true, creature.SelfConfigId))
                {
                    return true;
                }
            }

            return false;
        }

        public struct BuffResult
        {
            public float AddFactor;
            public float AddValue;
            public float TempData;
        }

        public static BuffResult GetBuffFactorAndValue(Entity master, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            EBuffType buffType, int checkTargetInt1 = 0, int checkTargetInt2 = 0, bool checkBool = false, float checkFloat = 0, bool checkingParent = false, int fromMonsterId = 0)
        {
            BuffResult result = default;
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(buffType, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var commonData))
                        {
                            if (commonData.CheckInt1 != 0 && commonData.CheckInt1 != checkTargetInt1)
                            {
                                continue;
                            }

                            if (commonData.CheckInt2 != 0 && commonData.CheckInt2 != checkTargetInt2)
                            {
                                continue;
                            }

                            //例如：buff配置了需要护盾，但当前护盾则不处理
                            if (commonData.CheckBool1 && !checkBool)
                            {
                                continue;
                            }

                            //例如：配置而来血量必须 > 50%, 但当前血量 < 50%则不处理
                            if (commonData.CheckFloatMin > 0 && checkFloat <= commonData.CheckFloatMin)
                            {
                                continue;
                            }

                            if (commonData.CheckFloatMax > 0 && checkFloat > commonData.CheckFloatMax)
                            {
                                continue;
                            }


                            result.AddFactor += commonData.AddFactor;
                            result.AddValue += commonData.AddValue;
                            result.TempData += commonData.TempData;
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                var parentResult = GetBuffFactorAndValue(creature.SummonParent, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, buffType,
                    checkTargetInt1, checkTargetInt2, checkBool, checkFloat, true, creature.SelfConfigId);

                result.AddFactor += parentResult.AddFactor;
                result.AddValue += parentResult.AddValue;
                result.TempData += parentResult.TempData;
            }

            return result;
        }

        public static float GetBuffFactorByTempPercent(EBuffType buffType, float curPercent, Entity master, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup, bool checkingParent = false, int fromMonsterId = 0)
        {
            var totalAddFactor = 0f;
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(buffType, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var info))
                        {
                            if (info.TempData == 0)
                            {
                                Debug.LogError("GetBuffFactorByTempPercent error， TempData 不能为0");
                                continue;
                            }

                            var count = (curPercent / info.TempData);
                            var addFactor = info.AddFactor * count;

                            //最大限制
                            if (info.MaxLimit > 0 && addFactor > info.MaxLimit)
                            {
                                addFactor = info.MaxLimit;
                            }

                            totalAddFactor += addFactor;
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                totalAddFactor += GetBuffFactorByTempPercent(buffType, curPercent, creature.SummonParent, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, true, creature.SelfConfigId);
            }

            return totalAddFactor;
        }


        public static NativeList<int> GetAppendBuffList(Entity master, EBuffType buffType, int checkInt1, int checkInt2, ComponentLookup<CreatureProperties> creatureLookup, ComponentLookup<BuffAppendRandomIds> buffAppendRandomIds,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, bool checkingParent = false, int fromMonsterId = 0)
        {
            var buffList = new NativeList<int>(Allocator.Temp);

            //来自AppendBuffToBullet
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(buffType, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffAppendRandomIds.TryGetComponent(buffEntity.Value, out var info) && buffAppendRandomIds.IsComponentEnabled(buffEntity.Value))
                        {
                            if (info.CheckInt1 != 0 && info.CheckInt1 != checkInt1)
                            {
                                continue;
                            }

                            if (info.CheckInt2 != 0 && info.CheckInt2 != checkInt2)
                            {
                                continue;
                            }

                            if (info.Append1 > 0) buffList.Add(info.Append1);
                            if (info.Append2 > 0) buffList.Add(info.Append2);
                            if (info.Append3 > 0) buffList.Add(info.Append3);
                            if (info.Append4 > 0) buffList.Add(info.Append4);
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                var parentList = GetAppendBuffList(creature.SummonParent, buffType, checkInt1, checkInt2, creatureLookup, buffAppendRandomIds, buffEntitiesLookup, buffTagLookup, true, creature.SelfConfigId);
                for (var i = 0; i < parentList.Length; i++)
                {
                    buffList.Add(parentList[i]);
                }

                parentList.Dispose();
            }

            return buffList;
        }

        //检查敌人的buff来增加伤害，可影响召唤物
        public static float GetDamageByEnemyBuffFactor(BulletConfig bulletConfig, Entity attacker, Entity hitCreature, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup, bool checkingParent = false, int fromMonsterId = 0)
        {
            var addFactor = 0f;
            if (buffEntitiesLookup.TryGetBuffer(attacker, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(EBuffType.DamageByEnemyBuff, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var info))
                        {
                            //检查enemy身上是否存在该buff
                            if (!CheckHasBuffType((EBuffType)info.CheckInt1, hitCreature, buffEntitiesLookup, buffTagLookup))
                            {
                                continue;
                            }

                            //检查子弹ID
                            if (info.CheckInt2 != 0 && bulletConfig.Id != info.CheckInt2)
                            {
                                continue;
                            }

                            //看是否指定了子弹类型
                            if (info.CheckInt3 != 0 && bulletConfig.ClassId != info.CheckInt3)
                            {
                                continue;
                            }

                            addFactor += info.AddFactor;
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(attacker, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                addFactor += GetDamageByEnemyBuffFactor(bulletConfig, creature.SummonParent, hitCreature, 
                    creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, true, creature.SelfConfigId);
            }

            return addFactor;
        }

        public static bool CheckBuffProb(Entity master, ComponentLookup<CreatureProperties> creatureLookup, RefRW<RandomSeed> random, EBuffType buffType,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup, out int attachInt, bool checkingParent = false, int fromMonsterId = 0)
        {
            attachInt = 0;
            //看这个creature是否有反弹子弹的buff，如果有则反弹
            if (buffEntitiesLookup.TryGetBuffer(master, out var hitBuffers))
            {
                foreach (var buffEntity in hitBuffers)
                {
                    if (CheckBuffTag(buffType,buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var buffData))
                        {
                            if (random.ValueRW.Value.NextFloat(0f, 1f) < buffData.TempData)
                            {
                                //反弹子弹
                                attachInt = buffData.AttachInt;
                                return true;
                            }
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                if (CheckBuffProb(creature.SummonParent, creatureLookup, random, buffType, buffEntitiesLookup, buffTagLookup, buffCommonLookup, out var attachInt2, true, creature.SelfConfigId))
                {
                    if (attachInt2 != 0)
                    {
                        attachInt = attachInt2;
                    }
                    return true;
                }
            }
            return false;
        }

        //处理吸血
        public static void ProcessTransfusionBuff(Entity entity, float damageResult, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup,
            ComponentLookup<BuffTransfusion> buffTransfusion, ComponentLookup<CreatureProperties> creatureLookup, EntityCommandBuffer ecb, bool checkingParent = false, int fromMonsterId = 0)
        {
            if (buffEntitiesLookup.TryGetBuffer(entity, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(EBuffType.Transfusion, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffTransfusion.TryGetComponent(buffEntity.Value, out var buff) && buffTransfusion.IsComponentEnabled(buffEntity.Value))
                        {
                            if (creatureLookup.HasComponent(buff.Attacker))
                            {
                                var addHp = damageResult * buff.Factor;
                                if (addHp > 0)
                                {
                                    ecb.AppendToBuffer(buff.Attacker, new CreatureDataProcess { Type = ECreatureDataProcess.Cure, AddValue = addHp });
                                    ecb.SetComponentEnabled<CreatureDataProcess>(buff.Attacker, true);
                                }
                            }
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(entity, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                ProcessTransfusionBuff(creature.SummonParent, damageResult, buffEntitiesLookup, buffTagLookup, buffTransfusion, creatureLookup, ecb, true, creature.SelfConfigId);
            }
        }

        public static bool CheckExecutionBuff(Entity master, float damageResult, GlobalAspect global, Entity hitEntity, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup,  ComponentLookup<BuffCommonData> buffCommonLookup,
            ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> modifyLookup,
            out int effectId, bool checkingParent = false, int fromMonsterId = 0)
        {
            effectId = 0;

            if (!creatureLookup.TryGetComponent(hitEntity, out var hitCreature))
            {
                return false;
            }

            if (hitCreature.CurHp - damageResult <= 0)
            {
                return false;
            }

            var hitHpPercent = (hitCreature.CurHp - damageResult) / AttrHelper.GetMaxHp(hitEntity, attrLookup, modifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);

            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (CheckBuffTag(EBuffType.ExecutionEnemy, buffEntity.Value, buffTagLookup, checkingParent, fromMonsterId))
                    {
                        if (buffCommonLookup.TryGetComponent(buffEntity.Value, out var commonData))
                        {
                            //检查血量
                            if (hitHpPercent >= commonData.CheckFloatMax)
                            {
                                continue;
                            }

                            //检查是否对精英生效
                            if (commonData.CheckInt1 == 0 && hitCreature.Type == ECreatureType.Elite)
                            {
                                continue;
                            }

                            //检查是否对boss生效
                            if (commonData.CheckInt2 == 0 && hitCreature.Type == ECreatureType.Boss)
                            {
                                continue;
                            }

                            //检查几率
                            if (commonData.TempData > 0 && global.Random.ValueRW.Value.NextFloat(0f, 1f) < commonData.TempData)
                            {
                                effectId = commonData.AttachInt;
                                return true;
                            }
                        }
                    }
                }
            }

            //如果自己是召唤物，则需要查看parent的buff
            if (creatureLookup.TryGetComponent(master, out var creature) && creatureLookup.HasComponent(creature.SummonParent))
            {
                //检查parent身上是否有该buff会影响自己    
                if (CheckExecutionBuff(creature.SummonParent, damageResult, global, hitEntity, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, attrLookup, modifyLookup, out effectId, true, creature.SelfConfigId))
                {
                    return true;
                }
            }

            return false;
        }

        public static int CheckHasFreeze(Entity master, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffFreeze> buffFreezeLookup)
        {
            var count = 0;
            if (buffEntitiesLookup.TryGetBuffer(master, out var buffEntities))
            {
                foreach (var buffEntity in buffEntities)
                {
                    if (buffFreezeLookup.TryGetComponent(buffEntity.Value, out var freeze))
                    {
                        if (freeze.Enable)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }
    }
}