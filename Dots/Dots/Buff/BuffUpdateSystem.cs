using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BuffSystemGroup))]
    public partial struct BuffUpdateSystem : ISystem
    {
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<BuffTimeProperty> _buffTimeLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<CreatureProps> _propsLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;

        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<BuffTransfusion> _buffTransfusion;
        [ReadOnly] private ComponentLookup<BuffFreeze> _buffFreezeLookup;
        [ReadOnly] private BufferLookup<BindingBullet> _bindingBulletLookup;
        [ReadOnly] private ComponentLookup<BulletProperties> _bulletLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _buffTimeLookup = state.GetComponentLookup<BuffTimeProperty>();
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _propsLookup = state.GetComponentLookup<CreatureProps>(true);
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _summonEntitiesLookup = state.GetBufferLookup<SummonEntities>(true);
            _bindingBulletLookup =  state.GetBufferLookup<BindingBullet>(true);
            _bulletLookup = state.GetComponentLookup<BulletProperties>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>();
            _buffTransfusion = state.GetComponentLookup<BuffTransfusion>();
            _buffFreezeLookup = state.GetComponentLookup<BuffFreeze>();
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

            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffTimeLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _propsLookup.Update(ref state);
            _monsterLookup.Update(ref state);
            _summonEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _buffTransfusion.Update(ref state);
            _buffFreezeLookup.Update(ref state);
            _bindingBulletLookup.Update(ref state);
            _bulletLookup.Update(ref state);
            
            //buff不能并行
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            foreach (var (updateBuffer, properties, entity) in SystemAPI.Query<DynamicBuffer<BuffUpdateBuffer>, RefRW<BuffTag>>().WithEntityAccess())
            {
                var buffId = properties.ValueRO.BuffId;
                if (!cache.GetBuffConfig(buffId, out var config))
                {
                    Debug.LogError($"找不到buffConfig, buffId:{buffId}");
                    continue;
                }

                var masterCreature = properties.ValueRO.Master;
                if (!_propsLookup.HasComponent(masterCreature))
                {
                    continue;
                }

                for (var idx = updateBuffer.Length - 1; idx >= 0; idx--)
                {
                    var tag = updateBuffer[idx];
                    updateBuffer.RemoveAt(idx);

                    //更新BuffData
                    switch (tag.Value)
                    {
                        case EBuffUpdate.Remove:
                        {
                            //remove effect 
                            if (properties.ValueRO.HasEffect)
                            {
                                ecb.AppendToBuffer(global.Entity, new DestroyEffectByFrom
                                {
                                    Parent = masterCreature,
                                    From = EEffectFrom.Buff,
                                    FromId = buffId
                                });
                                properties.ValueRW.HasEffect = false;
                            }

                            //不是Overlap的话新建特效
                            if (config.RemoveEffectId > 0)
                            {
                                if (_transformLookup.TryGetComponent(masterCreature, out var masterTrans))
                                {
                                    ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                                    {
                                        ResourceId = config.RemoveEffectId,
                                        Pos = masterTrans.Position,
                                        Scale = masterTrans.Scale,
                                    });
                                }
                            }
                            
                            //destroy bind bullet
                            BulletHelper.ClearBuffBindBullet(entity, _buffTagLookup, _bindingBulletLookup, _bulletLookup, ecb);

                            ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                            break;
                        }
                        case EBuffUpdate.Add:
                        case EBuffUpdate.Overlap:
                        {
                            //SkillTrigger
                            SkillHelper.DoSkillTrigger(properties.ValueRO.Attacker, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.AddBuffToEnemy)
                            {
                                IntValue1 = (int)config.BuffType,
                                Entity = masterCreature,
                            }, ecb);

                            if (tag.Value == EBuffUpdate.Overlap)
                            {
                                //超过最大层数了
                                if (_buffTimeLookup.HasComponent(entity))
                                {
                                    _buffTimeLookup.GetRefRW(entity).ValueRW.StartTime = global.Time;
                                }

                                if (config.LayerLimit > 0 && properties.ValueRO.Layer >= config.LayerLimit)
                                {
                                    continue;
                                }

                                properties.ValueRW.Layer = properties.ValueRO.Layer + 1;
                            }
                            
                            //bind bullet
                            var bindBullet = config.BulletId;
                            if (bindBullet > 0)
                            {
                                BulletHelper.BindBuffBullet(global, entity, _buffTagLookup, _propsLookup, _bindingBulletLookup,
                                    _transformLookup, _bulletLookup, bindBullet, properties.ValueRO.Layer, ecb);
                            }  
                            break;
                        }
                    }

                    switch (config.BuffType)
                    {
                        case EBuffType.Freeze:
                        case EBuffType.Stunt:
                        {
                            if (tag.Value == EBuffUpdate.Add)
                            {

                                var freezeCount = BuffHelper.CheckHasFreeze(masterCreature, _buffEntitiesLookup, _buffFreezeLookup);
                                if (freezeCount <= 0)
                                {
                                    ecb.SetComponent(masterCreature, new EnterFreezeTag { });
                                    ecb.SetComponentEnabled<EnterFreezeTag>(masterCreature, true);
                                }

                                _buffFreezeLookup.GetRefRW(entity).ValueRW.Enable = true;
                            }

                            if (tag.Value == EBuffUpdate.Remove)
                            {
                                _buffFreezeLookup.GetRefRW(entity).ValueRW.Enable = false;

                                var freezeCount = BuffHelper.CheckHasFreeze(masterCreature, _buffEntitiesLookup, _buffFreezeLookup);

                                //没buff了，给宿主解冻
                                if (freezeCount <= 0)
                                {
                                    ecb.SetComponentEnabled<RemoveFreezeTag>(masterCreature, true);
                                }
                            }

                            break;
                        }
                        
                        //输血给攻击者
                        case EBuffType.Transfusion:
                        {
                            var factor = config.Param1;

                            if (tag.Value == EBuffUpdate.Add)
                            {
                                ecb.SetComponent(entity, new BuffTransfusion
                                {
                                    Factor = factor,
                                    Attacker = properties.ValueRO.Attacker,
                                });
                                ecb.SetComponentEnabled<BuffTransfusion>(entity, true);
                            }
                            else if (tag.Value == EBuffUpdate.Overlap)
                            {
                                if (_buffTransfusion.IsComponentEnabled(entity))
                                {
                                    _buffTransfusion.GetRefRW(entity).ValueRW.Attacker = properties.ValueRO.Attacker;
                                    _buffTransfusion.GetRefRW(entity).ValueRW.Factor += factor;
                                }
                            }
                            else if (tag.Value == EBuffUpdate.Remove)
                            {
                                ecb.SetComponentEnabled<BuffTransfusion>(entity, false);
                            }

                            break;
                        }
                        //增加子弹行为
                        case EBuffType.AddBulletBehaviour:
                        {
                            var bulletId = config.Param1.ToInt();
                            var bulletClassId = config.Param2.ToInt();
                            var behaviourId = config.Param3.ToInt();

                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                                AttachInt = behaviourId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.ChangeBulletBehaviour:
                        {
                            var bulletId = config.Param1.ToInt();
                            var bulletClassId = config.Param2.ToInt();
                            var oldBehaviourId = config.Param3.ToInt();
                            var newBehaviourId = config.Param4.ToInt();

                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                                CheckInt3 = oldBehaviourId,
                                AttachInt = newBehaviourId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //绑定技能, 替换技能
                        case EBuffType.BindSkill:
                        {
                            var newSkillId = config.Param1.ToInt();
                            var lookingSkillClass = config.Param2.ToInt();
                            var lookingSkillId = config.Param3.ToInt();

                            var data = new BuffCommonData
                            {
                                CheckInt1 = lookingSkillClass,
                                CheckInt2 = lookingSkillId,
                                AttachInt = newSkillId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        case EBuffType.ReplaceBullet:
                        case EBuffType.BulletChangeCollisionType:
                        {
                            var newValue = config.Param1.ToInt();
                            var bulletId = config.Param2.ToInt();
                            var bulletClass = config.Param3.ToInt();

                            if (bulletId <= 0 && bulletClass <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClass,
                                AttachInt = newValue,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //子弹伤害
                        case EBuffType.BulletDamage: 
                        {
                            var factor = config.Param1;
                            var bulletId = config.Param2.ToInt();
                            var value = config.Param3.ToInt();
                            var classId = config.Param4.ToInt();

                            if (bulletId <= 0 && classId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddValue = value,
                                AddFactor = factor,
                                CheckInt1 = bulletId,
                                CheckInt2 = classId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //子弹分裂
                        case EBuffType.BulletSplit:
                        case EBuffType.BulletSplitHoriz:
                        {
                            var value = (int)config.Param1;
                            var angle = config.Param2;
                            var factor = config.Param3;
                            var bulletId = config.Param4.ToInt();
                            var bulletClassId = config.Param5.ToInt();

                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddValue = value,
                                AddFactor = factor,
                                TempData = angle,
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //敌人身上存在某类型buff时候加伤害
                        case EBuffType.DamageByEnemyBuff:
                        {
                            var factor = config.Param1;
                            var enemyBuffType = config.Param2.ToInt();
                            var bulletId = config.Param4.ToInt();
                            var bulletClassId = config.Param5.ToInt();

                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckInt1 = enemyBuffType,
                                CheckInt2 = bulletId,
                                CheckInt3 = bulletClassId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        case EBuffType.DamageBySelfHp:
                        case EBuffType.DamageByEnemyMoveSpeed:
                        case EBuffType.DamageBySelfMoveSpeed:
                        case EBuffType.CritByMoveSpeed:
                        {
                            var perPercent = config.Param1;
                            var addFactor = config.Param2;
                            var maxFactor = config.Param3;

                            var data = new BuffCommonData
                            {
                                AddFactor = addFactor,
                                MaxLimit = maxFactor,
                                TempData = perPercent,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //附加buff时候的伤害
                        case EBuffType.AttackerBuffDamage:
                        {
                            var factor = config.Param1;
                            var buffType = config.Param2.ToInt();
                            var value = config.Param3;

                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                AddValue = value,
                                CheckInt1 = buffType,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        //给子弹添加Buff
                        case EBuffType.AppendBuffToBullet:
                        {
                            var bulletId = (int)config.Param1;
                            var append1 = (int)config.Param2;
                            var append2 = (int)config.Param3;
                            var append3 = (int)config.Param4;
                            var append4 = (int)config.Param5;
                            var bulletClassId = (int)config.Param6;

                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            if (tag.Value == EBuffUpdate.Add)
                            {
                                var data = new BuffAppendRandomIds
                                {
                                    CheckInt1 = bulletId,
                                    CheckInt2 = bulletClassId,
                                    Append1 = append1,
                                    Append2 = append2,
                                    Append3 = append3,
                                    Append4 = append4,
                                };

                                ecb.SetComponent(entity, data);
                                ecb.SetComponentEnabled<BuffAppendRandomIds>(entity, true);
                            }
                            else if (tag.Value == EBuffUpdate.Overlap)
                            {
                            }
                            else if (tag.Value == EBuffUpdate.Remove)
                            {
                                ecb.SetComponentEnabled<BuffAppendRandomIds>(entity, false);
                            }

                            break;
                        }
                        case EBuffType.BulletChangeBanHit:
                        {
                            var banHit = config.Param1.ToInt();
                            var bulletId = config.Param2.ToInt();
                            var bulletClassId = config.Param3.ToInt();
                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                                AttachInt = banHit,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //子弹击退力度
                        case EBuffType.BulletHitForce:
                        {
                            var factor = config.Param1;
                            var value = config.Param2;
                            var bulletId = config.Param3.ToInt();
                            var bulletClassId = config.Param4.ToInt();
                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                AddValue = value,
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        case EBuffType.BulletDamageCount:
                        case EBuffType.BulletBounce:
                        {
                            var value = (int)config.Param1;
                            var bulletId = (int)config.Param2;
                            var bulletClassId = config.Param3.ToInt();
                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddValue = value,
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.BulletSpeed:
                        case EBuffType.BulletScale:
                        case EBuffType.BulletExplosionRange:
                        case EBuffType.BulletContTime:
                        case EBuffType.BulletDamageInterval:
                        {
                            var addFactor = config.Param1;
                            var bulletId = config.Param2.ToInt();
                            var bulletClassId = config.Param3.ToInt();

                            if (bulletId <= 0 && bulletClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error bulletId or ClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddFactor = addFactor,
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        //移动速度
                        case EBuffType.MoveSpeed:
                        {
                            var factor = config.Param1;
                            var needShield = (int)config.Param2 == 1;

                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckBool1 = needShield
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //附加buff时候的持续时间
                        case EBuffType.AttackerBuffTime:
                        {
                            var factor = config.Param1;
                            var buffType = (EBuffType)config.Param2;

                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckInt1 = (int)buffType
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //生物体缩放
                        case EBuffType.CreatureScale:
                        {
                            var factor = config.Param1;
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //增加或减少对应技能Trigger Interval的频率
                        case EBuffType.SkillTriggerInterval:
                        case EBuffType.SkillTriggerProb:
                        case EBuffType.SkillDelayTime:
                        {
                            var factor = config.Param1;
                            var skillClassId = config.Param2.ToInt();
                            var skillId = config.Param3.ToInt();
                            if (skillId <= 0 && skillClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error skillId or skillClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckInt1 = skillClassId,
                                CheckInt2 = skillId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.SkillTargetCount:
                        case EBuffType.SkillIntervalDualCastCount:
                        case EBuffType.SkillShootBulletCount:
                        case EBuffType.SkillCastCount:
                        case EBuffType.SkillRecursionCount:
                        {
                            var addValue = config.Param1.ToInt();
                            var skillClassId = config.Param2.ToInt();
                            var skillId = config.Param3.ToInt();
                            var addPercent = config.Param4;
                            if (skillId <= 0 && skillClassId <= 0)
                            {
                                Debug.LogError($"Add Buff error skillId or skillClassId, all zero, buffId:{config.Id}");
                            }
                            var data = new BuffCommonData
                            {
                                AddValue = addValue,
                                AddFactor = addPercent,
                                CheckInt1 = skillClassId,
                                CheckInt2 = skillId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.Damage:
                        {
                            var factor = config.Param1;
                            var monsterTypeLimit = config.Param2.ToInt();
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckInt1 = monsterTypeLimit,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        case EBuffType.ElementHurt:
                        {
                            var elementId = config.Param1.ToInt();
                            var factor = config.Param2;
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckInt1 = elementId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        case EBuffType.HurtFactor:
                        case EBuffType.ContTime:
                        case EBuffType.AtkRange:
                        case EBuffType.CureHpFactor:
                        case EBuffType.PercentDamageFactor:
                        case EBuffType.Dodge:
                        {
                            var factor = config.Param1;
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        case EBuffType.DebuffTime:
                        {
                            var factor = config.Param1;
                            var buffType = config.Param2.ToInt();
                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckInt1 = buffType
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        //无参数
                        case EBuffType.Invincible:
                        case EBuffType.ImmuneDie:
                        case EBuffType.DisableCure:
                        {
                            var data = new BuffCommonData();
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        //暴击
                        case EBuffType.Crit:
                        case EBuffType.CritDamage:
                        {
                            var factor = config.Param1;
                            var bulletId = config.Param2.ToInt();
                            var bulletClassId = config.Param3.ToInt();
                            var hpMin = config.Param4;
                            var hpMax = config.Param5;

                            var data = new BuffCommonData
                            {
                                AddFactor = factor,
                                CheckInt1 = bulletId,
                                CheckInt2 = bulletClassId,
                                CheckFloatMin = hpMin,
                                CheckFloatMax = hpMax,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        //致命一击
                        case EBuffType.SuperCrit:
                        {
                            var prob = config.Param1;
                            var extraDamageFactor = config.Param2;

                            var data = new BuffCommonData
                            {
                                TempData = prob,
                                AddFactor = extraDamageFactor,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }

                        case EBuffType.FatalHit:
                        {
                            var prob = config.Param1;
                            var multiple = config.Param2;
                            var hpMin = config.Param3;
                            var hpMax = config.Param4;

                            var data = new BuffCommonData
                            {
                                TempData = prob,
                                AddFactor = multiple,
                                CheckFloatMin = hpMin,
                                CheckFloatMax = hpMax,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.ExecutionEnemy:
                        {
                            var hpLess = config.Param1;
                            var prob = config.Param2;
                            var elite = config.Param3.ToInt();
                            var boss = config.Param4.ToInt();
                            var effectId = config.Param5.ToInt();
                            var data = new BuffCommonData
                            {
                                TempData = prob,
                                CheckFloatMax = hpLess,
                                CheckInt1 = elite,
                                CheckInt2 = boss,
                                AttachInt = effectId,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.ReflectBullet:
                        {
                            var prob = config.Param1;
                            var noChangeTeam = config.Param2.ToInt();
                            var data = new BuffCommonData
                            {
                                TempData = prob,
                                AttachInt = noChangeTeam,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.MissionBuff_MonsterCount:
                        {
                            var addFactor = config.Param1;
                            var monsterType = config.Param2.ToInt();

                            var data = new BuffCommonData
                            {
                                AddFactor = addFactor,
                                CheckInt1 = monsterType,
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.MissionBuff_EliteToBoss:
                        {
                            var data = new BuffCommonData();
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.MissionBuff_Wind:
                        {
                            var min = config.Param1;
                            var max = config.Param2;

                            var angle = global.Random.ValueRW.Value.NextFloat(0f, 360f);
                            var speed = global.Random.ValueRW.Value.NextFloat(min, max);
                            var data = new BuffCommonData
                            {
                                AddFactor = speed,
                                TempData = angle
                            };
                            TryProcessCommonData(_buffCommonLookup, entity, buffId, tag.Value, data, ecb, out _);
                            break;
                        }
                        case EBuffType.None:
                        default:
                        {
                            /*var errorType = (int)config.BuffType;
                            Debug.LogError($"配置了不生效的buffType:{errorType}");*/
                            break;
                        }
                    }
                }

                //update 完成后，禁用component，直到下次被激活
                if (updateBuffer.Length <= 0)
                {
                    ecb.SetComponentEnabled<BuffUpdateBuffer>(entity, false);
                }
            }


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static void TryProcessCommonData(ComponentLookup<BuffCommonData> commonDataLookup, Entity buffEntity, int buffId, EBuffUpdate updateType, BuffCommonData newData, EntityCommandBuffer ecb, out float factorChange)
        {
            factorChange = 0f;

            switch (updateType)
            {
                case EBuffUpdate.Add:
                {
                    ecb.SetComponent(buffEntity, newData);
                    ecb.SetComponentEnabled<BuffCommonData>(buffEntity, true);

                    factorChange += newData.AddFactor;
                    break;
                }
                case EBuffUpdate.Overlap:
                {
                    if (commonDataLookup.IsComponentEnabled(buffEntity))
                    {
                        commonDataLookup.GetRefRW(buffEntity).ValueRW.AddFactor += newData.AddFactor;
                        commonDataLookup.GetRefRW(buffEntity).ValueRW.AddValue += newData.AddValue;
                        commonDataLookup.GetRefRW(buffEntity).ValueRW.TempData = newData.TempData;
                        factorChange += newData.AddFactor;
                    }

                    break;
                }
                case EBuffUpdate.Remove:
                {
                    if (commonDataLookup.IsComponentEnabled(buffEntity) && commonDataLookup.TryGetComponent(buffEntity, out var data))
                    {
                        factorChange -= data.AddFactor;
                    }

                    ecb.SetComponentEnabled<BuffCommonData>(buffEntity, false);
                    break;
                }
            }
        }
    }
}