using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    [UpdateAfter(typeof(MonsterMoveSystem))]
    public partial struct MonsterDeadSystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private ComponentLookup<ProgressBarComponent> _progressBarLookup;
        [ReadOnly] private ComponentLookup<EffectProperties> _effectLookup;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<MonsterMove> _monsterMoveLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_SetActive> _eventSetActive;
        [ReadOnly] private BufferLookup<Child> _childLookup;
        [ReadOnly] private BufferLookup<BindingBullet> _bindingBulletLookup;
        [ReadOnly] private ComponentLookup<BulletProperties> _bulletLookup;
        [ReadOnly] private ComponentLookup<MonsterDropMaterial> _dropMaterialLookup;
        [ReadOnly] private ComponentLookup<MonsterDropGold> _dropGoldLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
         
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _progressBarLookup = state.GetComponentLookup<ProgressBarComponent>(true);
            _effectLookup = state.GetComponentLookup<EffectProperties>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
            _monsterMoveLookup = state.GetComponentLookup<MonsterMove>(true);
            _summonEntitiesLookup =  state.GetBufferLookup<SummonEntities>(true);
            _eventSetActive = state.GetComponentLookup<HybridEvent_SetActive>(true);
            _bindingBulletLookup = state.GetBufferLookup<BindingBullet>(true);
            _bulletLookup = state.GetComponentLookup<BulletProperties>(true);
            _dropMaterialLookup = state.GetComponentLookup<MonsterDropMaterial>(true);
            _dropGoldLookup = state.GetComponentLookup<MonsterDropGold>(true);
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

            _skillEntitiesLookup.Update(ref state);
            _localToWorldLookup.Update(ref state);
            _progressBarLookup.Update(ref state);
            _effectLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _childLookup.Update(ref state);
            _monsterMoveLookup.Update(ref state);
            _summonEntitiesLookup.Update(ref state);
            _eventSetActive.Update(ref state);
            _bindingBulletLookup.Update(ref state);
            _bulletLookup.Update(ref state);
            _dropMaterialLookup.Update(ref state);
            _dropGoldLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

            //死亡处理
            new ProcessDieJob
            {
                Ecb = ecb.AsParallelWriter(),
                GlobalEntity = global.Entity,
                ProgressBarLookup = _progressBarLookup,
                SkillEntitiesLookup = _skillEntitiesLookup,
                EffectLookup = _effectLookup,
                SkillTagLookup = _skillTagLookup,
                TransformLookup = _localToWorldLookup,
                DeadLookup = _deadLookup,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                SummonLookup = _summonLookup,
                ChildLookup = _childLookup,
                MonsterMoveLookup = _monsterMoveLookup,
                EventSetActive = _eventSetActive,
                SummonEntitiesLookup = _summonEntitiesLookup,
                BindingBulletLookup = _bindingBulletLookup,
                BulletLookup = _bulletLookup,
                DropMaterialLookup = _dropMaterialLookup,
                DropGoldLookup = _dropGoldLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct ProcessDieJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity CacheEntity;
            public Entity GlobalEntity;

            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public ComponentLookup<ProgressBarComponent> ProgressBarLookup;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<EffectProperties> EffectLookup;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<HybridEvent_SetActive> EventSetActive;
            [ReadOnly] public ComponentLookup<MonsterMove> MonsterMoveLookup;
            [ReadOnly] public BufferLookup<SummonEntities> SummonEntitiesLookup;
            [ReadOnly] public BufferLookup<Child> ChildLookup;
            [ReadOnly] public BufferLookup<BindingBullet> BindingBulletLookup;
            [ReadOnly] public ComponentLookup<BulletProperties> BulletLookup;
            [ReadOnly] public ComponentLookup<MonsterDropMaterial> DropMaterialLookup;
            [ReadOnly] public ComponentLookup<MonsterDropGold> DropGoldLookup;
            
            [BurstCompile]
            private void Execute(EnterDieTag enterDieTag, MonsterProperties monsterProperties, 
                CreatureTag creatureTag,
                DynamicBuffer<MonsterDropInfo> dropList, RefRW<LocalTransform> localTransform,
                RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetMonsterConfig(monsterProperties.Id, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                if (DeadLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                Ecb.SetComponentEnabled<EnterDieTag>(sortKey, entity, false);
                Ecb.SetComponentEnabled<InDeadState>(sortKey, entity, true);

                if (!enterDieTag.BanDrop)
                {
                    var startPos = localTransform.ValueRO.Position;
                    var circleCenterPos = startPos;
                    var circleId = 0;
                    var circleFinished = true;
                    var circleCurCount = 0;
                    var distInterval = 1.5f;

                    for (var i = 0; i < dropList.Length; i++)
                    {
                        var buff = dropList[i];

                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new DropItemCreateBuffer
                        {
                            Pos = startPos,
                            DropItemId = buff.ItemId,
                            Count = buff.ItemCount,
                        });

                        if (circleFinished)
                        {
                            circleId += 1;
                            startPos = circleCenterPos + new float3(0, distInterval * circleId, 0);
                            circleFinished = false;
                            circleCurCount = 1;
                        }
                        else
                        {
                            //绕圈
                            var angle = 60f / circleId;
                            var x = distInterval * circleId * math.sin(math.radians(angle) * circleCurCount);
                            var y = distInterval * circleId * math.cos(math.radians(angle) * circleCurCount);
                            var pos = circleCenterPos + new float3(x, y, 0);
                            startPos = pos;
                            circleCurCount++;

                            var maxCircleCount = 360f / angle;
                            if (circleCurCount > maxCircleCount)
                            {
                                circleCurCount = 0;
                                circleFinished = true;
                            }
                        }
                    }
                    
                    //wave的材料掉落
                    if (DropMaterialLookup.TryGetComponent(entity, out var dropMaterial))
                    {
                        var dropCount = dropMaterial.Count;
                        FactoryDropItemSystem.AutoDropMaterial(dropCount, startPos, random, GlobalEntity, Ecb, sortKey);
                    }

                    //drop gold
                    if (DropGoldLookup.TryGetComponent(entity, out var dropGold))
                    {
                        FactoryDropItemSystem.AutoDropGold(dropGold.Count, startPos, random, GlobalEntity, Ecb, sortKey);
                    }
                    
                }

                //延迟10帧进行，否则OnDead触发的技能放不出来
                Ecb.SetComponent(sortKey, entity, new MonsterDestroyTag { DestroyDelay = 0.2f, });
                Ecb.SetComponentEnabled<MonsterDestroyTag>(sortKey, entity, true);

                //remove all effect
                FactoryHelper.RemoveAllEffect(entity, GlobalEntity, ChildLookup, EffectLookup, Ecb, sortKey);

                //remove bind bullet
                BulletHelper.ClearAllBindingBullet(entity, BindingBulletLookup, BulletLookup, Ecb, sortKey);
                
                //清理所有召唤物
                if (SummonEntitiesLookup.TryGetBuffer(entity, out var summons))
                {
                    foreach (var summonEntity in summons)
                    {
                        if (TransformLookup.HasComponent(summonEntity.Value))
                        {
                            Ecb.SetComponent(sortKey, summonEntity.Value, new EnterDieTag { BanTrigger = true });
                            Ecb.SetComponentEnabled<EnterDieTag>(sortKey, summonEntity.Value, true);
                        }  
                    }
                }
                
                //SkillTrigger
                if (!enterDieTag.BanTrigger)
                {
                    SkillHelper.DoSkillTrigger(entity, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnDead), Ecb, sortKey);    
                }
                
                //移除血条（如果有）
                if (ProgressBarLookup.HasComponent(entity))
                {
                    var progressBarEntity = ProgressBarLookup.GetRefRO(entity).ValueRO.Value;
                    if (TransformLookup.HasComponent(progressBarEntity))
                    {
                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = progressBarEntity });
                    }

                    Ecb.SetComponentEnabled<ProgressBarComponent>(sortKey, entity, false);
                }

                //global event
                Ecb.AppendToBuffer(sortKey, GlobalEntity, new MonsterDieEvent
                {
                    SpawnPointId = monsterProperties.SpawnPointId,
                    CollisionType = config.CollisionType,
                    Type = creatureTag.Type,
                    MonsterId = monsterProperties.Id,
                });

                //隐藏Prefab特效
                if (EventSetActive.HasComponent(entity))
                {
                    Ecb.SetComponent(sortKey, entity, new HybridEvent_SetActive { Value = false });
                    Ecb.SetComponentEnabled<HybridEvent_SetActive>(sortKey, entity, true);
                }
                
                //序列帧死亡动画
                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                {
                    ResourceId = config.DieResId,
                    Pos = localTransform.ValueRO.Position,
                    Scale = localTransform.ValueRO.Scale,

                    MoveData = new AnimationMoveData
                    {
                        Enable = true,
                        Speed = 3,
                        TotalTime = 0,
                        Forward = enterDieTag.HitForward,
                    }
                });

                Ecb.AppendToBuffer(sortKey, entity, new CreatureDataProcess { Type = ECreatureDataProcess.SetActive, IntValue1 = 0});
                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, entity, true);
            
                //死亡音效
                if (config.DieSound > 0)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new PlaySoundBuffer
                    {
                        SoundId = config.DieSound
                    });
                }

                //召唤处理
                if (SummonLookup.TryGetComponent(entity, out var summon))
                {
                    if (SummonLookup.HasComponent(summon.SummonParent))
                    {
                        //如果自己是召唤物, 且自己是环绕运行的， 则要给Parent触发召唤物变化事件
                        if (!enterDieTag.FromHatch && MonsterMoveLookup.TryGetComponent(entity, out var monsterMove) && monsterMove.Mode == EMonsterMoveMode.Round)
                        {
                            Ecb.AppendToBuffer(sortKey, summon.SummonParent, new CreatureDataProcess { Type = ECreatureDataProcess.ResetSummonsAroundAngle, EntityValue = entity });
                            Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, summon.SummonParent, true);
                        }

                        //标志召唤物死亡，Parent要在列表中remove掉
                        Ecb.AppendToBuffer(sortKey, summon.SummonParent, new CreatureDataProcess { Type = ECreatureDataProcess.SummonEntitiesDie, EntityValue = entity });
                        Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, summon.SummonParent, true);
                    }
                }
            }
        }
    }
}