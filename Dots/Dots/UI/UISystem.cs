using System;
using Deploys;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [UpdateInGroup(typeof(LateSystemGroup))]
    public partial struct UISystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalPlayerTag> _localPlayerLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<SkillProperties> _skillPropertiesLookup;
        [ReadOnly] private ComponentLookup<StatusHp> _hpLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private ComponentLookup<StatusForward> _forwardLookup;
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
            _hpLookup = state.GetComponentLookup<StatusHp>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _summonEntitiesLookup = state.GetBufferLookup<SummonEntities>(true);
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _forwardLookup = state.GetComponentLookup<StatusForward>(true);
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
            _hpLookup.Update(ref state);
            _summonLookup.Update(ref state);
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
            

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            var servantList = SystemAPI.GetBuffer<ServantList>(localPlayer);
            var uiEvents = SystemAPI.GetBuffer<UIUpdateBuffer>(localPlayer);
            var playerAttr = SystemAPI.GetComponentRW<PlayerAttrData>(localPlayer);
            var playerTrans = SystemAPI.GetComponent<LocalToWorld>(localPlayer);
            var playerProps = SystemAPI.GetComponent<CreatureProps>(localPlayer);
            var playerTag = SystemAPI.GetComponentRW<LocalPlayerTag>(localPlayer);
            var playerHp = SystemAPI.GetComponentRW<StatusHp>(localPlayer);
            var playerDps = SystemAPI.GetBuffer<PlayerDpsBuffer>(localPlayer);
            var playerShield = SystemAPI.GetComponent<ShieldProperties>(localPlayer);
            
            //发送UI事件（告知ts处理)
            foreach (var uiEvent in uiEvents)
            {
                UIDataTransfer.DispatchFightEvent(uiEvent.Value);
            }
            uiEvents.Clear();


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
                        AttrHelper.AddAttr(localPlayer, playerAttr, servantList, type, add, ecb);
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
                            idx = servantList.Length;
                        }

                        if (FightData.GetServantProp(servantId, out var servantProps))
                        {
                            SystemAPI.TryGetSingletonEntity<MainServantTag>(out var mainServant);
                            var bornPos = CreatureHelper.GetServantBornPos(playerTrans, global, idx, mainServant, _transformLookup, _forwardLookup);

                            //Debug.LogError($"create servant:{buffer.ServantId}");
                            FactoryHelper.CreateServant(localPlayer, collisionWorld, global, cache, servantId, uniqueId, rarity, idx, servantProps, bornPos, ecb);

                            //给主角加上对应英灵的dps统计buffer
                            ecb.AppendToBuffer(localPlayer, new PlayerDpsBuffer { ServantId = servantId, DpsStartTime = global.Time });
                        }

                        break;
                    }
                    case UIEventToCSharp.RemoveServant:
                    {
                        var uniqueId = (int)e.Param1;
                        var bFind = false;
                        foreach (var buffer in servantList)
                        {
                            if (_servantLookup.TryGetComponent(buffer.Value, out var properties))
                            {
                                if (properties.UniqueId == uniqueId)
                                {
                                    CreatureHelper.UnbindServant(servantList, global, buffer.Value, _transformLookup, _servantLookup, _mainServantLookup,
                                        _childLookup, _effectLookup, _bindingBulletLookup, _bulletLookup, ecb, 0.1f);

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
                        foreach (var buffer in servantList)
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
                        foreach (var buffer in servantList)
                        {
                            if (_servantLookup.TryGetComponent(buffer.Value, out var properties))
                            {
                                if (properties.UniqueId == delUniqueId)
                                {
                                    CreatureHelper.UnbindServant(servantList, global, buffer.Value, _transformLookup, _servantLookup, _mainServantLookup,
                                        _childLookup, _effectLookup, _bindingBulletLookup, _bulletLookup, ecb, 0.1f);
                                    break;
                                }
                            }
                        }

                        break;
                    }
                    case UIEventToCSharp.AddSkill:
                    {
                        var skillId = (int)e.Param1;
                        SkillHelper.AddSkill(global.Entity, localPlayer, skillId, playerProps.AtkValue, playerTrans.Position, ecb);
                        break;
                    }
                    case UIEventToCSharp.RemoveSkill:
                    {
                        var skillId = (int)e.Param1;
                        SkillHelper.RemoveSkill(global.Entity, localPlayer, skillId, ecb);
                        break;
                    }
                    case UIEventToCSharp.ClearAllSkill:
                    {
                        SkillHelper.RemoveAllSkill(global.Entity, localPlayer, _skillEntitiesLookup, _skillPropertiesLookup, ecb);
                        break;
                    }
                    case UIEventToCSharp.AddDropItem:
                    {
                        var dropItemId = (int)e.Param1;
                        var dropDistance = e.Param2;

                        if (_forwardLookup.TryGetComponent(localPlayer, out var forwardInfo))
                        {
                            var pos = playerTrans.Position + forwardInfo.FaceForward * dropDistance;
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
                        ecb.SetComponentEnabled<EnterReviveTag>(localPlayer, true);
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
                        SkillHelper.DoSkillTrigger(localPlayer, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnLevelUp), ecb);

                        //记录玩家等级
                        playerTag.ValueRW.Level = level;
                        playerTag.ValueRW.NeedExp = needExp;
                        break;
                    }
                    //ts重进战斗
                    case UIEventToCSharp.SetCurHp:
                    {
                        playerHp.ValueRW.CurHp = e.Param1;
                        break;
                    }
                    //清空dps统计
                    case UIEventToCSharp.ClearPlayerDps:
                    {
                        var list = new NativeArray<PlayerDpsBuffer>(playerDps.Length, Allocator.Temp);
                        for (var i = 0; i < playerDps.Length; i++)
                        {
                            list[i] = playerDps[i];
                        }

                        for (var i = 0; i < list.Length; i++)
                        {
                            var dot = list[i];
                            dot.DpsStartTime = global.Time;
                            dot.DpsTotalDamage = 0;
                            list[i] = dot;
                        }

                        playerDps.Clear();
                        playerDps.CopyFrom(list);
                        break;
                    }
                    //加0%血
                    case UIEventToCSharp.AddHpPercent:
                    {
                        var addPercent = e.Param1 / 100f;
                        ecb.AppendToBuffer(localPlayer, new CreatureDataProcess { Type = ECreatureDataProcess.Cure, AddPercent = addPercent });
                        ecb.SetComponentEnabled<CreatureDataProcess>(localPlayer, true);
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
                var maxHp = AttrHelper.GetMaxHp(localPlayer, _attrLookup, _attrModifyLookup, _hpLookup, _summonLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                UIDataTransfer.Hp.Current = playerHp.ValueRO.CurHp;
                UIDataTransfer.Hp.Max = maxHp;
                UIDataTransfer.Hp.HasShield = playerShield is { Value: > 0, MaxValue: > 0 };
                UIDataTransfer.Hp.CurShield = playerShield.Value;
                UIDataTransfer.Hp.MaxShield = playerShield.MaxValue;
                UIDataTransfer.InPause = global.InPause;
            }

            //更新servant hp
            foreach (var servant in servantList)
            {
                if (_servantLookup.TryGetComponent(servant.Value, out var servantProperties) && _hpLookup.TryGetComponent(servant.Value, out var servantCreature))
                {
                    var uniqueId = servantProperties.UniqueId;
                    UIDataTransfer.ServantHp.TryAdd(uniqueId, new UIHpInfo());

                    var servantMaxHp = AttrHelper.GetMaxHp(servant.Value, _attrLookup, _attrModifyLookup, _hpLookup, _summonLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                    var servantCurHp = servantCreature.CurHp;
                    UIDataTransfer.ServantHp[uniqueId].Current = servantCurHp;
                    UIDataTransfer.ServantHp[uniqueId].Max = servantMaxHp;
                }
            }

            //更新dps
            for (var i = 0; i < playerDps.Length; i++)
            {
                var bFind = false;
                var summonId = playerDps[i].ServantId;
                var dps =playerDps[i].DpsTotalDamage / (global.Time - playerDps[i].DpsStartTime);
                var dmg = playerDps[i].DpsTotalDamage;
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
                    UIDataTransfer.Attrs[type] = AttrHelper.GetAttr(localPlayer, type, _attrLookup, _attrModifyLookup, _summonLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                    UIDataTransfer.AttrMax[type] = AttrHelper.GetMax(type, localPlayer, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}