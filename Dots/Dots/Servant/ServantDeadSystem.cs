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
    public partial struct ServantDeadSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<ServantProperties> _servantLookup;
        [ReadOnly] private ComponentLookup<MainServantTag> _mainServantLookup;
        [ReadOnly] private BufferLookup<Child> _childLookup;
        [ReadOnly] private ComponentLookup<EffectProperties> _effectLookup;
        [ReadOnly] private BufferLookup<BindingBullet> _bindingBulletLookup;
        [ReadOnly] private ComponentLookup<BulletProperties> _bulletLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_PlayAnimation> _eventPlayAnimation;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<LocalPlayerTag>();
            
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _mainServantLookup = state.GetComponentLookup<MainServantTag>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
            _effectLookup = state.GetComponentLookup<EffectProperties>(true);
            _bindingBulletLookup = state.GetBufferLookup<BindingBullet>(true);
            _bulletLookup = state.GetComponentLookup<BulletProperties>(true);
            _summonEntitiesLookup =  state.GetBufferLookup<SummonEntities>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _eventPlayAnimation = state.GetComponentLookup<HybridEvent_PlayAnimation>(true);
            
            _servantLookup = state.GetComponentLookup<ServantProperties>();
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
            _deadLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _servantLookup.Update(ref state);
            _mainServantLookup.Update(ref state);
            _childLookup.Update(ref state);
            _effectLookup.Update(ref state);
            _bindingBulletLookup.Update(ref state);
            _bulletLookup.Update(ref state);
            _summonEntitiesLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _eventPlayAnimation.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            var servantList = SystemAPI.GetBuffer<ServantList>(localPlayer);
            
            foreach (var (tag, servant, entity) in SystemAPI.Query<EnterDieTag, ServantProperties>().WithEntityAccess())
            {
                if (_deadLookup.HasComponent(entity) && _deadLookup.IsComponentEnabled(entity))
                {
                    continue;
                }
                
                //死亡标志
                ecb.SetComponentEnabled<EnterDieTag>(entity, false);
                ecb.SetComponentEnabled<InDeadState>(entity, true);

                //ui event
                ecb.AppendToBuffer(localPlayer, new UIUpdateBuffer
                { 
                    Value = new EventData
                    {
                        Command = EEventCommand.OnServantDead,
                        Param1 = servant.UniqueId,
                    }
                });
                
                if (_eventPlayAnimation.HasComponent(entity))
                {
                    ecb.SetComponent(entity, new HybridEvent_PlayAnimation { Type = EAnimationType.Dead });
                    ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(entity, true);
                }
                 
                CreatureHelper.UnbindServant(servantList, global, entity, _transformLookup, _servantLookup, _mainServantLookup,
                    _childLookup, _effectLookup,  _bindingBulletLookup,_bulletLookup, ecb, 3f);

                //全部死亡了
                if (servantList.Length <= 0)
                {
                    ecb.AppendToBuffer(localPlayer, new UIUpdateBuffer
                    {
                        Value = new EventData
                        {
                            Command = EEventCommand.OnDead,
                        }
                    });
                }
                
                /*//播放音效
                if (config.DieSound > 0)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new PlaySoundBuffer
                    {
                        SoundId = config.DieSound
                    });
                }

                //播放特效
                if (config.DieEffect > 0)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                    {
                        ResourceId = config.DieEffect,
                        Pos = localTransform.Position,
                        Scale = localTransform.Scale,
                    });
                }*/
              
                //延迟10帧进行，否则OnDead触发的技能放不出来
                
                
                //清理所有召唤物
                if (_summonEntitiesLookup.TryGetBuffer(entity, out var summons))
                {
                    foreach (var summonEntity in summons)
                    {
                        if (_transformLookup.HasComponent(summonEntity.Value))
                        {
                            ecb.SetComponent(summonEntity.Value, new EnterDieTag { BanTrigger = true });
                            ecb.SetComponentEnabled<EnterDieTag>(summonEntity.Value, true);
                        }  
                    }
                }
                
                //SkillTrigger
                if (!tag.BanTrigger)
                {
                    SkillHelper.DoSkillTrigger(entity, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnDead), ecb);    
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}