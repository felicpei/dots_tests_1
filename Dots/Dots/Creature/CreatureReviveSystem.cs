using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureReviveSystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_SetActive> _eventSetActive;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _eventSetActive = state.GetComponentLookup<HybridEvent_SetActive>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
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

            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _eventSetActive.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //复活状态
            new ReviveJob
            { 
                DeltaTime = deltaTime,
                Ecb = ecb.AsParallelWriter(), 
                SkillEntitiesLookup = _skillEntitiesLookup,
                SkillTagLookup = _skillTagLookup,
                EventSetActive = _eventSetActive,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
                CreatureLookup = _creatureLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct ReviveJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<HybridEvent_SetActive> EventSetActive;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            
            [BurstCompile]
            private void Execute(RefRW<EnterReviveTag> tag, InDeadTag inDeadTag,  Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (tag.ValueRO.Delay > 0)
                {
                    if (tag.ValueRO.Timer < tag.ValueRO.Delay)
                    {
                        tag.ValueRW.Timer += DeltaTime;
                        return;
                    }
                }
                
                Ecb.SetComponentEnabled<EnterReviveTag>(sortKey, entity, false);
                Ecb.SetComponentEnabled<InDeadTag>(sortKey, entity, false);

                Ecb.AppendToBuffer(sortKey, entity, new CreatureDataProcess
                {
                    Type = ECreatureDataProcess.ResetHp,
                });
                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, entity, true);

                //加个短暂免伤防错
                Ecb.SetComponent(sortKey, entity, new DisableHurtTag(contTime: 0.3f, timer: 0));
                Ecb.SetComponentEnabled<DisableHurtTag>(sortKey, entity, true);

                //SkillTrigger
                SkillHelper.DoSkillTrigger(entity, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnRevive), Ecb, sortKey);

                //显示角色模型
                if (EventSetActive.HasComponent(entity))
                {
                    Ecb.SetComponent(sortKey, entity, new HybridEvent_SetActive {  Value = true });
                    Ecb.SetComponentEnabled<HybridEvent_SetActive>(sortKey, entity, true);
                }
            }
        }
    }
}