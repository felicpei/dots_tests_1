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
    public partial struct CreatureDataProcessSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalPlayerTag> _localPlayerLookup;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<MonsterMove> _monsterMoveLookup;
        [ReadOnly] private ComponentLookup<MonsterTarget> _monsterTarget;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<CreatureForward> _forwardLookup;
        [ReadOnly] private ComponentLookup<InDashingTag> _inDashLookup;
        [ReadOnly] private ComponentLookup<BulletTriggerData> _bulletTriggerLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_SetActive> _eventSetActive;
        [ReadOnly] private BufferLookup<BindingBullet> _bindBulletLookup;
        [ReadOnly] private ComponentLookup<BulletProperties> _bulletLookup;
        [ReadOnly] private ComponentLookup<ShieldProperties> _shieldLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<LocalPlayerTag>();
            
            _localPlayerLookup = state.GetComponentLookup<LocalPlayerTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _monsterMoveLookup = state.GetComponentLookup<MonsterMove>();
            _monsterTarget = state.GetComponentLookup<MonsterTarget>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _forwardLookup = state.GetComponentLookup<CreatureForward>(true);
            _inDashLookup = state.GetComponentLookup<InDashingTag>(true);
            _bulletTriggerLookup = state.GetComponentLookup<BulletTriggerData>(true);
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _eventSetActive = state.GetComponentLookup<HybridEvent_SetActive>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _bulletLookup = state.GetComponentLookup<BulletProperties>(true);
            _bindBulletLookup = state.GetBufferLookup<BindingBullet>();
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
 
            _localPlayerLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _monsterMoveLookup.Update(ref state);
            _monsterTarget.Update(ref state);
            _creatureLookup.Update(ref state);
            _bulletTriggerLookup.Update(ref state);
            _monsterLookup.Update(ref state);
            _forwardLookup.Update(ref state);
            _inDashLookup.Update(ref state);
            _eventSetActive.Update(ref state);
            _bindBulletLookup.Update(ref state);
            _bulletLookup.Update(ref state);
            _shieldLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var playerAspect = SystemAPI.GetAspect<PlayerAspect>(SystemAPI.GetSingletonEntity<LocalPlayerTag>());
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (buffers, creature, localTransform, summonEntities, entity) in
                     SystemAPI.Query<DynamicBuffer<CreatureDataProcess>, RefRW<CreatureProperties>, RefRW<LocalTransform>, DynamicBuffer<SummonEntities>>().WithEntityAccess())
            {
                //最大HP变化
                var killEnemyCount = 0;
                var killBossCount = 0;
                for (var i = buffers.Length - 1; i >= 0; i--)
                {
                    var buffer = buffers[i];
                    buffers.RemoveAt(i);

                    switch (buffer.Type)
                    {
                        case ECreatureDataProcess.AddAttr:
                        {
                            var attrType = (EAttr)buffer.IntValue1;
                            var addValue = buffer.FloatValue1;
                            
                            //only process local player
                            if (_localPlayerLookup.HasComponent(entity))
                            {
                                AttrHelper.AddAttr(playerAspect, attrType, addValue, ecb);
                            }
                            else
                            {
                                Debug.LogError("add attr error, only can to localplayer");
                            }
                            break;
                        }
                        case ECreatureDataProcess.SetActive:
                        {
                            var bActive = buffer.IntValue1 == 1;
                            CreatureHelper.ProcessMasterActive(bActive, entity, _eventSetActive, ecb);
                            break;
                        }
                        case ECreatureDataProcess.Cure:
                        {
                            if (!BuffHelper.GetHasBuff(entity, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, EBuffType.DisableCure))
                            {
                                ProcessCure(entity, global,  buffer, creature, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, _skillEntitiesLookup, _skillTagLookup, _attrLookup, _attrModifyLookup, ecb);
                            }
                            break;
                        }
                        case ECreatureDataProcess.AddCurHp:
                        {
                            var addValue = creature.ValueRO.FullHp * buffer.AddValue;
                            if (addValue > 0)
                            {
                                creature.ValueRW.CurHp += addValue;
                            }

                            break;
                        }
                        case ECreatureDataProcess.ResetHp:
                        {
                            creature.ValueRW.CurHp = AttrHelper.GetMaxHp(entity, _attrLookup, _attrModifyLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                            break;
                        }
                        case ECreatureDataProcess.AddScale:
                        {
                            ProcessScaleChange(buffer, creature);
                            break;
                        }
                        case ECreatureDataProcess.AddMaxHp:
                        {
                            ProcessAddMaxHp(buffer, creature);
                            break;
                        }
                        case ECreatureDataProcess.KillEnemy:
                        {
                            killEnemyCount++;

                            if (_creatureLookup.TryGetComponent(buffer.EntityValue, out var killMonster))
                            {
                                if (killMonster.Type == ECreatureType.Boss)
                                {
                                    killBossCount++;
                                }
                            }

                            break;
                        }
                        case ECreatureDataProcess.Teleport:
                        {
                            localTransform.ValueRW.Position = buffer.Float3Value;

                            //看向仇恨目标
                            if (_monsterTarget.TryGetComponent(entity, out var target))
                            {
                                if (creature.ValueRO.Type != ECreatureType.Player)
                                {
                                    CreatureHelper.UpdateFaceForward(entity, math.normalizesafe(target.Pos - localTransform.ValueRO.Position), _inDashLookup, ecb);
                                }
                            }

                            break;
                        }
                        case ECreatureDataProcess.AddShield:
                        {
                            ProcessAddShield(global, buffer, creature.ValueRO, localTransform.ValueRO, _shieldLookup, _skillEntitiesLookup, _skillTagLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, _attrLookup, _attrModifyLookup, entity, ecb);
                            break;
                        }
                        case ECreatureDataProcess.RemoveShield:
                        {
                            var isDisappear = buffer.BoolValue;
                            ProcessRemoveShield(global, entity, buffer, _shieldLookup, creature.ValueRO, _skillEntitiesLookup, _skillTagLookup, localTransform.ValueRO, ecb, isDisappear);
                            break;
                        }
                        case ECreatureDataProcess.SummonEntitiesDie:
                        {
                            for (var j = summonEntities.Length - 1; j >= 0; j--)
                            {
                                if (summonEntities[j].Value == buffer.EntityValue)
                                {
                                    summonEntities.RemoveAt(j);
                                }
                            }

                            break;
                        }

                        case ECreatureDataProcess.ResetSummonsAroundAngle:
                        {
                            ProcessResetSummonAngle(global, entity, buffer, summonEntities, _monsterMoveLookup, ecb);
                            break;
                        }

                        case ECreatureDataProcess.CastBulletAction:
                        {
                            foreach (var (bullet, bulletEntity) in SystemAPI.Query<BulletProperties>().WithEntityAccess())
                            {
                                if (_bulletTriggerLookup.HasComponent(bulletEntity))
                                {
                                    if (bullet.MasterCreature == entity && cache.GetBulletConfig(bullet.BulletId, out var bulletConfig))
                                    {
                                        if (bullet.BulletId == buffer.IntValue1 || bulletConfig.ClassId == buffer.IntValue2)
                                        {
                                            ecb.SetComponentEnabled<BulletTriggerBuffer>(bulletEntity, true);
                                            ecb.SetComponentEnabled<BulletBehaviourBuffer>(bulletEntity, true);

                                            BulletHelper.AddBehaviour(global, cache, (int)buffer.AddValue, bulletEntity, ecb);
                                        }
                                    }
                                }
                            }

                            break;
                        }

                        case ECreatureDataProcess.Turn:
                        {
                            if (creature.ValueRO.Type != ECreatureType.Player)
                            {
                                CreatureHelper.UpdateFaceForward(entity, buffer.Float3Value, _inDashLookup, ecb);
                            }

                            break;
                        }
                        case ECreatureDataProcess.UnbindBullet:
                        {
                            var bulletId = buffer.IntValue1;
                            if (_bindBulletLookup.TryGetBuffer(entity, out var bindingBullets))
                            {
                                for (var j = bindingBullets.Length - 1; j >= 0; j--)
                                {
                                    var bulletEntity = bindingBullets[j].Value;
                                    if (_bulletLookup.TryGetComponent(bindingBullets[j].Value, out var bulletProperties))
                                    {
                                        if (bulletProperties.BulletId == bulletId)
                                        {
                                            ecb.SetComponentEnabled<BulletDestroyTag>(bulletEntity, true);
                                            bindingBullets.RemoveAt(j);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                        case ECreatureDataProcess.SmallMonsterToElite:
                        {
                            if (_localPlayerLookup.HasComponent(entity))
                            {
                                foreach (var (creatureProperties, monster, monsterTrans, monsterEntity) in
                                         SystemAPI.Query<CreatureProperties, MonsterProperties, LocalTransform>().WithEntityAccess())
                                {
                                    if (creatureProperties.Type != ECreatureType.Small)
                                    {
                                        continue;
                                    }

                                    if (!_deadLookup.HasComponent(monsterEntity) || _deadLookup.IsComponentEnabled(monsterEntity))
                                    {
                                        continue;
                                    }

                                    if (cache.GetMonsterConfig(monster.Id, out var monsterConfig))
                                    {
                                        if (monsterConfig.EliteId > 0)
                                        {
                                            //召唤一个精英怪
                                            ecb.AppendToBuffer(global.Entity, new MonsterCreateBuffer
                                            {
                                                BornPos = monsterTrans.Position,
                                                MonsterId = monsterConfig.EliteId,
                                                IsElite = true,
                                                TeamId = creatureProperties.AtkValue.Team,
                                                HpPercent = 1f,
                                            });

                                            //让小怪死亡
                                            ecb.SetComponent(monsterEntity, new MonsterDelayDestroy { DelayTime = 0.1f });
                                            ecb.SetComponentEnabled<MonsterDelayDestroy>(monsterEntity, true);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("SmallMonsterToElite error, only can process in localPlayer");
                            }
                            break;
                        }
                    }
                }

                //击杀数量累计
                if (killEnemyCount > 0 || killBossCount > 0)
                {
                    ProcessKillEnemy(global, entity, killEnemyCount, killBossCount, _localPlayerLookup, _creatureLookup, ecb);
                }

                if (buffers.Length <= 0)
                {
                    ecb.SetComponentEnabled<CreatureDataProcess>(entity, false);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        private static void ProcessScaleChange(CreatureDataProcess buffer, RefRW<CreatureProperties> creature)
        {
            var scale = creature.ValueRO.OriginScale;
            if (buffer.AddValue != 0)
            {
                scale += buffer.AddValue;
            }

            if (buffer.AddPercent != 0)
            {
                scale = BuffHelper.CalcFactor(scale, buffer.AddPercent);
            }

            creature.ValueRW.OriginScale = scale;
        }

        private static void ProcessAddMaxHp(CreatureDataProcess buffer, RefRW<CreatureProperties> creature)
        {
            var addCur = 0f;
            if (buffer.AddPercent != 0f)
            {
                addCur += creature.ValueRO.FullHp * buffer.AddPercent;
                creature.ValueRW.FullHpFactor += buffer.AddPercent;
            }

            if (buffer.AddValue != 0f)
            {
                addCur += buffer.AddValue;
                creature.ValueRW.FullHp += buffer.AddValue;
            }
            
            if (addCur != 0)
            {
                creature.ValueRW.CurHp = creature.ValueRO.CurHp + addCur;
                if (creature.ValueRO.CurHp <= 0)
                {
                    creature.ValueRW.CurHp = 1;
                }
            }
        }

        private static void ProcessRemoveShield(GlobalAspect global, Entity entity, CreatureDataProcess buffer, ComponentLookup<ShieldProperties> shieldLookup, CreatureProperties creature,
            BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup, LocalTransform localTransform, EntityCommandBuffer ecb, bool isDisappear)
        {

            if (shieldLookup.TryGetComponent(entity, out var shield) && shieldLookup.IsComponentEnabled(entity))
            {
                //删除特效
                ecb.AppendToBuffer(global.Entity, new DestroyEffectByFrom { From = EEffectFrom.Shield, FromId = 1, Parent = entity});
                 
                //护盾破碎音效
                if (!isDisappear)
                {
                    //ecb.AppendToBuffer(global.Entity, new PlaySoundBuffer { SoundId = shield.BreakSound });
                }
                
                //破碎特效  
                if (!isDisappear)
                {
                    ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                    {
                        DelayDestroy = 1f,
                        ResourceId = (int)EEffectId.ShieldBreak,
                        Pos = CreatureHelper.getCenterPos(localTransform.Position, creature, localTransform.Scale),
                    });
                }

                //SkillTrigger
                SkillHelper.DoSkillTrigger(entity, skillEntitiesLookup, skillTagLookup, new SkillTriggerData( isDisappear ? ESkillTrigger.OnShieldDisappear : ESkillTrigger.OnShieldBreak), ecb);
                
                //disable shield properties
                ecb.SetComponentEnabled<ShieldProperties>(entity, false);
            }
        }

        private static void ProcessAddShield(GlobalAspect global, CreatureDataProcess buffer, CreatureProperties creature, LocalTransform masterTrans, ComponentLookup<ShieldProperties> shieldLookup,
            BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup, ComponentLookup<CreatureProperties> creatureLookup,  
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup,  Entity entity, EntityCommandBuffer ecb)
        {
            var hpPercent = buffer.AddPercent;
            var contTime = buffer.FloatValue1;

            if (hpPercent <= 0)
            {
                Debug.LogError("add shield error, hpPercent less than 0");
                return;
            }
            
            var maxHp = AttrHelper.GetMaxHp(entity, attrLookup, attrModifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            var shieldValue = maxHp * hpPercent;
            var shieldMaxValue = maxHp;

            if (shieldLookup.TryGetComponent(entity, out var shield) && shieldLookup.IsComponentEnabled(entity))
            {
                //已经存在的护盾，增加value即可, 如果超过最大值, 则=最大值
                var afterValue = shield.Value + shieldValue;
                if (afterValue > shieldMaxValue)
                {
                    afterValue = shieldMaxValue;
                }
                shieldLookup.GetRefRW(entity).ValueRW.Value = afterValue;
                shieldLookup.GetRefRW(entity).ValueRW.MaxValue = shieldMaxValue;
            }
            else
            {
                ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                {
                    From = EEffectFrom.Shield,
                    FromId = 1,
                    Loop = true,
                    ResourceId = (int)EEffectId.Shield,
                    Parent = entity,
                });
                
                ecb.SetComponent(entity, new ShieldProperties
                {
                    ContTime = contTime,
                    Value = shieldValue,
                    MaxValue = shieldMaxValue,
                    Timer = 0,
                });
                ecb.SetComponentEnabled<ShieldProperties>(entity, true);
            }
            
            //SkillTrigger
            SkillHelper.DoSkillTrigger(entity, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnShieldCreate), ecb);
        }

        private static void ProcessCure(Entity entity, GlobalAspect global, CreatureDataProcess buffer, RefRW<CreatureProperties> creature, ComponentLookup<CreatureProperties> creatureLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup, ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup, EntityCommandBuffer ecb)
        {
            var maxHp = AttrHelper.GetMaxHp(entity, attrLookup, attrModifyLookup, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);
            
            var addValue = buffer.AddValue;
            if (buffer.AddPercent != 0)
            {
                addValue += Mathf.CeilToInt(maxHp * buffer.AddPercent);
            }

            //基础属性加成
            var addFactor = BuffHelper.GetBuffAddFactor(entity, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.CureHpFactor);
            addValue = BuffHelper.CalcFactor(addValue, addFactor);

            if (addValue <= 0)
            {
                Debug.LogError($"CureHp error, add Value Less 0, value: {addValue}");
                return;
            }

       
            var beforePercent = creature.ValueRO.CurHp / maxHp;
            var beforeValue = creature.ValueRO.CurHp;

            var targetHp = creature.ValueRO.CurHp + addValue;
            if (targetHp > maxHp)
            {
                targetHp = maxHp;
            }

            creature.ValueRW.CurHp = targetHp;

            var afterPercent = targetHp / maxHp;
            var afterValue = targetHp;

            //SkillTrigger 血量变化
            SkillHelper.DoSkillTrigger(entity, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnHpChanged)
            {
                FloatValue1 = beforePercent,
                FloatValue2 = afterPercent,
                HpBefore = beforeValue,
                HpAfter = afterValue,
            }, ecb);

            //SkillTrigger 治疗
            SkillHelper.DoSkillTrigger(entity, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.AfterCure), ecb);

            //是否显示伤害数字
            if (!buffer.BoolValue)
            {
                var add = afterValue - beforeValue;
                if (add > 0)
                {
                    if (global.ShowDamageNumber)
                    {
                        ecb.SetComponentEnabled<DamageNumberBuffer>(entity, true);
                        ecb.AppendToBuffer(entity, new DamageNumberBuffer
                        {
                            Element = EElement.None,
                            Type = EDamageNumber.Cure,
                            Value = add
                        });
                    }
                }
            }
        }

        private static void ProcessKillEnemy(GlobalAspect global, Entity entity, int killCount, int killBossCount, ComponentLookup<LocalPlayerTag> localPlayerLookup, ComponentLookup<CreatureProperties> creatureLookup, EntityCommandBuffer ecb)
        {
            var masterRoot = CreatureHelper.GetMasterRoot(entity, creatureLookup);
            if (localPlayerLookup.HasComponent(masterRoot))
            {
                ecb.AppendToBuffer(masterRoot, new UIUpdateBuffer
                {
                    Value = new EventData
                    {
                        Command = EEventCommand.OnKillMonster,
                        Param1 = killCount,
                        Param2 = killBossCount
                    }
                });
            }
        }

        private static void ProcessResetSummonAngle(GlobalAspect global, Entity entity, CreatureDataProcess buffer, DynamicBuffer<SummonEntities> summons, ComponentLookup<MonsterMove> monsterMoveLookup, EntityCommandBuffer ecb)
        {
            if (!monsterMoveLookup.TryGetComponent(buffer.EntityValue, out var selfMove))
            {
                return;
            }

            if (selfMove.Mode != EMonsterMoveMode.Round)
            {
                return;
            }

            //找到所有AroundMove运行的Child
            var totalCount = 0;
            for (var i = 0; i < summons.Length; i++)
            {
                if (monsterMoveLookup.TryGetComponent(summons[i].Value, out var summonMonsterMove))
                {
                    if (summonMonsterMove.Mode == EMonsterMoveMode.Round && (int)selfMove.Param1 == (int)summonMonsterMove.Param1)
                    {
                        totalCount++;
                    }
                }
            }

            //重置所有AroundMove的Child的初始角度
            if (totalCount > 0)
            {
                var angleInterval = 360f / totalCount;
                var startAngle = 0f;
                for (var i = 0; i < summons.Length; i++)
                {
                    if (monsterMoveLookup.TryGetComponent(summons[i].Value, out var summonMonsterMove))
                    {
                        if (summonMonsterMove.Mode == EMonsterMoveMode.Round && (int)selfMove.Param1 == (int)summonMonsterMove.Param1)
                        {
                            monsterMoveLookup.GetRefRW(summons[i].Value).ValueRW.AroundAngle = startAngle;
                            startAngle += angleInterval;
                        }
                    }
                }
            }
        }
    }
}