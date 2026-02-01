using Deploys;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    public partial struct HpRecoverySystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<DisableHurtTag> _disableHurtLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _disableHurtLookup = state.GetComponentLookup<DisableHurtTag>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
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
            
            _deadLookup.Update(ref state);
            _disableHurtLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //拾取技能
            new HpRecoveryJob
            {
                Ecb = ecb.AsParallelWriter(),
                DeltaTime = deltaTime,
                DeadLookup = _deadLookup,
                DisableHurtLookup = _disableHurtLookup,
                CreatureLookup = _creatureLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct HpRecoveryJob : IJobEntity
        {
            public float DeltaTime;
            public Entity CacheEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public ComponentLookup<DisableHurtTag> DisableHurtLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            
            [BurstCompile]
            private void Execute(RefRW<StatusHpRecovery> hpRecovery, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (DeadLookup.HasComponent(entity) && DeadLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                //无敌状态下不回血
                if (DisableHurtLookup.HasComponent(entity) && DisableHurtLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                var attrRecovery = AttrHelper.GetAttr(entity, EAttr.Recovery, AttrLookup, AttrModifyLookup, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                if (attrRecovery > 0)
                {
                    var interval = AttrHelper.GetHpRecoveryInterval(attrRecovery);
                    hpRecovery.ValueRW.Timer += DeltaTime;
                    
                    if (hpRecovery.ValueRO.Timer >= interval)
                    {
                        hpRecovery.ValueRW.Timer = 0;
                        Ecb.AppendToBuffer(sortKey, entity, new CreatureDataProcess
                        {
                            Type = ECreatureDataProcess.Cure,
                            AddPercent = AttrHelper.GetHpRecoveryPercent(),
                        });
                        Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, entity, true);
                    }
                }
            }
        }
    }
}