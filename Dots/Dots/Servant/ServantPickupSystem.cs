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
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    [UpdateAfter(typeof(PlayerDeadSystem))]
    public partial struct ServantPickupSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<ServantList> _servantListLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _servantListLookup = state.GetBufferLookup<ServantList>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
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

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            _skillEntitiesLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _servantListLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);

            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            var localCreature = SystemAPI.GetComponentRO<CreatureProperties>(localPlayer);
            
            //拾取 
            new PlayerPickupJob
            {
                Ecb = ecb.AsParallelWriter(),
                LocalCreature= localCreature.ValueRO,
                GlobalEntity = global.Entity,
                LocalPlayer = localPlayer,
                SkillEntitiesLookup = _skillEntitiesLookup,
                DeadLookup = _deadLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                SkillTagLookup = _skillTagLookup,
                CreatureLookup = _creatureLookup,
                ServantListLookup = _servantListLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
            }.ScheduleParallel(); 
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct PlayerPickupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity GlobalEntity;
            public Entity LocalPlayer;

            [ReadOnly] public CreatureProperties LocalCreature;
            
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public BufferLookup<ServantList> ServantListLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            
            [BurstCompile]
            private void Execute(MainServantTag mainServant, DynamicBuffer<PickupBuffer> buffer, LocalTransform localTransform, Entity servantEntity, [EntityIndexInQuery] int sortKey)
            {
                if (DeadLookup.IsComponentEnabled(servantEntity))
                {
                    return;
                }

                var totalExp = 0;
                var totalGold = 0;
                var totalAddHpFactor = 0f;
                for (var i = 0; i < buffer.Length; i++)
                {
                    var data = buffer[i];

                    //SkillTrigger
                    SkillHelper.DoSkillTrigger(LocalPlayer, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnPickupItem)
                    {
                        IntValue1 = data.DropItemId
                    }, Ecb, sortKey);

                    switch (data.Action)
                    {
                        //宝箱
                        case EDropItemAction.Treasure:
                        {
                            Ecb.AppendToBuffer(sortKey, LocalPlayer, new UIUpdateBuffer
                            {
                                Value = new EventData
                                {
                                    Command = EEventCommand.OnPickTreasure,
                                }
                            });
                            break;
                        }
                        case EDropItemAction.AddHp:
                        {
                            var addPercent = data.Param1.ToFloat();
                            totalAddHpFactor += addPercent;

                            //SkillTrigger
                            var playerMaxHp = AttrHelper.GetMaxHp(LocalPlayer, AttrLookup, AttrModifyLookup, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                            SkillHelper.DoSkillTrigger(LocalPlayer, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnPickupHp)
                            {
                                FloatValue1 = LocalCreature.CurHp / playerMaxHp,
                            }, Ecb, sortKey);
                            break;
                        }
                        case EDropItemAction.AddGold:
                        {
                            var addGold = data.Param1.ToInt();
                            if (data.Value > 0)
                            {
                                addGold = data.Value;
                            }
                            totalGold += addGold;

                            //SkillTrigger
                            SkillHelper.DoSkillTrigger(LocalPlayer, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnPickupGold), Ecb, sortKey);
                            break;
                        }
                        case EDropItemAction.AddExp:
                        {
                            var addExp = data.Param1.ToInt();
                            if (data.Value > 0)
                            {
                                addExp = data.Value;
                            }
                            totalExp += addExp;

                            //SkillTrigger
                            SkillHelper.DoSkillTrigger(LocalPlayer, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnPickupExp), Ecb, sortKey);
                            break;
                        }
                        case EDropItemAction.AddShield:
                        {
                            var addPercent = data.Param1.ToFloat();
                            var shieldCreateBuffer = new CreatureDataProcess
                            {
                                Type = ECreatureDataProcess.AddShield,
                                AddPercent = addPercent,
                            };
                            
                            //local player的护盾
                            Ecb.AppendToBuffer(sortKey, LocalPlayer, shieldCreateBuffer);
                            Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, LocalPlayer, true);

                            if (ServantListLookup.TryGetBuffer(LocalPlayer, out var servantList))
                            {
                                foreach (var servant in servantList)
                                {
                                    Ecb.AppendToBuffer(sortKey, servant.Value, shieldCreateBuffer);
                                    Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, servant.Value, true);
                                }
                            }
                            
                            break;
                        }
                        //拾取所有物品
                        case EDropItemAction.Magnet:
                        {
                            var radius = data.Param1.ToFloat();
                            Ecb.SetComponent(sortKey, servantEntity, new PickupMagnetTag { Radius = radius });
                            Ecb.SetComponentEnabled<PickupMagnetTag>(sortKey, servantEntity, true);
                            break;
                        }
                        case EDropItemAction.CastSkill:
                        {
                            var skillId = data.Param1.ToInt();
                            var contTime = data.Param2.ToFloat();

                            if (skillId > 0)
                            {
                                //装备上技能
                                SkillHelper.AddSkill(GlobalEntity, LocalPlayer, skillId, LocalCreature.AtkValue, localTransform.Position, Ecb, sortKey);

                                //加tag恢复
                                if (contTime > 0)
                                {
                                    Ecb.SetComponent(sortKey, LocalPlayer, new PickupSkillTag { SkillId = skillId, ContTime = contTime });
                                    Ecb.SetComponentEnabled<PickupSkillTag>(sortKey, LocalPlayer, true);    
                                }
                            }

                            break;
                        }
                        case EDropItemAction.None:
                        {
                            break;
                        }
                        default:
                        {
                            Debug.LogError($"PlayerPickupJob error, 不存在的action:{(int)data.Action}");
                            break;
                        }
                    }
                }

                //加血处理
                if (totalAddHpFactor > 0)
                {
                    Ecb.AppendToBuffer(sortKey, LocalPlayer, new CreatureDataProcess { Type = ECreatureDataProcess.Cure, AddPercent = totalAddHpFactor });
                    Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, LocalPlayer, true);
                }

                //处理金币
                if (totalGold > 0)
                {
                    //通知UI加金币
                    Ecb.AppendToBuffer(sortKey, LocalPlayer, new UIUpdateBuffer
                    {
                        Value = new EventData
                        {
                            Command = EEventCommand.OnPickGold,
                            Param1 = totalGold
                        }
                    });
                }

                //处理经验
                if (totalExp > 0)
                {
                    //通知ui加经验
                    Ecb.AppendToBuffer(sortKey, LocalPlayer, new UIUpdateBuffer
                    {
                        Value = new EventData
                        {
                            Command = EEventCommand.OnPickExp,
                            Param1 = totalExp
                        }
                    });
                }

                //执行完毕后禁用buffer
                buffer.Clear();
                Ecb.SetComponentEnabled<PickupBuffer>(sortKey, servantEntity, false);
            }
        }
    }
}