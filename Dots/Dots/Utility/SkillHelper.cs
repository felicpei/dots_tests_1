using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Deploys;

namespace Dots
{
    public static class SkillHelper
    {
        //移除skill
        public static void DeleteSkill(Entity entity, int skillId, EntityCommandBuffer ecb, GlobalAspect global, CacheAspect cache,
            BufferLookup<SkillEntities> skillEntitiesLookup,
            ComponentLookup<SkillProperties> skillLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup,
            ComponentLookup<BuffTag> buffTagLookup)
        {
            if (!cache.GetSkillConfig(skillId, out var removeSkillConfig))
            {
                Debug.LogError($"RemoveSkill Skill.tab 找不到SkillId:{skillId}");
                return;
            }

            if (skillEntitiesLookup.TryGetBuffer(entity, out var skills))
            {
                foreach (var skill in skills)
                {
                    if (skillLookup.TryGetComponent(skill.Value, out var skillInfo))
                    {
                        /*//如果是技能召唤出来的技能不移除
                        if (skillInfo.RootSkillId > 0)
                        {
                            continue;
                        }*/

                        if (cache.GetSkillConfig(skillInfo.Id, out var skillInfoConfig))
                        {
                            //移动技能id相同 或者 BindingGroup相同的
                            if (skillInfoConfig.Id == skillId || removeSkillConfig.BindingGroup > 0 && removeSkillConfig.BindingGroup == skillInfoConfig.BindingGroup)
                            {
                                ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = skill.Value });

                                //destroy skill 后, 要移除对应的buff
                                BuffHelper.RemoveBuffByFrom(entity, EBuffFrom.Skill, skillInfoConfig.Id, buffEntitiesLookup, buffTagLookup, ecb);
                            }
                        }
                    }
                }
            }
        }


        public static void AddSkill(Entity globalEntity, Entity creature, int skillId, AtkValue atkValue, float3 position, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            if (skillId > 0)
            {
                ecb.AppendToBuffer(sortKey, globalEntity, new CreateSkillBuffer(creature, skillId, atkValue, creature, position));
            }
        }

        public static void AddSkill(Entity globalEntity, Entity creature, int skillId, AtkValue atkValue, float3 position, EntityCommandBuffer ecb)
        {
            if (skillId > 0)
            {
                ecb.AppendToBuffer(globalEntity, new CreateSkillBuffer(creature, skillId, atkValue, creature, position));
            }
        }

        public static void CreateSkill(float deltaTime, GlobalAspect global, 
            CacheAspect cache, CreateSkillBuffer buffer, EntityCommandBuffer ecb, 
            ComponentLookup<StatusSummon> summonLookup, 
            BufferLookup<SkillEntities> skillEntitiesLookup,
            ComponentLookup<SkillProperties> skillPropertiesLookup, 
            ComponentLookup<LocalToWorld> localToWorldLookup, 
            BufferLookup<BuffEntities> buffEntitiesLookup, 
            ComponentLookup<BuffTag> buffTagLookup, 
            ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            var skillId = buffer.SkillId;
            
         
            //addSkill
            if (!cache.GetSkillConfig(skillId, out var skillConfig))
            {
                Debug.LogError($"Skill.tab 找不到SkillId:{skillId}");
                return;
            }

            var bExists = false;
            if (skillEntitiesLookup.TryGetBuffer(buffer.Master, out var skills))
            {
                foreach (var skill in skills)
                {
                    if (skillPropertiesLookup.TryGetComponent(skill.Value, out var skillProperties))
                    {
                        if (cache.GetSkillConfig(skillProperties.Id, out var childConfig))
                        {
                            //查看有没有同group的, 移除
                            if (skillConfig.Group != 0 && childConfig.Group == skillConfig.Group && childConfig.Id != skillConfig.Id)
                            {
                                DeleteSkill(buffer.Master, childConfig.Id, ecb, global, cache, skillEntitiesLookup, skillPropertiesLookup, buffEntitiesLookup, buffTagLookup);
                                continue;
                            }

                            //技能触发的技能不进行叠加
                            if (buffer.RootSkillId == 0)
                            {
                                //查看技能ID是否已经存在，如果是则叠加层数
                                if (skillConfig.Id == childConfig.Id)
                                {
                                    bExists = true;

                                    //叠加自己
                                    ecb.AppendToBuffer(skill.Value, new SkillLayerAddBuffer { Value = 1 });
                                    ecb.SetComponentEnabled<SkillLayerAddBuffer>(skill.Value, true);
                                }

                                //查找是否存在BindGroup相同的，叠加层数
                                if (skillConfig.BindingGroup > 0 && skillConfig.BindingGroup == childConfig.BindingGroup && skillConfig.Id != childConfig.Id)
                                {
                                    //叠加自己
                                    ecb.AppendToBuffer(skill.Value, new SkillLayerAddBuffer { Value = 1 });
                                    ecb.SetComponentEnabled<SkillLayerAddBuffer>(skill.Value, true);
                                }
                            }
                        }
                    }
                }
            }

            if (!bExists)
            {
                FactoryHelper.CreateSkill(buffer.Master, global.Time, deltaTime, skillId, global, cache, buffer, false, localToWorldLookup, ecb);

                //检查buff替换技能
                if (cache.GetSkillConfig(skillId, out var sourceSkillConfig))
                {
                    var attachList = BuffHelper.GetBuffAttachInt(buffer.Master, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BindSkill, sourceSkillConfig.ClassId, sourceSkillConfig.Id);
                    for (var i = 0; i < attachList.Length; i++)
                    {
                        ecb.AppendToBuffer(global.Entity, new CreateSkillBuffer(buffer.Master, attachList[i], buffer.AtkValue, buffer.StartEntity, buffer.StartPos, buffer.PrevTarget, buffer.RootSkillId, buffer.CastDelay, buffer.Recursion, buffer.MaxRecursion));
                    }

                    attachList.Dispose();
                }
            }
        }

        public static void RemoveSkill(Entity globalEntity, Entity creature, int skillId, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            if (skillId > 0)
            {
                ecb.AppendToBuffer(sortKey, globalEntity, new RemoveSkillBuffer(creature, skillId));
            }
        }

        public static void RemoveSkill(Entity globalEntity, Entity creature, int skillId, EntityCommandBuffer ecb)
        {
            if (skillId > 0)
            {
                ecb.AppendToBuffer(globalEntity, new RemoveSkillBuffer(creature, skillId));
            }
        }

        public static void RemoveAllSkill(Entity globalEntity, Entity entity, BufferLookup<SkillEntities> skillEntitiesLookup,
            ComponentLookup<SkillProperties> skillLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            if (skillEntitiesLookup.TryGetBuffer(entity, out var skills))
            {
                foreach (var skill in skills)
                {
                    if (skillLookup.TryGetComponent(skill.Value, out var tag))
                    {
                        RemoveSkill(globalEntity, entity, tag.Id, ecb, sortKey);
                    }
                }
            }
        }

        public static void RemoveAllSkill(Entity globalEntity, Entity entity, BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillProperties> skillTagLookup, EntityCommandBuffer ecb)
        {
            if (skillEntitiesLookup.TryGetBuffer(entity, out var skills))
            {
                foreach (var skill in skills)
                {
                    if (skillTagLookup.TryGetComponent(skill.Value, out var tag))
                    {
                        RemoveSkill(globalEntity, entity, tag.Id, ecb);
                    }
                }
            }
        }


        public static void DoSkillTrigger(Entity master, BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup, SkillTriggerData triggerData, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            //SkillTrigger
            if (skillEntitiesLookup.TryGetBuffer(master, out var skillEntities))
            {
                foreach (var skill in skillEntities)
                {
                    if (skillTagLookup.HasComponent(skill.Value))
                    {
                        ecb.AppendToBuffer(sortKey, skill.Value, triggerData);
                    }
                }
            }
        }

        public static void DoSkillTrigger(Entity master, BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup, SkillTriggerData triggerData, EntityCommandBuffer ecb)
        {
            //SkillTrigger
            if (skillEntitiesLookup.TryGetBuffer(master, out var skillEntities))
            {
                foreach (var skill in skillEntities)
                {
                    if (skillTagLookup.HasComponent(skill.Value))
                    {
                        ecb.AppendToBuffer(skill.Value, triggerData);
                    }
                }
            }
        }


        public static float CalcSkillCd(float cdSource, SkillConfig config, Entity master, ComponentLookup<StatusSummon> summonLookup, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup)
        {
            //基础属性影响持续时间
            var addFactor = BuffHelper.GetBuffAddFactor(master, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup,  EBuffType.SkillTriggerInterval, config.ClassId, config.Id);

            //attr: skill speed
            addFactor += AttrHelper.GetAttr(master, EAttr.SkillSpeed, attrLookup, attrModifyLookup, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup); 
            
            return BuffHelper.CalcFactorSpeed(cdSource, addFactor);
        }
    }
}