using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct ServantInitialSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            
            //初始化
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (tag, servant, creature, transform, entity) 
                     in SystemAPI.Query<ServantInitTag, RefRW<ServantProperties>, RefRW<CreatureProperties>, LocalTransform>().WithEntityAccess())
            {
                ecb.RemoveComponent<ServantInitTag>(entity);

                if (!cache.GetServantConfig(servant.ValueRO.Id, out var config))
                {
                    continue;
                }

                creature.ValueRW.CurHp = AttrHelper.GetMaxHp(entity, _attrLookup, _attrModifyLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
              
                //来自外部养成系统技能
                for (var j = 0; j < FightData.ServantSkills.Count; j++)
                {
                    var info = FightData.ServantSkills[j];
                    if (info.Id == config.Id)
                    {
                        SkillHelper.AddSkill(global.Entity, entity, info.SkillId, creature.ValueRO.AtkValue, transform.Position, ecb);
                    }
                }
                
                //add rarity skills
                var dp = Table.GetServantSkill(servant.ValueRO.Id, (int)tag.Rarity);
                if (dp != null)
                {
                    if (dp.MainSkill == 0)
                    {
                        Debug.LogError($"Add servant error, main skill is 0?, servantId:{servant.ValueRO.Id}  rarity:{(int)tag.Rarity}");
                    }
                    else
                    {
                        SkillHelper.AddSkill(global.Entity, entity, dp.MainSkill, creature.ValueRO.AtkValue, transform.Position, ecb);
                    }

                    if (dp.UpgradeSkills != null)
                    {
                        foreach (var skillId in dp.UpgradeSkills)
                        {
                            SkillHelper.AddSkill(global.Entity, entity, skillId, creature.ValueRO.AtkValue, transform.Position, ecb);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"add servant error, id:{servant.ValueRO.Id} rarity:{tag.Rarity}");
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}