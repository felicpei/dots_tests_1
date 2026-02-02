using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SkillSystemGroup))]
    public partial struct SkillTriggerSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<StatusHp> _hpLookup;
        [ReadOnly] private ComponentLookup<StatusMove> _moveLookup;
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTagLookup;

        [ReadOnly] private ComponentLookup<MonsterTarget> _monsterTargetLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<InBornTag> _inBornLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_PlayAnimation> _eventPlayAnimation;
        [ReadOnly] private ComponentLookup<HybridEvent_PlayMuzzleEffect> _eventPlayMuzzleEffect;
        [ReadOnly] private ComponentLookup<HybridEvent_StopMuzzleEffect> _eventStopMuzzleEffect;

        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private ComponentLookup<ServantLockForward> _servantLockForwardLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();

            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _hpLookup = state.GetComponentLookup<StatusHp>(true);
            _moveLookup = state.GetComponentLookup<StatusMove>(true);
            _creatureTagLookup = state.GetComponentLookup<CreatureTag>(true);
            _monsterTargetLookup = state.GetComponentLookup<MonsterTarget>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _inBornLookup = state.GetComponentLookup<InBornTag>(true);
            _eventPlayAnimation = state.GetComponentLookup<HybridEvent_PlayAnimation>(true);
            _eventPlayMuzzleEffect = state.GetComponentLookup<HybridEvent_PlayMuzzleEffect>(true);
            _eventStopMuzzleEffect = state.GetComponentLookup<HybridEvent_StopMuzzleEffect>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _servantLockForwardLookup = state.GetComponentLookup<ServantLockForward>(true);
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

            _summonLookup.Update(ref state);
            _hpLookup.Update(ref state);
            _moveLookup.Update(ref state);
            _creatureTagLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _monsterTargetLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _inBornLookup.Update(ref state);
            _eventPlayAnimation.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _eventPlayMuzzleEffect.Update(ref state);
            _eventStopMuzzleEffect.Update(ref state);
            _servantLockForwardLookup.Update(ref state);

            var currTime = global.Time;
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

            new TriggerJob
            {
                InMonsterPause = global.InMonsterPause,
                DeltaTime = deltaTime,
                CurrTime = currTime,
                GlobalEntity = global.Entity,
                Ecb = ecb.AsParallelWriter(),
                SummonLookup = _summonLookup,

                MoveLookup = _moveLookup,
                HpLookup = _hpLookup,
                CreatureTagLookup = _creatureTagLookup,
                MonsterTargetLookup = _monsterTargetLookup,
                TransformLookup = _transformLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                InBornLookup = _inBornLookup,
                EventPlayAnimation = _eventPlayAnimation,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
                EventPlayMuzzleEffect = _eventPlayMuzzleEffect,
                EventStopMuzzleEffect = _eventStopMuzzleEffect,
                ServantLockForwardLookup = _servantLockForwardLookup,
            }.ScheduleParallel();

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct TriggerJob : IJobEntity
        {
            public Entity CacheEntity;
            public bool InMonsterPause;
            public float DeltaTime;
            public float CurrTime;
            public Entity GlobalEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<StatusMove> MoveLookup;
            [ReadOnly] public ComponentLookup<StatusHp> HpLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTagLookup;

            [ReadOnly] public ComponentLookup<MonsterTarget> MonsterTargetLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<InBornTag> InBornLookup;
            [ReadOnly] public ComponentLookup<HybridEvent_PlayAnimation> EventPlayAnimation;
            [ReadOnly] public ComponentLookup<HybridEvent_PlayMuzzleEffect> EventPlayMuzzleEffect;
            [ReadOnly] public ComponentLookup<HybridEvent_StopMuzzleEffect> EventStopMuzzleEffect;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            [ReadOnly] public ComponentLookup<ServantLockForward> ServantLockForwardLookup;

            [BurstCompile]
            private void Execute(DynamicBuffer<SkillTriggerData> triggerBuffers, MasterCreature master, RefRW<SkillProperties> properties, RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                var startInfo = properties.ValueRO.StartInfo;

                if (!CacheHelper.GetSkillConfig(properties.ValueRO.Id, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                if (CreatureTagLookup.TryGetComponent(master.Value, out var masterTag))
                {
                    if (InMonsterPause && masterTag.TeamId == ETeamId.Monster)
                    {
                        return;
                    }
                }

                if (InBornLookup.HasComponent(master.Value) && InBornLookup.IsComponentEnabled(master.Value))
                {
                    return;
                }

                /*if (InFreezeLookup.HasComponent(master.Value) && InFreezeLookup.IsComponentEnabled(master.Value))
                {
                    return;
                }*/

                //从技能递归来的直接释放
                if (properties.ValueRO.RootSkillId > 0)
                {
                    if (!properties.ValueRO.IsOver)
                    {
                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags { CastDelay = startInfo.CastDelayTime, CreateTime = CurrTime });
                        Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                        properties.ValueRW.IsOver = true;
                    }
                    else
                    {
                        //5秒超时就destroy(防错处理)
                        if (CurrTime - properties.ValueRO.CreateTime > startInfo.CastDelayTime + 15)
                        {
                            BuffHelper.RemoveBuffByFrom(entity, EBuffFrom.Skill, config.Id, BuffEntitiesLookup, BuffTagLookup, Ecb, sortKey);
                            Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });
                        }
                    }

                    return;
                }

                //已经结束的技能，叠加层数后会立即再生效一次
                if (properties.ValueRO.IsOver)
                {
                    if (properties.ValueRO.AddedLayer > 0)
                    {
                        //还要判断下TriggerCount是否已经达到最大了，已经达到最大的，即使Over了也不叠加了
                        if (properties.ValueRO.TriggeredCount < config.MaxTriggerCount || config.MaxTriggerCount == 0)
                        {
                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags { ForceCastCount = properties.ValueRO.AddedLayer });
                            Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                        }
                    }

                    properties.ValueRW.AddedLayer = 0;
                }
                //技能未被标记未结束
                else
                {
                    var triggerConfig = config.Trigger;
                    switch (triggerConfig.Method)
                    {
                        //立即释放
                        case ESkillTrigger.OnCreate:
                        {
                            var hpLessPercent = triggerConfig.Param1;

                            var bValid = false;
                            if (hpLessPercent > 0)
                            {
                                if (HpLookup.TryGetComponent(master.Value, out var masterHp))
                                {
                                    var curPercent = masterHp.CurHp / AttrHelper.GetMaxHp(master.Value, AttrLookup, AttrModifyLookup, HpLookup, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                                    if (curPercent <= hpLessPercent)
                                    {
                                        bValid = true;
                                    }
                                }
                            }
                            else
                            {
                                bValid = true;
                            }

                            if (bValid)
                            {
                                //直接标记释放完毕
                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                            }

                            properties.ValueRW.IsOver = true;
                            break;
                        }
                        //进入Delay状态
                        case ESkillTrigger.Delay:
                        {
                            var delayTime = triggerConfig.Param1;

                            //这里延迟释放, 表示也会延迟决定目标
                            if (delayTime > properties.ValueRO.DelayTimer)
                            {
                                properties.ValueRW.DelayTimer += DeltaTime;
                            }
                            else
                            {
                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                properties.ValueRW.IsOver = true;
                            }

                            break;
                        }
                        case ESkillTrigger.IntervalMoveState:
                        {
                            var cd = triggerConfig.Param1;
                            var needMove = triggerConfig.Param2.ToInt() == 1;
                            if (cd <= 0)
                            {
                                cd = 1f;
                                Debug.LogError($"技能配置錯誤, Trigger IntervalMoveState 配置的 CD(param1) == 0, skillId:{config.Id}");
                            }

                            if (!MoveLookup.TryGetComponent(master.Value, out var masterMove))
                            {
                                break;
                            }

                            var skillCd = SkillHelper.CalcSkillCd(cd, config, master.Value, SummonLookup, AttrLookup, AttrModifyLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                            if (needMove)
                            {
                                if (masterMove.InMove)
                                {
                                    properties.ValueRW.CdTimer = properties.ValueRO.CdTimer + DeltaTime;
                                }
                                else
                                {
                                    properties.ValueRW.CdTimer = 0;
                                }
                            }
                            else
                            {
                                if (!masterMove.InMove)
                                {
                                    properties.ValueRW.CdTimer = properties.ValueRO.CdTimer + DeltaTime;
                                }
                                else
                                {
                                    properties.ValueRW.CdTimer = 0;
                                }
                            }

                            if (properties.ValueRO.CdTimer >= skillCd)
                            {
                                properties.ValueRW.CdTimer = 0;

                                //释放技能
                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                            }

                            break;
                        }
                        //被动CD触发
                        case ESkillTrigger.Interval:
                        {
                            var cd = triggerConfig.Param1;
                            var delayTime = triggerConfig.Param2;
                            var playAtkPrev = triggerConfig.Param3;
                            var aniType = (ESkillAniType)triggerConfig.Param4.ToInt(); //1: atk, 2: spell
                            var muzzleEffectId = triggerConfig.Param5.ToInt();

                            if (cd <= 0)
                            {
                                cd = 1f;
                                Debug.LogError($"技能配置錯誤, Trigger Interval 配置的 CD(param1) == 0, skillId:{config.Id}");
                            }

                            bool bAtkTimer;
                            if (properties.ValueRO.DelayTimer < delayTime)
                            {
                                properties.ValueRW.DelayTimer += DeltaTime;
                                bAtkTimer = playAtkPrev > 0 &&　properties.ValueRO.DelayTimer > delayTime - playAtkPrev;
                            }
                            else
                            {
                                properties.ValueRW.CdTimer = properties.ValueRO.CdTimer - DeltaTime;

                                if (properties.ValueRO.CdTimer <= 0)
                                {
                                    var skillCd = SkillHelper.CalcSkillCd(cd, config, master.Value, SummonLookup, AttrLookup, AttrModifyLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                                    properties.ValueRW.CdTimer = skillCd;
                                    properties.ValueRW.PlayedAtk = false;

                                    //无前摇直接释放技能
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);

                                    if (muzzleEffectId > 0 && EventStopMuzzleEffect.HasComponent(master.Value))
                                    {
                                        Ecb.SetComponent(sortKey, master.Value, new HybridEvent_StopMuzzleEffect { Delay = 0.1f });
                                        Ecb.SetComponentEnabled<HybridEvent_StopMuzzleEffect>(sortKey, master.Value, true);
                                    }

                                    if (aniType == ESkillAniType.Spell && EventPlayAnimation.HasComponent(master.Value))
                                    {
                                        Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = EAnimationType.SpellEnd });
                                        Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                    }

                                    bAtkTimer = false;
                                }
                                else
                                {
                                    bAtkTimer = playAtkPrev > 0 && properties.ValueRO.CdTimer < playAtkPrev;
                                }
                            }

                            //攻击动作
                            if (!properties.ValueRO.PlayedAtk && bAtkTimer)
                            {
                                properties.ValueRW.PlayedAtk = true;

                                if (aniType != ESkillAniType.None && EventPlayAnimation.HasComponent(master.Value))
                                {
                                    Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = aniType == ESkillAniType.Atk ? EAnimationType.Atk : EAnimationType.SpellStart });
                                    Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                }

                                if (ServantLockForwardLookup.HasComponent(master.Value))
                                {
                                    Ecb.SetComponent(sortKey, master.Value, new ServantLockForward
                                    {
                                        ContTime = playAtkPrev + 0.2f
                                    });
                                    Ecb.SetComponentEnabled<ServantLockForward>(sortKey, master.Value, true);
                                }

                                if (muzzleEffectId > 0 && EventPlayMuzzleEffect.HasComponent(master.Value))
                                {
                                    Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayMuzzleEffect { EffectId = muzzleEffectId, Delay = 0.2f });
                                    Ecb.SetComponentEnabled<HybridEvent_PlayMuzzleEffect>(sortKey, master.Value, true);
                                }
                            }

                            break;
                        }
                        //2层CD机制
                        case ESkillTrigger.IntervalDual:
                        {
                            var mainCD = triggerConfig.Param1;
                            var subCD = triggerConfig.Param2;
                            var sourceSubCount = triggerConfig.Param3;
                            var delayTime = triggerConfig.Param4;
                            var aniType = (ESkillAniType)triggerConfig.Param5.ToInt(); //1: atk, 2: spell
                            var prevAniTime = triggerConfig.Param6;
                            var muzzleEffectId = triggerConfig.Param7.ToInt();
                            var playAniSpellInSub = triggerConfig.Param8.ToInt() == 1;

                            //防錯
                            if (mainCD <= 0)
                            {
                                mainCD = 1f;
                                Debug.LogError($"技能配置錯誤, Trigger IntervalDual 配置的 MainCD(param1) == 0, skillId:{config.Id}");
                            }

                            if (subCD <= 0)
                            {
                                subCD = 0.1f;
                                Debug.LogError($"技能配置錯誤, Trigger IntervalDual 配置的 SubCD(param2) == 0, skillId:{config.Id}");
                            }

                            //subCount buff
                            var addValue = BuffHelper.GetBuffAddValue(master.Value, SummonLookup,
                                BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillIntervalDualCastCount, config.ClassId, config.Id);

                            //attr shoot count
                            var attrAddValue = AttrHelper.GetAttr(master.Value, EAttr.ShootCount, AttrLookup, AttrModifyLookup, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                            var maxAttrAdd = AttrHelper.GetMax(EAttr.ShootCount, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                            if (attrAddValue > maxAttrAdd)
                            {
                                attrAddValue = maxAttrAdd;
                            }

                            var totalExtraAdd = addValue + attrAddValue;

                            //正在SUB CD状态中
                            if (properties.ValueRO.InSubCdStatus)
                            {
                                properties.ValueRW.SubCdTimer = properties.ValueRO.SubCdTimer - DeltaTime;
                                if (properties.ValueRO.SubCdTimer <= 0)
                                {
                                    var subCdTime = subCD;

                                    //根据增加的数量减少subCd
                                    if (totalExtraAdd > 0)
                                    {
                                        var sourceTotalTime = sourceSubCount * subCD;
                                        subCdTime = sourceTotalTime / (sourceSubCount + totalExtraAdd);
                                    }

                                    //释放一次技能
                                    properties.ValueRW.CountFlag = properties.ValueRO.CountFlag + 1;
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                    {
                                        Counter = (int)properties.ValueRO.CountFlag
                                    });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);


                                    //进入下一次CD
                                    properties.ValueRW.SubCdTimer = subCdTime;

                                    //到最大次数后，停止技能
                                    if (properties.ValueRO.CountFlag >= sourceSubCount + totalExtraAdd)
                                    {
                                        properties.ValueRW.InSubCdStatus = false;
                                        properties.ValueRW.CountFlag = 0;

                                        if (playAniSpellInSub)
                                        {
                                            if (EventPlayAnimation.HasComponent(master.Value))
                                            {
                                                Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = EAnimationType.SpellEnd });
                                                Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                            }

                                            if (ServantLockForwardLookup.HasComponent(master.Value))
                                            {
                                                Ecb.SetComponent(sortKey, master.Value, new ServantLockForward
                                                {
                                                    ContTime = 0.1f
                                                });
                                                Ecb.SetComponentEnabled<ServantLockForward>(sortKey, master.Value, true);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //主要CD到了
                                bool bPlayAtkTimer;
                                if (delayTime > properties.ValueRO.DelayTimer)
                                {
                                    properties.ValueRW.DelayTimer += DeltaTime;
                                    bPlayAtkTimer = prevAniTime > 0 && properties.ValueRO.DelayTimer > delayTime - prevAniTime;
                                }
                                else
                                {
                                    properties.ValueRW.CdTimer = properties.ValueRO.CdTimer - DeltaTime;
                                    if (properties.ValueRO.CdTimer <= 0)
                                    {
                                        var mainCdTime = SkillHelper.CalcSkillCd(mainCD, config, master.Value, SummonLookup, AttrLookup, AttrModifyLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                                        properties.ValueRW.CdTimer = mainCdTime;

                                        if (playAniSpellInSub)
                                        {
                                            if (EventPlayAnimation.HasComponent(master.Value))
                                            {
                                                Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = EAnimationType.SpellStart });
                                                Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                            }

                                            if (ServantLockForwardLookup.HasComponent(master.Value))
                                            {
                                                Ecb.SetComponent(sortKey, master.Value, new ServantLockForward
                                                {
                                                    ContTime = 10
                                                });
                                                Ecb.SetComponentEnabled<ServantLockForward>(sortKey, master.Value, true);
                                            }
                                        }

                                        //显示主角Reload条
                                        //var subCdTime = SkillHelper.CalcSkillCd(subCD, config, master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, true);
                                        properties.ValueRW.InSubCdStatus = true;
                                        properties.ValueRW.AniFlag = false;

                                        bPlayAtkTimer = prevAniTime <= 0;

                                        if (aniType == ESkillAniType.Spell && EventPlayAnimation.HasComponent(master.Value))
                                        {
                                            Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = EAnimationType.SpellEnd });
                                            Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                        }

                                        if (muzzleEffectId > 0 && EventStopMuzzleEffect.HasComponent(master.Value))
                                        {
                                            Ecb.SetComponent(sortKey, master.Value, new HybridEvent_StopMuzzleEffect { Delay = 0.1f });
                                            Ecb.SetComponentEnabled<HybridEvent_StopMuzzleEffect>(sortKey, master.Value, true);
                                        }
                                    }
                                    else
                                    {
                                        bPlayAtkTimer = prevAniTime > 0 && properties.ValueRO.CdTimer < prevAniTime;
                                    }
                                }

                                if (aniType != ESkillAniType.None && bPlayAtkTimer)
                                {
                                    if (!properties.ValueRO.AniFlag)
                                    {
                                        properties.ValueRW.AniFlag = true;
                                        if (EventPlayAnimation.HasComponent(master.Value))
                                        {
                                            Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = aniType == ESkillAniType.Atk ? EAnimationType.Atk : EAnimationType.SpellStart });
                                            Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                        }

                                        if (muzzleEffectId > 0 && EventPlayMuzzleEffect.HasComponent(master.Value))
                                        {
                                            Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayMuzzleEffect { EffectId = muzzleEffectId, Delay = 0.2f });
                                            Ecb.SetComponentEnabled<HybridEvent_PlayMuzzleEffect>(sortKey, master.Value, true);
                                        }

                                        if (ServantLockForwardLookup.HasComponent(master.Value))
                                        {
                                            Ecb.SetComponent(sortKey, master.Value, new ServantLockForward
                                            {
                                                ContTime = prevAniTime + 0.2f
                                            });
                                            Ecb.SetComponentEnabled<ServantLockForward>(sortKey, master.Value, true);
                                        }
                                    }
                                }
                            }

                            break;
                        }

                        //怪物接近仇恨目标
                        case ESkillTrigger.OnNearbyHatred:
                        {
                            var prob = triggerConfig.Param1;
                            var needLessDist = triggerConfig.Param2;
                            var cd = triggerConfig.Param3;
                            var delayTime = triggerConfig.Param4;
                            var minDistLimit = triggerConfig.Param5;

                            if (prob <= 0)
                            {
                                prob = 1f;
                            }

                            if (delayTime > properties.ValueRO.DelayTimer)
                            {
                                properties.ValueRW.DelayTimer += DeltaTime;
                            }
                            else
                            {
                                properties.ValueRW.CdTimer = properties.ValueRO.CdTimer - DeltaTime;
                                if (properties.ValueRO.CdTimer <= 0)
                                {
                                    if (MonsterTargetLookup.TryGetComponent(master.Value, out var target) && target.HasTarget)
                                    {
                                        if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                                        {
                                            var dist = math.distance(target.Pos, masterTrans.Position);
                                            bool distValid;
                                            if (minDistLimit > 0)
                                            {
                                                distValid = dist >= minDistLimit && dist < needLessDist;
                                            }
                                            else
                                            {
                                                distValid = dist < needLessDist;
                                            }

                                            if (distValid)
                                            {
                                                properties.ValueRW.CountFlag = dist;

                                                //计算概率是否释放
                                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                                {
                                                    //计算CD
                                                    var cdTime = SkillHelper.CalcSkillCd(cd, config, master.Value, SummonLookup, AttrLookup, AttrModifyLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                                                    properties.ValueRW.CdTimer = cdTime;

                                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        }

                        //被击触发
                        case ESkillTrigger.OnHit:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var data in triggerBuffers)
                            {
                                if (data.Type == ESkillTrigger.OnHit)
                                {
                                    if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                        {
                                            TriggerType = ESkillTrigger.OnHit,
                                            EntityValue = data.Entity
                                        });
                                        Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                    }
                                }
                            }

                            break;
                        }

                        //复活触发
                        case ESkillTrigger.OnRevive:
                        {
                            var bRevive = false;
                            foreach (var data in triggerBuffers)
                            {
                                if (data.Type == ESkillTrigger.OnRevive)
                                {
                                    bRevive = true;
                                    break;
                                }
                            }

                            if (bRevive)
                            {
                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                            }

                            break;
                        }
                        //召唤物击中敌人时
                        case ESkillTrigger.SummonHitEnemy:
                        {
                            var prob = triggerConfig.Param1;
                            var needMonsterId = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.SummonHitEnemy)
                                {
                                    continue;
                                }

                                if (needMonsterId != 0 && needMonsterId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                    {
                                        TriggerType = ESkillTrigger.SummonHitEnemy,
                                        EntityValue = buffer.Entity,
                                    });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //子弹击中敌人时
                        case ESkillTrigger.BulletHitEnemy:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = (int)triggerConfig.Param2;
                            var needBulletId = (int)triggerConfig.Param3;
                            var needBulletClass = (int)triggerConfig.Param5;
                            var enemyNeedBuffType = (EBuffType)triggerConfig.Param6;
                            var containsSummon = triggerConfig.Param7.ToInt() == 1;

                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.BulletHitEnemy)
                                {
                                    continue;
                                }


                                //召唤物子弹是否能触发
                                if (!containsSummon && buffer.BoolValue1)
                                {
                                    continue;
                                }

                                //子弹ID是否符合
                                if (needBulletId != 0 && needBulletId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                //看bulletClass是否符合
                                if (needBulletClass != 0 && needBulletClass != buffer.IntValue2)
                                {
                                    continue;
                                }

                                //检查怪身上已有的buff类型
                                var bExistTypeCheck = true;
                                if (enemyNeedBuffType != EBuffType.None)
                                {
                                    bExistTypeCheck = TransformLookup.HasComponent(buffer.Entity) &&
                                                      BuffHelper.CheckHasBuffType(enemyNeedBuffType, buffer.Entity, BuffEntitiesLookup, BuffTagLookup);
                                }

                                if (bExistTypeCheck)
                                {
                                    if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                    {
                                        //记录子弹ID
                                        properties.ValueRW.SavedBulletId = buffer.IntValue1;
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                        {
                                            TriggerType = ESkillTrigger.BulletHitEnemy,
                                            EntityValue = buffer.Entity,
                                        });
                                        Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                    }
                                }
                            }

                            break;
                        }
                        //给敌人上buff
                        case ESkillTrigger.AddBuffToEnemy:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = (int)triggerConfig.Param2;
                            var needBuffType_1 = triggerConfig.Param3.ToInt();
                            var needBuffType_2 = triggerConfig.Param4.ToInt();
                            var enemyBuffType_1 = triggerConfig.Param5.ToInt();
                            var enemyBuffType_2 = triggerConfig.Param6.ToInt();
                            var enemyBuffType_3 = triggerConfig.Param7.ToInt();
                            var enemyBuffType_4 = triggerConfig.Param8.ToInt();

                            if (prob <= 0) prob = 1f;

                            var needTypes = new NativeList<int>(Allocator.Temp);
                            if (needBuffType_1 > 0) needTypes.Add(needBuffType_1);
                            if (needBuffType_2 > 0) needTypes.Add(needBuffType_2);

                            var enemyTypes = new NativeList<int>(Allocator.Temp);
                            if (enemyBuffType_1 > 0) enemyTypes.Add(enemyBuffType_1);
                            if (enemyBuffType_2 > 0) enemyTypes.Add(enemyBuffType_2);
                            if (enemyBuffType_3 > 0) enemyTypes.Add(enemyBuffType_3);
                            if (enemyBuffType_4 > 0) enemyTypes.Add(enemyBuffType_4);

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.AddBuffToEnemy)
                                {
                                    continue;
                                }

                                //检查附加的buff类型要求
                                var bCheckNeed = needTypes.Length == 0;
                                for (var j = 0; j < needTypes.Length; j++)
                                {
                                    if (buffer.IntValue1 == needTypes[j])
                                    {
                                        bCheckNeed = true;
                                        break;
                                    }
                                }

                                if (!bCheckNeed)
                                {
                                    continue;
                                }

                                //检查敌人身上的buff要求
                                var bCheckEnemy = enemyTypes.Length == 0;
                                for (var j = 0; j < enemyTypes.Length; j++)
                                {
                                    if (BuffHelper.CheckHasBuffType((EBuffType)enemyTypes[j], buffer.Entity, BuffEntitiesLookup, BuffTagLookup))
                                    {
                                        bCheckEnemy = true;
                                        break;
                                    }
                                }

                                if (!bCheckEnemy)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                    {
                                        TriggerType = ESkillTrigger.AddBuffToEnemy,
                                        EntityValue = buffer.Entity,
                                    });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            needTypes.Dispose();
                            enemyTypes.Dispose();
                            break;
                        }
                        //击杀敌人时
                        case ESkillTrigger.KillEnemy:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = (int)triggerConfig.Param2;
                            var enemyNeedBuffType = (EBuffType)triggerConfig.Param3;
                            var needBulletId = (int)triggerConfig.Param4;
                            var needBulletClass = (int)triggerConfig.Param5;
                            var containsSummon = triggerConfig.Param6.ToInt() == 1;
                            var monsterTypeCond = triggerConfig.Param7.ToInt();

                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.KillEnemy)
                                {
                                    continue;
                                }

                                //检查是否包含召唤物
                                if (!containsSummon && buffer.BoolValue1)
                                {
                                    continue;
                                }

                                //仅boss触发
                                if (monsterTypeCond == 1 && (ECreatureType)buffer.IntValue1 != ECreatureType.Boss)
                                {
                                    continue;
                                }

                                //仅boss或精英触发
                                if (monsterTypeCond == 2 && (ECreatureType)buffer.IntValue1 != ECreatureType.Boss && (ECreatureType)buffer.IntValue1 != ECreatureType.Elite)
                                {
                                    continue;
                                }

                                //检查怪身上已有的buff类型
                                var bBuffCheck = true;
                                if (enemyNeedBuffType != EBuffType.None)
                                {
                                    if (buffer.From == EBulletFrom.Buff && buffer.FromParam == (int)enemyNeedBuffType)
                                    {
                                        bBuffCheck = false;
                                    }
                                    else
                                    {
                                        bBuffCheck = BuffHelper.CheckHasBuffType(enemyNeedBuffType, buffer.Entity, BuffEntitiesLookup, BuffTagLookup);
                                    }
                                }

                                var bBulletCheck = false;
                                if (needBulletId > 0)
                                {
                                    if (buffer.From is EBulletFrom.Default && buffer.FromParam == needBulletId)
                                    {
                                        bBulletCheck = true;
                                    }
                                }
                                else if (needBulletClass > 0)
                                {
                                    if (buffer.From is EBulletFrom.Default)
                                    {
                                        var killUseBulletId = buffer.FromParam;

                                        //尝试获得子弹config
                                        if (CacheHelper.GetBulletConfig(killUseBulletId, CacheEntity, CacheLookup, out var bulletConfig))
                                        {
                                            if (needBulletClass == bulletConfig.ClassId)
                                            {
                                                bBulletCheck = true;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    bBulletCheck = true;
                                }

                                if (bBulletCheck && bBuffCheck)
                                {
                                    if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                        {
                                            TriggerType = ESkillTrigger.KillEnemy,
                                            EntityValue = buffer.Entity
                                        });
                                        Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                    }
                                }
                            }

                            break;
                        }
                        //当释放某个技能的时候
                        case ESkillTrigger.OnSkillCast:
                        {
                            var skillClassId = (int)triggerConfig.Param1;
                            var skillId = triggerConfig.Param2.ToInt();
                            var delay = triggerConfig.Param3;
                            var prob = triggerConfig.Param4;

                            if (prob <= 0) prob = 1;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnSkillCast)
                                {
                                    continue;
                                }

                                if (skillClassId != 0 && skillClassId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (skillId != 0 && skillId != buffer.IntValue2)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags { CastDelay = delay, CreateTime = CurrTime });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //释放完某个技能的时候
                        case ESkillTrigger.AfterSkillCast:
                        {
                            var skillClassId = (int)triggerConfig.Param1;
                            var skillId = triggerConfig.Param2.ToInt();
                            var delay = triggerConfig.Param3;
                            var bChangeStartInfo = (int)triggerConfig.Param4 == 1;
                            var prob = triggerConfig.Param5;
                            var needCount = triggerConfig.Param6.ToInt();
                            if (prob <= 0) prob = 1f;


                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.AfterSkillCast)
                                {
                                    continue;
                                }

                                if (skillClassId != 0 && skillClassId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (skillId != 0 && skillId != buffer.IntValue2)
                                {
                                    continue;
                                }

                                if (bChangeStartInfo)
                                {
                                    properties.ValueRW.StartInfo.StartEntity = buffer.Entity;
                                    properties.ValueRW.StartInfo.StartPos = buffer.Pos;
                                }

                                properties.ValueRW.PrevTarget = new SkillPrevTarget
                                {
                                    Enable = true,
                                    Pos = buffer.Pos,
                                    Target = buffer.Entity,
                                };

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags { CastDelay = delay, CreateTime = CurrTime });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }

                        //当血量少于百分之多少时
                        case ESkillTrigger.OnHpLessThan:
                        {
                            var hpPercent = triggerConfig.Param1;
                            var hpValue = triggerConfig.Param2;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnHpChanged)
                                {
                                    continue;
                                }

                                var beforeHpPercent = buffer.FloatValue1;
                                var afterHpPercent = buffer.FloatValue2;
                                var beforeHpValue = buffer.HpBefore;
                                var afterHpPValue = buffer.HpAfter;

                                if (beforeHpPercent > hpPercent && afterHpPercent <= hpPercent)
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                                else if (beforeHpValue > hpValue && afterHpPValue <= hpValue)
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //当血量大于百分之多少时
                        case ESkillTrigger.OnHpMoreThan:
                        {
                            var hpPercent = triggerConfig.Param1;
                            var hpValue = triggerConfig.Param2;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnHpChanged)
                                {
                                    continue;
                                }

                                var beforeHpPercent = buffer.FloatValue1;
                                var afterHpPercent = buffer.FloatValue2;
                                var beforeHpValue = buffer.HpBefore;
                                var afterHpValue = buffer.HpAfter;

                                if (hpPercent > 0)
                                {
                                    if (beforeHpPercent < hpPercent && afterHpPercent >= hpPercent)
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                        Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                    }
                                }
                                else if (hpValue > 0)
                                {
                                    if (beforeHpValue < hpValue && afterHpValue >= hpValue)
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                        Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                    }
                                }
                            }

                            break;
                        }
                        //掉血时触发
                        case ESkillTrigger.OnHpLoss:
                        {
                            var prob = triggerConfig.Param1;
                            var needLostPercent = triggerConfig.Param2;
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnHpChanged)
                                {
                                    continue;
                                }

                                var beforeHpValue = buffer.HpBefore;
                                var afterHpValue = buffer.HpAfter;

                                var lostValue = beforeHpValue - afterHpValue;
                                if (lostValue > 0)
                                {
                                    properties.ValueRW.CountFlag = properties.ValueRO.CountFlag + lostValue;

                                    //看是否达到percent要求
                                    var lostPercent = 0f;
                                    var maxHp = AttrHelper.GetMaxHp(master.Value, AttrLookup, AttrModifyLookup, HpLookup, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                                    if (maxHp > 0)
                                    {
                                        lostPercent = properties.ValueRO.CountFlag / maxHp;
                                    }

                                    if (lostPercent >= needLostPercent)
                                    {
                                        properties.ValueRW.CountFlag = 0;

                                        var probFactor = BuffHelper.GetBuffAddFactor(master.Value, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillTriggerProb, config.ClassId, config.Id);
                                        prob += probFactor;

                                        if (random.ValueRW.Value.NextFloat(0f, 1f) < prob)
                                        {
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                            Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                        }
                                    }
                                }
                            }

                            break;
                        }
                        //血量变化时触发
                        case ESkillTrigger.OnHpChanged:
                        {
                            var prob = triggerConfig.Param1;
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnHpChanged)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //受到治疗后
                        case ESkillTrigger.AfterCure:
                        {
                            var prob = triggerConfig.Param1;
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.AfterCure)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //移动1M
                        case ESkillTrigger.OnMove1M:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnMove1M)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //拾取经验时
                        case ESkillTrigger.OnPickupExp:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnPickupExp)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //拾取金币
                        case ESkillTrigger.OnPickupGold:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnPickupGold)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        case ESkillTrigger.OnPickupHp:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            var needHpGreater = triggerConfig.Param3;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnPickupHp)
                                {
                                    continue;
                                }

                                if (buffer.FloatValue1 < needHpGreater)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        case ESkillTrigger.OnPickupItem:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            var dropItemId = triggerConfig.Param3.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnPickupItem)
                                {
                                    continue;
                                }

                                if (dropItemId != 0 && dropItemId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }

                        //升级时
                        case ESkillTrigger.OnLevelUp:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnLevelUp)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //护盾创建
                        case ESkillTrigger.OnShieldCreate:
                        {
                            var prob = triggerConfig.Param1;
                            var shieldId = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnShieldCreate)
                                {
                                    continue;
                                }

                                if (shieldId != 0 && buffer.IntValue1 != shieldId)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //护盾破碎
                        case ESkillTrigger.OnShieldBreak:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            var shieldId = triggerConfig.Param3.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnShieldBreak)
                                {
                                    continue;
                                }

                                if (shieldId != 0 && buffer.IntValue1 != shieldId)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }

                        case ESkillTrigger.OnShieldDisappear:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            var shieldId = triggerConfig.Param3.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnShieldDisappear)
                                {
                                    continue;
                                }

                                if (shieldId != 0 && buffer.IntValue1 != shieldId)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }

                        //on dead
                        case ESkillTrigger.OnDead:
                        {
                            var prob = triggerConfig.Param1;
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnDead)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        case ESkillTrigger.OnBulletBounce:
                        {
                            var prob = triggerConfig.Param1;
                            var bulletId = triggerConfig.Param2.ToInt();
                            var bulletClassId = triggerConfig.Param3.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnBulletBounce)
                                {
                                    continue;
                                }

                                if (bulletId != 0 && buffer.IntValue1 != bulletId)
                                {
                                    continue;
                                }

                                if (bulletClassId != 0 && buffer.IntValue2 != bulletClassId)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, 0, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                    {
                                        TriggerType = ESkillTrigger.OnBulletBounce,
                                        PosValue = buffer.Pos,
                                        Forward = buffer.Forward,
                                    });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }

                        //当特定的子弹爆炸时
                        case ESkillTrigger.OnBulletExplosion:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            var bulletId = triggerConfig.Param3.ToInt();
                            var bulletClassId = triggerConfig.Param4.ToInt();
                            if (prob <= 0) prob = 1f;


                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnBulletExplosion)
                                {
                                    continue;
                                }

                                if (bulletId != 0 && bulletId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (bulletClassId != 0 && bulletClassId != buffer.IntValue2)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                    {
                                        TriggerType = ESkillTrigger.OnBulletExplosion,
                                        PosValue = buffer.Pos,
                                    });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //当子弹创建时
                        case ESkillTrigger.OnBulletCreate:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            var bulletId = triggerConfig.Param3.ToInt();
                            var bulletClassId = triggerConfig.Param4.ToInt();
                            var ignoreSelf = triggerConfig.Param5.ToInt() == 1;
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnBulletCreate)
                                {
                                    continue;
                                }

                                if (bulletId > 0 && bulletId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (bulletClassId != 0 && bulletClassId != buffer.IntValue2)
                                {
                                    continue;
                                }

                                if (ignoreSelf && buffer.FloatValue1 > 0 && (int)buffer.FloatValue1 == config.Id)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                    {
                                        TriggerType = ESkillTrigger.OnBulletCreate,
                                        PosValue = buffer.Pos,
                                    });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        //暴击时
                        case ESkillTrigger.OnCrit:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            var containsSummon = triggerConfig.Param3.ToInt() == 1;
                            var needBulletId = triggerConfig.Param4.ToInt();
                            var bulletClassId = triggerConfig.Param5.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnCrit)
                                {
                                    continue;
                                }

                                //看是否包含召唤物
                                if (!containsSummon && buffer.BoolValue1)
                                {
                                    continue;
                                }

                                if (needBulletId != 0 && needBulletId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (bulletClassId != 0 && bulletClassId != buffer.IntValue2)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                    {
                                        TriggerType = ESkillTrigger.OnCrit,
                                        PosValue = buffer.Pos,
                                    });
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }

                        case ESkillTrigger.OnDodge:
                        {
                            var prob = triggerConfig.Param1;
                            var needCount = triggerConfig.Param2.ToInt();
                            if (prob <= 0) prob = 1f;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnDodge)
                                {
                                    continue;
                                }

                                if (CalcCountAndProb(properties, random, needCount, prob, SummonLookup, config, master.Value, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                    Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                                }
                            }

                            break;
                        }
                        case ESkillTrigger.OnSkillRecursionOver:
                        {
                            var skillClassId = triggerConfig.Param1.ToInt();
                            var skillId = triggerConfig.Param2.ToInt();
                            var delay = triggerConfig.Param3;

                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnSkillRecursionOver)
                                {
                                    continue;
                                }

                                if (skillClassId != 0 && skillClassId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                if (skillId != 0 && skillId != buffer.IntValue2)
                                {
                                    continue;
                                }

                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags { CastDelay = delay, CreateTime = CurrTime });
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                            }

                            break;
                        }
                        case ESkillTrigger.OnSummonMonster:
                        {
                            var monsterId = triggerConfig.Param1.ToInt();
                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnSummonMonster)
                                {
                                    continue;
                                }

                                if (monsterId != 0 && monsterId != buffer.IntValue1)
                                {
                                    continue;
                                }

                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags
                                {
                                    TriggerType = ESkillTrigger.OnSummonMonster,
                                    EntityValue = buffer.Entity
                                });
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                            }

                            break;
                        }
                        case ESkillTrigger.OnTheWorldEnd:
                        {
                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnTheWorldEnd)
                                {
                                    continue;
                                }

                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                            }

                            break;
                        }
                        case ESkillTrigger.OnDashHitObstacle:
                        {
                            foreach (var buffer in triggerBuffers)
                            {
                                if (buffer.Type != ESkillTrigger.OnDashHitObstacle)
                                {
                                    continue;
                                }

                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetTags());
                                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, true);
                            }

                            break;
                        }
                        case ESkillTrigger.None:
                        {
                            break;
                        }
                        default:
                        {
                            Debug.LogError($"配置了不存在的SkillTrigger:{triggerConfig.Method}");
                            break;
                        }
                    }
                }

                //clear trigger buffers
                triggerBuffers.Clear();
            }

            private static bool CalcCountAndProb(RefRW<SkillProperties> properties, RefRW<RandomSeed> random, int needCount, float prob,
                ComponentLookup<StatusSummon> summonLookup, SkillConfig config, Entity master,
                BufferLookup<BuffEntities> buffEntitiesLookup,
                ComponentLookup<BuffTag> buffTagLookup,
                ComponentLookup<BuffCommonData> buffCommonLookup)
            {
                properties.ValueRW.CountFlag = properties.ValueRO.CountFlag + 1;
                if (properties.ValueRO.CountFlag >= needCount)
                {
                    properties.ValueRW.CountFlag = 0;

                    var probFactor = BuffHelper.GetBuffAddFactor(master, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.SkillTriggerProb, config.ClassId, config.Id);
                    prob += probFactor;

                    if (random.ValueRW.Value.NextFloat(0f, 1f) < prob)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}