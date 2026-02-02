using System.Collections.Generic;
using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct MonsterInitialSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<StatusForward> _forwardLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_SetActive> _eventSetActive;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private BufferLookup<BindingBullet> _bindBulletLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();

            _forwardLookup = state.GetComponentLookup<StatusForward>(true);
            _forwardLookup = state.GetComponentLookup<StatusForward>(true);
            _eventSetActive = state.GetComponentLookup<HybridEvent_SetActive>(true);
            _bindBulletLookup = state.GetBufferLookup<BindingBullet>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _forwardLookup.Update(ref state);
            _eventSetActive.Update(ref state);
            _bindBulletLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();

            //初始化
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (tag, monsterProperties, creature, props, transform, entity) 
                     in SystemAPI.Query<MonsterInitTag, RefRW<MonsterProperties>, RefRW<CreatureTag>, CreatureProps, LocalTransform>().WithEntityAccess())
            {
                ecb.RemoveComponent<MonsterInitTag>(entity);

                if (!cache.GetMonsterConfig(monsterProperties.ValueRO.Id, out var config))
                {
                    continue;
                }

                //加血条
                if (creature.ValueRO.Type is ECreatureType.Elite or ECreatureType.Boss)
                {
                    ecb.AppendToBuffer(global.Entity, new ProgressBarCreateBuffer
                    {
                        Parent = entity,
                        Value = 1f,
                        StartPos = transform.Position,
                    });
                }

                //初始化掉落信息 
                var deploy = Table.GetMonster(config.Id);
                {
                    //drop 1
                    if (RandomDropItem(global, cache, deploy.Drop1, out var dropDeploy, out var count))
                    {
                        ecb.AppendToBuffer(entity, new MonsterDropInfo
                        {
                            IsExp = dropDeploy.Action == EDropItemAction.AddExp,
                            IsGold = dropDeploy.Action == EDropItemAction.AddGold,
                            ItemId = dropDeploy.Id,
                            ItemCount = count
                        });
                    }
                }

                {
                    //drop 2
                    if (RandomDropItem(global, cache, deploy.Drop2, out var dropDeploy, out var count))
                    {
                        ecb.AppendToBuffer(entity, new MonsterDropInfo
                        {
                            IsExp = dropDeploy.Action == EDropItemAction.AddExp,
                            IsGold = dropDeploy.Action == EDropItemAction.AddGold,
                            ItemId = dropDeploy.Id,
                            ItemCount = count
                        });
                    }
                }

                {
                    //drop3
                    if (RandomDropItem(global, cache, deploy.Drop3, out var dropDeploy, out var count))
                    {
                        ecb.AppendToBuffer(entity, new MonsterDropInfo
                        {
                            IsExp = dropDeploy.Action == EDropItemAction.AddExp,
                            IsGold = dropDeploy.Action == EDropItemAction.AddGold,
                            ItemId = dropDeploy.Id,
                            ItemCount = count
                        });
                    }
                }


                //怪物出生技能, 来自配置表技能
                var position = transform.Position;
                SkillHelper.AddSkill(global.Entity, entity, config.Skill1, props.AtkValue, position, ecb);
                SkillHelper.AddSkill(global.Entity, entity, config.Skill2, props.AtkValue, position, ecb);
                SkillHelper.AddSkill(global.Entity, entity, config.Skill3, props.AtkValue, position, ecb);
                SkillHelper.AddSkill(global.Entity, entity, config.Skill4, props.AtkValue, position, ecb);
                SkillHelper.AddSkill(global.Entity, entity, config.Skill5, props.AtkValue, position, ecb);

                //来自外部养成系统技能
                switch (creature.ValueRO.Type)
                {
                    case ECreatureType.Small:
                    {
                        for (var i = 0; i < FightData.SmallSkills.Count; i++)
                        {
                            var skillId = FightData.SmallSkills[i];
                            SkillHelper.AddSkill(global.Entity, entity, skillId, props.AtkValue, position, ecb);
                        }

                        break;
                    }
                    case ECreatureType.Elite:
                    {
                        for (var i = 0; i < FightData.EliteSkills.Count; i++)
                        {
                            var skillId = FightData.EliteSkills[i];
                            SkillHelper.AddSkill(global.Entity, entity, skillId, props.AtkValue, position, ecb);
                        }

                        break;
                    }
                    case ECreatureType.Boss:
                    {
                        for (var i = 0; i < FightData.BossSkills.Count; i++)
                        {
                            var skillId = FightData.BossSkills[i];
                            SkillHelper.AddSkill(global.Entity, entity, skillId, props.AtkValue, position, ecb);
                        }

                        break;
                    }
                }

                //出生动画
                if (config.BornResId > 0)
                {
                    ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                    {
                        ResourceId = config.BornResId,
                        Pos = transform.Position,
                        Scale = creature.ValueRO.Type == ECreatureType.Boss ? 2.5f : transform.Scale,
                        //Caster = entity,
                    });

                    CreatureHelper.ProcessMasterActive(false, entity, _eventSetActive, ecb);

                    ecb.SetComponent(entity, new DisableMoveTag { DestroyDelay = 3 });
                    ecb.SetComponentEnabled<DisableMoveTag>(entity, true);

                    ecb.SetComponent(entity, new DisableHurtTag { ContTime = 3 });
                    ecb.SetComponentEnabled<DisableHurtTag>(entity, true);

                    ecb.SetComponentEnabled<DisableAutoTargetTag>(entity, true);

                    ecb.SetComponent(entity, new InBornTag { Timer = config.BornTime });
                    ecb.SetComponentEnabled<InBornTag>(entity, true);
                }
                else
                {
                    //绑定子弹
                    if (config.BindBulletId > 0)
                    {
                        BulletHelper.BindCollisionBullet(global, entity, props, _bindBulletLookup, _transformLookup, config.BindBulletId);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }

        private class RandomData
        {
            public int Id;
            public float Start;
            public float End;
            public int Count;
        }

        public static bool RandomDropItem(GlobalAspect global, CacheAspect cache, List<MonsterDropCfg> info, out DropItemConfig deploy, out int count)
        {
            deploy = default;
            count = 0;

            if (info == null || info.Count == 0)
            {
                return false;
            }

            var totalProb = 0f;
            var randomList = new List<RandomData>();
            var probStart = 0f;
            for (var i = 0; i < info.Count; i++)
            {
                var id = info[i].DropItemId;
                var prob = info[i].Prob;
                var dropCount = info[i].Count;

                randomList.Add(new RandomData
                {
                    Id = id,
                    Count = dropCount,
                    Start = probStart,
                    End = probStart + prob,
                });

                probStart += prob;
                totalProb += prob;
            }

            var randomValue = global.Random.ValueRW.Value.NextFloat(0f, totalProb);
            for (var i = 0; i < randomList.Count; i++)
            {
                var r = randomList[i];
                if (randomValue >= r.Start && randomValue < r.End)
                {
                    // 命中“空项”或无效项则视为不掉落
                    if (r.Id <= 0 || !cache.GetDropItemConfig(r.Id, out var config))
                    {
                        deploy = default;
                        count = 0;
                        return false;
                    }

                    deploy = config;
                    count = r.Count;
                    return true;
                }
            }

            return false;
        }
    }
}