using System;
using Deploys;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSystemGroup))]
    public partial struct UISystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalPlayerTag> _localPlayerLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<SkillProperties> _skillPropertiesLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private ComponentLookup<CreatureForward> _forwardLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<ServantProperties> _servantLookup;
        [ReadOnly] private ComponentLookup<MainServantTag> _mainServantLookup;
        [ReadOnly] private BufferLookup<Child> _childLookup;
        [ReadOnly] private ComponentLookup<EffectProperties> _effectLookup;
        [ReadOnly] private BufferLookup<BindingBullet> _bindingBulletLookup;
        [ReadOnly] private ComponentLookup<BulletProperties> _bulletLookup;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            
            _localPlayerLookup = state.GetComponentLookup<LocalPlayerTag>();
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillPropertiesLookup = state.GetComponentLookup<SkillProperties>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _summonEntitiesLookup = state.GetBufferLookup<SummonEntities>(true);
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _forwardLookup = state.GetComponentLookup<CreatureForward>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _mainServantLookup = state.GetComponentLookup<MainServantTag>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
            _effectLookup = state.GetComponentLookup<EffectProperties>(true);
            _bindingBulletLookup = state.GetBufferLookup<BindingBullet>(true);
            _bulletLookup = state.GetComponentLookup<BulletProperties>(true);
            
            _servantLookup = state.GetComponentLookup<ServantProperties>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _skillEntitiesLookup.Update(ref state);
            _skillPropertiesLookup.Update(ref state);
            _localPlayerLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _summonEntitiesLookup.Update(ref state);
            _monsterLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _forwardLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _servantLookup.Update(ref state);
            _mainServantLookup.Update(ref state);
            _childLookup.Update(ref state);
            _effectLookup.Update(ref state);
            _bindingBulletLookup.Update(ref state);
            _bulletLookup.Update(ref state);
            
            //更新inPause状态
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var playerAspect = SystemAPI.GetAspect<PlayerAspect>(SystemAPI.GetSingletonEntity<LocalPlayerTag>());
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            //发送UI事件（告知ts处理)
            foreach (var info in playerAspect.UIUpdateBuffer)
            {
                UIDataTransfer.DispatchFightEvent(info.Value);
            }
            playerAspect.UIUpdateBuffer.Clear();
            

            //响应UI(从ts发回的事件）
            foreach (var e in UIDataTransfer.UIEvents)
            {
                switch (e.Command)
                {
                    case UIEventToCSharp.TsWaveStart:
                    {
                        //TS 处理完了，通知可以开始了
                        global.WaveId += 1;
                        ecb.AddComponent<SpawnWaveInitTag>(global.Entity);
                        break;
                    }
                    case UIEventToCSharp.TsAddAttr:
                    {
                        var type = (EAttr)e.Param1;
                        var add = e.Param2;
                        AttrHelper.AddAttr(playerAspect, type, add, ecb);
                        break;
                    }
                    case UIEventToCSharp.AddServant:
                    {
                        var servantId = (int)e.Param1;
                        var rarity = (ERarity)e.Param2;
                        var uniqueId = (int)e.Param3;
                        var idx = (int)e.Param4;

                        if (idx == -1)
                        {
                            idx = playerAspect.ServantCount;
                        }
                        if (FightData.GetServantProp(servantId, out var servantProps))
                        {
                            SystemAPI.TryGetSingletonEntity<MainServantTag>(out var mainServant);
                            var bornPos = CreatureHelper.GetServantBornPos(playerAspect, global, idx, mainServant, _transformLookup, _forwardLookup);
                        
                            //Debug.LogError($"create servant:{buffer.ServantId}");
                            FactoryHelper.CreateServant(playerAspect.Entity, collisionWorld, global, cache, servantId, uniqueId, rarity, idx, servantProps, bornPos, ecb);

                            //给主角加上对应英灵的dps统计buffer
                            ecb.AppendToBuffer(playerAspect.Entity, new PlayerDpsBuffer { ServantId = servantId, DpsStartTime = global.Time });
                        }
                        break;
                    }
                    case UIEventToCSharp.RemoveServant:
                    {
                        var uniqueId = (int)e.Param1;
                        var bFind = false;
                        foreach (var buffer in  playerAspect.Servants)
                        {
                            if (_servantLookup.TryGetComponent(buffer.Value, out var properties))
                            {
                                if (properties.UniqueId == uniqueId)
                                {
                                    CreatureHelper.UnbindServant(playerAspect, global, buffer.Value, _transformLookup, _servantLookup, _mainServantLookup,
                                        _childLookup, _effectLookup,  _bindingBulletLookup,_bulletLookup, ecb, 0.1f);
                                    
                                    bFind = true;
                                    break;
                                }
                            }
                        }

                        if (!bFind)
                        {
                            Debug.LogError($"remove servant error, cant find, uniqueId:{uniqueId}");
                        }
                        break;
                    }
                    case UIEventToCSharp.ReplaceServant:
                    {
                        var changeUniqueId = (int)e.Param1;
                        var servantId = (int)e.Param2;
                        var rarity = (ERarity)e.Param3;
                        var delUniqueId = (int)e.Param4;
                        
                        //change rarity
                        foreach (var buffer in  playerAspect.Servants)
                        {
                            if (_servantLookup.TryGetComponent(buffer.Value, out var properties))
                            {
                                if (properties.UniqueId == changeUniqueId)
                                {
                                    _servantLookup.GetRefRW(buffer.Value).ValueRW.Rarity = rarity;
                                    _servantLookup.GetRefRW(buffer.Value).ValueRW.Id = servantId;
                                    
                                    ecb.AddComponent(buffer.Value, new HybridEvent_ChangeServant
                                    {
                                        ServantId = servantId,
                                        Rarity = rarity
                                    });
                                }
                            }
                        }

                        //remove
                        foreach (var buffer in playerAspect.Servants)
                        {
                            if (_servantLookup.TryGetComponent(buffer.Value, out var properties))
                            {
                                if (properties.UniqueId == delUniqueId)
                                {
                                    CreatureHelper.UnbindServant(playerAspect, global, buffer.Value, _transformLookup, _servantLookup, _mainServantLookup,
                                        _childLookup, _effectLookup,  _bindingBulletLookup,_bulletLookup, ecb, 0.1f);
                                }
                            }
                        }
                        break;
                    }
                    case UIEventToCSharp.AddSkill:
                    {
                        var skillId = (int)e.Param1;
                        SkillHelper.AddSkill(global.Entity, playerAspect.Entity, skillId, playerAspect.Creature.ValueRO.AtkValue, playerAspect.Position, ecb);
                        break;
                    }
                    case UIEventToCSharp.RemoveSkill:
                    {
                        var skillId = (int)e.Param1;
                        SkillHelper.RemoveSkill(global.Entity, playerAspect.Entity, skillId, ecb);
                        break;
                    }
                    case UIEventToCSharp.ClearAllSkill:
                    {
                        SkillHelper.RemoveAllSkill(global.Entity, playerAspect.Entity, _skillEntitiesLookup, _skillPropertiesLookup, ecb);
                        break;
                    }
                    case UIEventToCSharp.AddDropItem:
                    {
                        var dropItemId = (int)e.Param1;
                        var dropDistance = e.Param2;

                        if (_forwardLookup.TryGetComponent(playerAspect.Entity, out var forwardInfo))
                        {
                            var pos = playerAspect.Position + forwardInfo.FaceForward * dropDistance;
                            ecb.AppendToBuffer(global.Entity, new DropItemCreateBuffer
                            {
                                Pos = pos,
                                DropItemId = dropItemId,
                                Count = 1,
                                RandomRange = 0.2f,
                            });
                        }
                        else
                        {
                            Debug.LogError("UI SYSTEM AddDropItem Error, no component CreatureForward");
                        }

                        break;
                    }
                    case UIEventToCSharp.Revive:
                    {
                        ecb.SetComponentEnabled<EnterReviveTag>(playerAspect.Entity, true);
                        break;
                    }
                    case UIEventToCSharp.Pause:
                    {
                        global.InPause = true;
                        global.PauseTag = true;
                        break;
                    }
                    case UIEventToCSharp.UnPause:
                    {
                        global.InPause = false;
                        global.UnPauseTag = true;
                        break;
                    }
                    //主角升级
                    case UIEventToCSharp.UpgradeLevel:
                    {
                        var level = (int)e.Param1;
                        var needExp = (int)e.Param2;

                        //SkillTrigger
                        SkillHelper.DoSkillTrigger(playerAspect.Entity, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnLevelUp), ecb);

                        //记录玩家等级
                        playerAspect.Properties.ValueRW.Level = level;
                        playerAspect.Properties.ValueRW.NeedExp = needExp;
                        break;
                    }
                    //ts重进战斗
                    case UIEventToCSharp.SetCurHp:
                    {
                        playerAspect.Creature.ValueRW.CurHp = e.Param1;
                        break;
                    }
                    //清空dps统计
                    case UIEventToCSharp.ClearPlayerDps:
                    {
                        var list = new NativeArray<PlayerDpsBuffer>(playerAspect.DpsBuffer.Length, Allocator.Temp);
                        for (var i = 0; i < playerAspect.DpsBuffer.Length; i++)
                        {
                            list[i] = playerAspect.DpsBuffer[i];
                        }

                        for (var i = 0; i < list.Length; i++)
                        {
                            var dot = list[i];
                            dot.DpsStartTime = global.Time;
                            dot.DpsTotalDamage = 0;
                            list[i] = dot;
                        }

                        playerAspect.DpsBuffer.Clear();
                        playerAspect.DpsBuffer.CopyFrom(list);
                        break;
                    }
                    //加0%血
                    case UIEventToCSharp.AddHpPercent:
                    {
                        var addPercent = e.Param1 / 100f;
                        ecb.AppendToBuffer(playerAspect.Entity, new CreatureDataProcess { Type = ECreatureDataProcess.Cure, AddPercent = addPercent });
                        ecb.SetComponentEnabled<CreatureDataProcess>(playerAspect.Entity, true);
                        break;
                    }
                    case UIEventToCSharp.BanHurtNumber:
                    {
                        var ban = (int)e.Param1 == 1;
                        global.ShowDamageNumber = !ban;
                        break;
                    }
                    default:
                    {
                        Debug.LogError("UI EVENT TO CSHARP ERROR, COMMAND:" + (int)e.Command);
                        break;
                    }
                }
            }

            UIDataTransfer.UIEvents.Clear();

            //更新血量, 护盾
            {
                var maxHp = AttrHelper.GetMaxHp(playerAspect.Entity, _attrLookup, _attrModifyLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                UIDataTransfer.Hp.Current = playerAspect.Creature.ValueRO.CurHp;
                UIDataTransfer.Hp.Max = maxHp;
                UIDataTransfer.Hp.HasShield = playerAspect.Shield.ValueRO is { Value: > 0, MaxValue: > 0 };
                UIDataTransfer.Hp.CurShield = playerAspect.Shield.ValueRO.Value;
                UIDataTransfer.Hp.MaxShield = playerAspect.Shield.ValueRO.MaxValue;
                UIDataTransfer.InPause = global.InPause;
            }
            
            //更新servant hp
            foreach (var servant in playerAspect.Servants)
            {
                if (_servantLookup.TryGetComponent(servant.Value, out var servantProperties) && _creatureLookup.TryGetComponent(servant.Value, out var servantCreature))
                {
                    var uniqueId = servantProperties.UniqueId;
                    UIDataTransfer.ServantHp.TryAdd(uniqueId, new UIHpInfo());
                    
                    var servantMaxHp = AttrHelper.GetMaxHp(servant.Value, _attrLookup, _attrModifyLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                    var servantCurHp = servantCreature.CurHp;
                    UIDataTransfer.ServantHp[uniqueId].Current = servantCurHp;
                    UIDataTransfer.ServantHp[uniqueId].Max = servantMaxHp;
                }
            }

            //更新dps
            for (var i = 0; i < playerAspect.DpsBuffer.Length; i++)
            {
                var bFind = false;
                var summonId = playerAspect.DpsBuffer[i].ServantId;
                var dps = playerAspect.DpsBuffer[i].DpsTotalDamage / (global.Time - playerAspect.DpsBuffer[i].DpsStartTime);
                var dmg = playerAspect.DpsBuffer[i].DpsTotalDamage;
                for (var j = 0; j < UIDataTransfer.Dps.Count; j++)
                {
                    if (UIDataTransfer.Dps[j].SummonId == summonId)
                    {
                        UIDataTransfer.Dps[j].Dps = dps;
                        UIDataTransfer.Dps[j].Damage = dmg;
                        bFind = true;
                        break;
                    }
                }

                if (!bFind)
                {
                    UIDataTransfer.Dps.Add(new DpsInfo
                    {
                        SummonId = summonId,
                        Dps = dps,
                        Damage = dmg,
                    });
                }
            }

            //bossId
            UIDataTransfer.CurrBoss.MonsterId = global.CurrBossId;
            UIDataTransfer.CurrBoss.HpPercent = global.CurrBossHpPercent;
            UIDataTransfer.TotalMonsterCount = global.TotalMonsterCount;
            UIDataTransfer.WaveTime = global.WaveCurTime;
            
            //update attrs
            var attrList = Enum.GetValues(typeof(EAttr));
            foreach (var attr in attrList)
            {
                var type = (EAttr)attr;
                if (type != EAttr.None)
                {
                    UIDataTransfer.Attrs[type] = AttrHelper.GetAttr(playerAspect.Entity, type, _attrLookup, _attrModifyLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                    UIDataTransfer.AttrMax[type] = AttrHelper.GetMax(type, playerAspect.Entity, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                }
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}