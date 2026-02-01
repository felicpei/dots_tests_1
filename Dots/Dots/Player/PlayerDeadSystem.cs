using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    public partial struct PlayerDeadSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_SetActive> _eventSetActive;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _eventSetActive = state.GetComponentLookup<HybridEvent_SetActive>(true);
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
            _deadLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _eventSetActive.Update(ref state);

            foreach (var (tag, localTransform, creature, entity) in 
                     SystemAPI.Query<EnterDieTag, LocalTransform, RefRW<CreatureProperties>>().WithAll<LocalPlayerTag>().WithEntityAccess())
            {
                if (_deadLookup.HasComponent(entity) && _deadLookup.IsComponentEnabled(entity))
                {
                    continue;
                }
                
                ecb.SetComponentEnabled<EnterDieTag>(entity, false);

                //死亡标志
                ecb.SetComponentEnabled<InDeadTag>(entity, true);

                //LocalPlayer 更新missionUI
                ecb.AppendToBuffer( entity, new UIUpdateBuffer
                {
                    Value = new EventData
                    {
                        Command = EEventCommand.OnDead,
                    }
                });

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

                //隐藏角色模型
                if (_eventSetActive.HasComponent(entity))
                {
                    ecb.SetComponent(entity, new HybridEvent_SetActive { Value = false});
                    ecb.SetComponentEnabled<HybridEvent_SetActive>(entity, true);
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