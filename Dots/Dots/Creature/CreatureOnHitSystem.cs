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
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureOnHitSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<StatusHp> _hpLookup;
        [ReadOnly] private ComponentLookup<CreatureProps> _propsLookup;
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTagLookup;
        [ReadOnly] private ComponentLookup<StatusMove> _creatureMoveLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<DisableHurtTag> _disableHurtLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<EnterDieTag> _enterDieLookup;
        [ReadOnly] private ComponentLookup<InDeadState> _inDeadLookup;
        [ReadOnly] private ComponentLookup<LocalPlayerTag> _localPlayerLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<BuffTransfusion> _buffTransfusion;
        [ReadOnly] private ComponentLookup<ShieldProperties> _shieldLookup;
        [ReadOnly] private ComponentLookup<BulletProperties> _bulletLookup;
        [ReadOnly] private ComponentLookup<BulletAtkValue> _bulletAtkLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();

            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _hpLookup = state.GetComponentLookup<StatusHp>(true);
            _propsLookup = state.GetComponentLookup<CreatureProps>(true);
            _creatureTagLookup = state.GetComponentLookup<CreatureTag>(true);
            _creatureMoveLookup = state.GetComponentLookup<StatusMove>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _disableHurtLookup = state.GetComponentLookup<DisableHurtTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _enterDieLookup = state.GetComponentLookup<EnterDieTag>(true);
            _inDeadLookup = state.GetComponentLookup<InDeadState>(true);
            _localPlayerLookup = state.GetComponentLookup<LocalPlayerTag>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _buffTransfusion = state.GetComponentLookup<BuffTransfusion>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _summonEntitiesLookup = state.GetBufferLookup<SummonEntities>(true);
            _bulletLookup = state.GetComponentLookup<BulletProperties>(true);
            _bulletAtkLookup = state.GetComponentLookup<BulletAtkValue>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);

            _shieldLookup = state.GetComponentLookup<ShieldProperties>();
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
            _propsLookup.Update(ref state);
            _creatureTagLookup.Update(ref state);
            _creatureMoveLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _disableHurtLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _enterDieLookup.Update(ref state);
            _inDeadLookup.Update(ref state);
            _localPlayerLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _buffTransfusion.Update(ref state);
            _skillTagLookup.Update(ref state);
            _summonEntitiesLookup.Update(ref state);
            _shieldLookup.Update(ref state);
            _bulletLookup.Update(ref state);
            _bulletAtkLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            foreach (var (damageBuffers, hpInfo, creatureTag, center, creatureElement, entity)
                     in SystemAPI.Query<DynamicBuffer<DamageBuffer>, RefRW<StatusHp>, CreatureTag, StatusCenter, RefRW<BindingElement>>().WithEntityAccess())
            {
                //已销毁或死亡的不处理
                if (!_transformLookup.TryGetComponent(entity, out var localTransform) || _enterDieLookup.IsComponentEnabled(entity) || _inDeadLookup.IsComponentEnabled(entity))
                {
                    damageBuffers.Clear();
                    ecb.SetComponentEnabled<DamageBuffer>(entity, false);
                    continue;
                }

                for (var j = damageBuffers.Length - 1; j >= 0; j--)
                {
                    var buffer = damageBuffers[j];
                    damageBuffers.RemoveAt(j);

                    //如果无敌，则不处理伤害
                    if (_disableHurtLookup.IsComponentEnabled(entity) || 
                        BuffHelper.GetHasBuff(entity, _summonLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, EBuffType.Invincible))
                    {
                        continue;
                    }

                    if (buffer.DamageFactor <= 0)
                    {
                        //Debug.LogError($"calc damage error, damage factor less 0, factor:{buffer.DamageFactor}");
                        continue;
                    }

                    //获得子弹信息
                    if (!_bulletLookup.TryGetComponent(buffer.Bullet, out var bulletProperties) ||
                        !_bulletAtkLookup.TryGetComponent(buffer.Bullet, out var bulletAtk) ||
                        !cache.GetBulletConfig(bulletProperties.BulletId, out var bulletConfig))
                    {
                        continue;
                    }

                    //被击方闪避处理
                    if (bulletProperties.From != EBulletFrom.Buff && ProcessDodge(entity, _summonLookup, global, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, _attrLookup, _attrModifyLookup, ecb))
                    {
                        SkillHelper.DoSkillTrigger(entity, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnDodge), ecb);
                        continue;
                    }

                    var attacker = bulletProperties.MasterCreature;
                    var damageCalc = bulletConfig.DamageCalc;
                    var attackerElementId = bulletProperties.ElementId;
                    var hitMaxHp = AttrHelper.GetMaxHp(entity, _attrLookup, _attrModifyLookup, _hpLookup, _summonLookup,  _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                    var beforeHpPercent = hpInfo.ValueRO.CurHp / hitMaxHp;
                    
                    //计算初始伤害
                    var sourceDamage = DamageHelper.CalcDamage(bulletProperties, entity, attacker, damageCalc,
                        buffer.DamageFactor, bulletAtk.Atk, global, cache,
                        _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, 
                        _hpLookup, _propsLookup, _creatureTagLookup,
                        _summonLookup, _creatureMoveLookup, _shieldLookup, _attrLookup, _attrModifyLookup, out var canCrit);

                    var damage = sourceDamage;
                    var damageNumberType = EDamageNumber.None;
                    EAgainstType againstType;

                    //元素克制伤害处理
                    var elementFactor = cache.GetAgainstFactor(attackerElementId, creatureTag.BelongElementId);
                    if (elementFactor > 1f)
                    {
                        againstType = EAgainstType.Weak;
                    }
                    else if (elementFactor < 1f)
                    {
                        againstType = EAgainstType.Strong;
                    }
                    else
                    {
                        againstType = EAgainstType.None;
                    }

                    //buff 元素抗性 %
                    var elementHurtFactor = BuffHelper.GetBuffAddFactor(entity, _summonLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, EBuffType.ElementHurt, (int)creatureTag.BelongElementId);
                    elementFactor += elementHurtFactor;

                    damage *= elementFactor;

                    //buff 不处理 暴击,闪避, 元素反应
                    var reaction = EElementReaction.None;
                    if (bulletProperties.From != EBulletFrom.Buff) 
                    {
                        //执行元素反应
                        if (bulletProperties.From != EBulletFrom.ElementReaction)
                        {
                            reaction = TryElementReaction(cache, global, attackerElementId, creatureElement, entity, localTransform, ecb, out var damageFactor);
                            if (!Mathf.Approximately(damageFactor, 1f))
                            {
                                damage *= damageFactor;
                            }

                            //remove binding element
                            if (reaction != EElementReaction.None)
                            {
                                //remove effect
                                ecb.AppendToBuffer(global.Entity, new DestroyEffectByFrom
                                {
                                    Parent = entity,
                                    From = EEffectFrom.Element,
                                    FromId = (int)creatureElement.ValueRO.Value
                                });
                                creatureElement.ValueRW.Value = EElement.None;
                            }

                            //处理元素反应
                            switch (reaction)
                            {
                                case EElementReaction.Overloaded:
                                {
                                    //(通过子弹伤害)
                                    //过载, 范围火元素爆炸伤害
                                    var bulletBuffer = new BulletCreateBuffer
                                    {
                                        From = EBulletFrom.ElementReaction,
                                        FromId = (int)reaction,
                                        BulletId = (int)EElementBulletId.Overloaded,
                                        ShootPos = CreatureHelper.GetCenterPos(localTransform.Position, center, localTransform.Scale),
                                        ParentCreature = attacker,
                                        AtkValue = new AtkValue(bulletAtk.Atk, bulletAtk.Crit, bulletAtk.CritDamage, bulletProperties.Team),
                                        ImmediatelyBomb = true,
                                        DisableSplit = true,
                                    };
                                    ecb.AppendToBuffer(global.Entity, bulletBuffer);
                                    break;
                                }
                                case EElementReaction.Charged:
                                {
                                    //(通过buff伤害)
                                    //感电: 加buff, 持续雷元素伤害
                                    BuffHelper.AppendCreateBuffData(global.Entity, (int)EBuffId.Charged, entity, attacker, 0, EBuffFrom.Element, (int)attackerElementId, ecb);
                                    break;
                                }
                                case EElementReaction.Freeze:
                                {
                                    var freezeContTime = 3f;
                                    BuffHelper.AppendCreateBuffData(global.Entity, (int)EBuffId.ElementFreeze, entity, attacker, freezeContTime, EBuffFrom.Element, (int)attackerElementId, ecb);

                                    //进入冻结状态
                                    creatureElement.ValueRW.Value = EElement.Freeze;
                                    creatureElement.ValueRW.Timer = 0;
                                    creatureElement.ValueRW.ContTime = freezeContTime;
                                    break;
                                }
                                case EElementReaction.Superconduct:
                                {
                                    //(通过子弹伤害)
                                    //超导: 范围冰伤害, 降低敌人物理抗性
                                    var bulletBuffer = new BulletCreateBuffer
                                    {
                                        From = EBulletFrom.ElementReaction,
                                        FromId = (int)reaction,
                                        BulletId = (int)EElementBulletId.Superconduct,
                                        ShootPos = CreatureHelper.GetCenterPos(localTransform.Position, center, localTransform.Scale),
                                        ParentCreature = attacker,
                                        AtkValue = new AtkValue(bulletAtk.Atk, bulletAtk.Crit, bulletAtk.CritDamage, bulletProperties.Team),
                                        ImmediatelyBomb = true,
                                        DisableSplit = true,
                                    };
                                    ecb.AppendToBuffer(global.Entity, bulletBuffer);

                                    break;
                                }
                                case EElementReaction.Shatter:
                                {
                                    //(当前伤害)
                                    //碎冰: 播放碎冰特效
                                    ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                                    {
                                        ResourceId = (int)EEffectId.Shatter,
                                        Pos = localTransform.Position,
                                        DelayDestroy = 2f,
                                    });

                                    //移除buff
                                    BuffHelper.RemoveBuffById((int)EBuffId.ElementFreeze, entity, _buffEntitiesLookup, _buffTagLookup, ecb);
                                    break;
                                }
                            }
                        }


                        //先检查fatalHit
                        var buffResult = BuffHelper.GetBuffFactorAndValue(attacker, _summonLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, EBuffType.FatalHit, checkFloat: beforeHpPercent);
                        if (global.Random.ValueRW.Value.NextFloat(0f, 1f) <= buffResult.TempData)
                        {
                            damageNumberType = EDamageNumber.FatalHit;
                            damage *= buffResult.AddFactor;
                            canCrit = false;
                        }

                        if (canCrit)
                        {
                            damage = ProcessCrit(damage, beforeHpPercent, localTransform.Position, buffer, bulletConfig,
                                bulletAtk, attacker, global, cache, _summonLookup, _propsLookup,
                                _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, _skillEntitiesLookup, _skillTagLookup, _shieldLookup, _creatureMoveLookup, _attrLookup, _attrModifyLookup, ecb, out var critType);

                            if (critType != EDamageNumber.None)
                            {
                                damageNumberType = critType;
                            }
                        }
                    }

                    //伤害四舍五入
                    var afterHp = hpInfo.ValueRO.CurHp;

                    //处理护盾
                    var shieldHurtValue = ProcessShield(entity, _shieldLookup, damage, ecb);
                    var bHitShield = shieldHurtValue > 0;
                    if (!bHitShield)
                    {
                        //处理斩杀
                        if (BuffHelper.CheckExecutionBuff(attacker, damage, global, entity,
                                _summonLookup, _hpLookup, _creatureTagLookup, _buffEntitiesLookup, _buffTagLookup,
                                _buffCommonLookup, _attrLookup, _attrModifyLookup, out var effectId))
                        {
                            damage = hpInfo.ValueRO.CurHp;

                            //斩杀特效
                            if (effectId > 0)
                            {
                                ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                                {
                                    ResourceId = effectId,
                                    Pos = localTransform.Position,
                                    Scale = localTransform.Scale,
                                });
                            }
                        }

                        afterHp = hpInfo.ValueRO.CurHp - damage;
                        if (afterHp < 0)
                        {
                            afterHp = 0;

                            //岩元素, 掉落护盾石
                            if (attackerElementId == EElement.Stone)
                            {
                                if (creatureElement.ValueRO.Value is EElement.Fire or EElement.Ice or EElement.Lighting or EElement.Water)
                                {
                                    reaction = EElementReaction.Crystallize;
                                }
                            }
                        }

                        hpInfo.ValueRW.CurHp = afterHp;
                        var afterHpPercent = afterHp / hitMaxHp;

                        //技能触发器：血量变化
                        SkillHelper.DoSkillTrigger(entity, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnHpChanged)
                        {
                            FloatValue1 = beforeHpPercent,
                            FloatValue2 = afterHpPercent,
                        }, ecb);

                        //查看被击者的输血buff，尝试输血给buff释放者
                        BuffHelper.ProcessTransfusionBuff(entity, damage, _buffEntitiesLookup, _buffTagLookup, _buffTransfusion, _summonLookup, ecb);

                        //飘血数字
                        if (global.ShowDamageNumber)
                        {
                            ecb.SetComponentEnabled<DamageNumberBuffer>(entity, true);
                            if (reaction == EElementReaction.None)
                            {
                                if (bulletProperties.From == EBulletFrom.ElementReaction)
                                {
                                    reaction = (EElementReaction)bulletProperties.FromId;    
                                }
                                else if (bulletProperties.From == EBulletFrom.Buff)
                                {
                                    if (bulletProperties.FromId == (int)EBuffId.Charged)
                                    {
                                        reaction = EElementReaction.Charged;
                                    }
                                }
                            }
                            ecb.AppendToBuffer(entity, new DamageNumberBuffer
                            {
                                Element = attackerElementId,
                                Type = damageNumberType,
                                Value = damage,
                                Against = againstType,
                                Reaction = reaction,
                            });
                        }
                    }
                    else
                    {
                        //护盾蓝色字
                        if (global.ShowDamageNumber)
                        {
                            ecb.SetComponentEnabled<DamageNumberBuffer>(entity, true);
                            ecb.AppendToBuffer(entity, new DamageNumberBuffer
                            {
                                Element = attackerElementId,
                                Type = EDamageNumber.Shield,
                                Value = shieldHurtValue
                            });
                        }
                    }

                    //dps统计
                    {
                        var findEntity = entity;
                        var servantId = 0;
                        var servantParent = Entity.Null;
                        while (_summonLookup.TryGetComponent(findEntity, out var attackeSummon))
                        {
                            if (attackeSummon.SelfType == ECreatureType.Servant)
                            {
                                servantId = attackeSummon.SelfConfigId;
                                servantParent = attackeSummon.SummonParent;
                                break;
                            }

                            if (attackeSummon.SelfType == ECreatureType.Player)
                            {
                                //local player
                                servantId = 0;
                                servantParent = findEntity;
                                break;
                            }

                            if (!_summonLookup.HasComponent(attackeSummon.SummonParent))
                            {
                                break;
                            }
                            findEntity = attackeSummon.SummonParent;    
                        } 
                            
                        if (_localPlayerLookup.HasComponent(servantParent))
                        {
                            ecb.AppendToBuffer(servantParent, new DpsAppendBuffer { Damage = damage, ServantId = servantId, });
                            ecb.SetComponentEnabled<DpsAppendBuffer>(servantParent, true);
                        }
                    }

                    var hitEffectId = bulletConfig.HitEffectId;
                    //hit流程
                    {
                        var hitSoundId = buffer.FromExplosion ? 0 : bulletConfig.HitSound;

                        //hit tag
                        if (bHitShield == false)
                        {
                            //被击特效
                            if (afterHp > 0 && hitEffectId > 0)
                            {
                                ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                                {
                                    ResourceId = hitEffectId,
                                    Parent = entity,
                                    DelayDestroy = 2f,
                                });
                            }

                            //被击音效
                            if (hitSoundId > 0)
                            {
                                ecb.AppendToBuffer(global.Entity, new PlaySoundBuffer { SoundId = hitSoundId });
                            }

                            ecb.SetComponent(entity, new EnterHitTag { Attacker = attacker, });
                            ecb.SetComponentEnabled<EnterHitTag>(entity, true);
                        }


                        //怪物加击退
                        if (buffer.RepelForce > 0)
                        {
                            var isPhysics = buffer.RepelTime <= 0;
                            ecb.SetComponent(entity, new CreatureRepelStart
                            {
                                IsPhysics = isPhysics,
                                Forward = buffer.RepelForward,
                                Force = buffer.RepelForce,
                                RepelMaxScale = buffer.RepelMaxScale,
                                RepelTime = buffer.RepelTime,
                                RepelCd = buffer.RepelCd,
                            });
                            ecb.SetComponentEnabled<CreatureRepelStart>(entity, true);
                        }

                        //SkillTrigger onHit
                        SkillHelper.DoSkillTrigger(entity, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnHit)
                        {
                            From = bulletProperties.From,
                            FromParam = bulletProperties.FromId,
                            Entity = attacker,
                        }, ecb);

                        //召唤物命中敌人技能Trigger
                        if (_summonLookup.TryGetComponent(attacker, out var attackerCreature) && _summonLookup.HasComponent(attackerCreature.SummonParent))
                        {
                            SkillHelper.DoSkillTrigger(attackerCreature.SummonParent, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.SummonHitEnemy)
                            {
                                IntValue1 = attackerCreature.SelfConfigId,
                                Entity = entity
                            }, ecb);
                        }
                    }

                    //死亡流程
                    if (afterHp <= 0)
                    {
                        //被击特效
                        if (hitEffectId > 0)
                        {
                            ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                            {
                                ResourceId = hitEffectId,
                                Pos = localTransform.Position,
                                DelayDestroy = 2f,
                            });
                        }

                        //是否有免疫死亡的buff，如果有，则先不执行死亡流程
                        var immuneDie = BuffHelper.GetHasBuff(entity, _summonLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, EBuffType.ImmuneDie);
                        if (immuneDie == false)
                        {
                            //SkillTrigger 击杀
                            {
                                SkillHelper.DoSkillTrigger(attacker, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.KillEnemy)
                                {
                                    Entity = entity,
                                    From = bulletProperties.From,
                                    FromParam = bulletProperties.FromId,
                                    IntValue1 = (int)creatureTag.Type,
                                }, ecb);

                                //如果master是召唤物，触发master的trigger
                                if (_summonLookup.TryGetComponent(attacker, out var masterCreature) && _summonLookup.HasComponent(masterCreature.SummonParent))
                                {
                                    SkillHelper.DoSkillTrigger(masterCreature.SummonParent, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.KillEnemy)
                                    {
                                        Entity = entity,
                                        From = bulletProperties.From,
                                        FromParam = bulletProperties.FromId,
                                        IntValue1 = (int)creatureTag.Type,
                                        BoolValue1 = true, //召唤物触发给master的
                                    }, ecb);
                                }
                            }

                            if (reaction == EElementReaction.Crystallize)
                            {
                                ecb.AppendToBuffer(global.Entity, new DropItemCreateBuffer
                                {
                                    Pos = localTransform.Position,
                                    DropItemId = 101,
                                    Count = 1,
                                    RandomRange = 2f,
                                });
                            }

                            //Kill 相关处理
                            ecb.AppendToBuffer(attacker, new CreatureDataProcess
                            {
                                Type = ECreatureDataProcess.KillEnemy,
                                EntityValue = entity,
                            });
                            ecb.SetComponentEnabled<CreatureDataProcess>(attacker, true);

                            ecb.SetComponent(entity, new EnterDieTag { HitForward = buffer.RepelForward });
                            ecb.SetComponentEnabled<EnterDieTag>(entity, true);

                            //死亡就不继续处理了
                            damageBuffers.Clear();
                            break;
                        }
                    }
                }


                if (damageBuffers.Length <= 0)
                {
                    ecb.SetComponentEnabled<DamageBuffer>(entity, false);
                }
            }


            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        private static float ProcessShield(Entity hitEntity, ComponentLookup<ShieldProperties> shieldLookup, float damageResult, EntityCommandBuffer ecb)
        {
            var shieldHurt = 0f;
            if (shieldLookup.TryGetComponent(hitEntity, out var shield) && shieldLookup.IsComponentEnabled(hitEntity))
            {
                if (shield.Value > 0)
                {
                    var afterShieldValue = shield.Value - damageResult;
                    
                    //护盾破碎了
                    if (afterShieldValue <= 0)
                    {
                        shieldHurt = shield.Value;
                        afterShieldValue = 0;

                        //remove shield
                        ecb.AppendToBuffer(hitEntity, new CreatureDataProcess
                        {
                            Type = ECreatureDataProcess.RemoveShield,
                            BoolValue = false,
                        });
                        ecb.SetComponentEnabled<CreatureDataProcess>(hitEntity, true);
                    }
                    else
                    {
                        shieldHurt = damageResult;
                    }

                    shieldLookup.GetRefRW(hitEntity).ValueRW.Value = afterShieldValue;
                }
            }
            
            return shieldHurt;
        }

        private static bool ProcessDodge(Entity hitEntity, ComponentLookup<StatusSummon> summonLookup, GlobalAspect global,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup, 
            ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup, EntityCommandBuffer ecb)
        {
            //buff Dodge 
            var dodgeProb = BuffHelper.GetBuffAddFactor(hitEntity, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.Dodge);
            
            //attr
            dodgeProb += AttrHelper.GetAttr(hitEntity, EAttr.Dodge, attrLookup, attrModifyLookup, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            
            //max dodge
            var maxDodge = AttrHelper.GetMax(EAttr.Dodge, hitEntity, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            if (dodgeProb > maxDodge)
            {
                dodgeProb = maxDodge;
            }
            
            if (dodgeProb > 0 && global.Random.ValueRW.Value.NextFloat(0f, 1f) <= dodgeProb)
            {
                //弹出闪避飘字
                ecb.SetComponentEnabled<DamageNumberBuffer>(hitEntity, true);
                ecb.AppendToBuffer(hitEntity, new DamageNumberBuffer
                {
                    Type = EDamageNumber.Miss,
                });
                return true;
            }

            return false;
        }

        public static EElementReaction TryElementReaction(CacheAspect cache, GlobalAspect global, EElement attackerElement, RefRW<BindingElement> creatureElement, Entity entity, LocalTransform localTransform, EntityCommandBuffer ecb, out float damageFactor)
        {
            damageFactor = 1f;

            if (creatureElement.ValueRO.Value == EElement.None)
            {
                //尝试绑定元素
                if (cache.GetElementConfig(attackerElement, out var elementConfig))
                {
                    if (elementConfig.bCont)
                    {
                        creatureElement.ValueRW.Value = attackerElement;
                        creatureElement.ValueRW.ContTime = elementConfig.contTime;
                        creatureElement.ValueRW.Timer = 0;

                        ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                        {
                            Loop = true,
                            From = EEffectFrom.Element,
                            FromId = (int)attackerElement,
                            ResourceId = elementConfig.buffRes,
                            Parent = entity,
                        });
                    }
                }
            }
            else
            {
                //相同元素重置时间
                if (creatureElement.ValueRO.Value == attackerElement)
                {
                    creatureElement.ValueRW.Timer = 0;
                }
                else
                {
                    //蒸发	Vaporize	火+水	提升伤害（火触发为 ×2，水触发为 ×1.5）
                    //融化	Melt	火+冰	提升伤害（火触发为 ×2，冰触发为 ×1.5）
                    //过载	Overloaded	火+雷	范围火元素爆炸伤害（推怪）
                    //感电	Electro-Charged	水 + 雷	持续雷元素伤害
                    //冻结	Freeze	水+冰	敌人定身，重击可碎冰
                    //超导	Superconduct	冰 + 雷	范围冰伤 + 降低敌人物理抗性（持续12秒）
                    //碎冰	Shatter	物理+冻结	双倍伤害
                    //结晶	Crystallize	岩石击杀附带火+雷+冰+水的敌人,掉落结晶石,拾取后增加护盾	
                    switch (creatureElement.ValueRO.Value)
                    {
                        case EElement.Water:
                        {
                            if (attackerElement == EElement.Fire)
                            {
                                //蒸发, 2倍伤害
                                damageFactor = 2f;
                                return EElementReaction.Vaporize;
                            }

                            if (attackerElement == EElement.Ice)
                            {
                                //冻结
                                return EElementReaction.Freeze;
                            }

                            if (attackerElement == EElement.Lighting)
                            {
                                //感电 
                                return EElementReaction.Charged;
                            }

                            break;
                        }
                        case EElement.Fire:
                        {
                            if (attackerElement == EElement.Water)
                            {
                                //蒸发(水触发), 1.5倍伤害
                                damageFactor = 1.5f;
                                return EElementReaction.Vaporize;
                            }

                            if (attackerElement == EElement.Ice)
                            {
                                //融化(冰触发), 1.5倍伤害
                                damageFactor = 1.5f;
                                return EElementReaction.Melt;
                            }

                            if (attackerElement == EElement.Lighting)
                            {
                                //过载
                                return EElementReaction.Overloaded;
                            }

                            break;
                        }
                        case EElement.Ice:
                        {
                            if (attackerElement == EElement.Fire)
                            {
                                //融化(火触发), 2倍伤害
                                damageFactor = 2f;
                                return EElementReaction.Melt;
                            }

                            if (attackerElement == EElement.Water)
                            {
                                //冻结
                                return EElementReaction.Freeze;
                            }

                            if (attackerElement == EElement.Lighting)
                            {
                                //超导
                                return EElementReaction.Superconduct;
                            }

                            break;
                        }
                        case EElement.Lighting:
                        {
                            if (attackerElement == EElement.Fire)
                            {
                                //过载
                                return EElementReaction.Overloaded;
                            }

                            if (attackerElement == EElement.Water)
                            {
                                //感电
                                return EElementReaction.Charged;
                            }

                            if (attackerElement == EElement.Ice)
                            {
                                //超导
                                return EElementReaction.Superconduct;
                            }

                            break;
                        }
                        case EElement.Freeze:
                        {
                            if (attackerElement == EElement.None)
                            {
                                //碎冰
                                damageFactor = 2f;
                                return EElementReaction.Shatter;
                            }

                            break;
                        }
                    }
                }
            }

            return EElementReaction.None;
        }

        private static float ProcessCrit(float sourceDamage, float hpPercent, float3 hitPos, DamageBuffer buffer, 
            BulletConfig bulletConfig, BulletAtkValue bulletAtk, Entity attacker,
            GlobalAspect global, CacheAspect cache, ComponentLookup<StatusSummon> summonLookup, ComponentLookup<CreatureProps> propsLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup, ComponentLookup<ShieldProperties> shieldLookup, ComponentLookup<StatusMove> moveLookup,
            ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup,
            EntityCommandBuffer ecb, out EDamageNumber critType)
        {
            critType = EDamageNumber.None;

            var bulletId = bulletConfig.Id;
            var bulletClassId = bulletConfig.ClassId;

            //base
            var critPercent = bulletAtk.Crit;

            //buff加的暴击
            critPercent += BuffHelper.GetBuffAddFactor(attacker, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.Crit, bulletId, bulletClassId, false, hpPercent);

            //根据移动速度计算暴击 
            {
                var addSpeedFactor = CreatureHelper.GetMoveSpeedFactor(attacker, summonLookup, propsLookup, shieldLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, attrLookup, attrModifyLookup);
                critPercent += BuffHelper.GetBuffFactorByTempPercent(EBuffType.CritByMoveSpeed, addSpeedFactor, attacker, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            }

            //attr
            {
                var attrCrit = AttrHelper.GetAttr(attacker, EAttr.Crit, attrLookup, attrModifyLookup, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                critPercent += attrCrit;
            }
            
            //calc max
            var maxCrit = AttrHelper.GetMax(EAttr.Crit, attacker, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            if (critPercent > maxCrit)
            {
                critPercent = maxCrit;
            }
            
            //计算暴击几率
            var isCrit = global.Random.ValueRW.Value.NextFloat(0f, 1f) <= critPercent;
            if (isCrit)
            {
                //暴击的SkillTrigger
                {
                    SkillHelper.DoSkillTrigger(attacker, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnCrit)
                    {
                        IntValue1 = bulletId,
                        IntValue2 = bulletClassId,
                        Pos = hitPos
                    }, ecb);

                    //如果master是召唤物，触发master的trigger
                    if (summonLookup.TryGetComponent(attacker, out var masterCreature) && summonLookup.HasComponent(masterCreature.SummonParent))
                    {
                        SkillHelper.DoSkillTrigger(masterCreature.SummonParent, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnCrit)
                        {
                            IntValue1 = bulletId,
                            IntValue2 = bulletClassId,
                            Pos = hitPos,
                            BoolValue1 = true, //召唤物触发给master的
                        }, ecb);
                    }
                }

                //初始暴击伤害
                var critDamagePercent = bulletAtk.CritDamage;

                //buff加的暴击伤害
                critDamagePercent += BuffHelper.GetBuffAddFactor(attacker, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.CritDamage, bulletId, bulletClassId, false, hpPercent);
                
                //attr
                critDamagePercent += AttrHelper.GetAttr(attacker, EAttr.CritDamage, attrLookup, attrModifyLookup, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
                
                //判断是否产生致命一击
                //如果确定产生暴击率，处理致命一击
                var buffResult = BuffHelper.GetBuffFactorAndValue(attacker, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.SuperCrit);
                var isSuperCrit = global.Random.ValueRW.Value.NextFloat(0f, 1f) <= buffResult.TempData;
                if (isSuperCrit)
                {
                    critDamagePercent += buffResult.AddFactor;
                    critType = EDamageNumber.SuperCrit;
                }
                else
                {
                    critType = EDamageNumber.Crit;
                }
                
                //min value 1
                var minCritDamage = AttrHelper.GetMin(EAttr.CritDamage);
                if (critDamagePercent < minCritDamage)
                {
                    critDamagePercent = minCritDamage;
                }
          
                sourceDamage *= critDamagePercent;
            }
            return sourceDamage;
        }
    }
}