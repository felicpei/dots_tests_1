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
    [UpdateInGroup(typeof(BulletSystemGroup))]
    public partial struct BulletAddScaleSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        [ReadOnly] private ComponentLookup<BulletTriggerData> _triggerDataLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>(true);
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _triggerDataLookup = state.GetComponentLookup<BulletTriggerData>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
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

            _destroyLookup.Update(ref state);
            _creatureTag.Update(ref state);
            _triggerDataLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

            //检查MasterCreature是否还存在
            new BulletAddScaleJob
            {
                Ecb = ecb.AsParallelWriter(),
                DestroyLookup = _destroyLookup,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();
            state.Dependency.Complete();


            new BulletScaleJob
            {
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                SummonLookup = _summonLookup,
                BuffCommonLookup = _buffCommonLookup,
                BuffTagLookup = _buffTagLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                TriggerDataLookup = _triggerDataLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct BulletAddScaleJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<BulletDestroyTag> DestroyLookup;

            [BurstCompile]
            private void Execute(RefRW<BulletAddScale> tag, RefRW<BulletTriggerData> triggerData, RefRW<BulletProperties> properties, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (DestroyLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                if (tag.ValueRO.Curr >= tag.ValueRO.Max)
                {
                    Ecb.SetComponentEnabled<BulletAddScale>(sortKey, entity, false);
                    return;
                }

                var addScale = DeltaTime * tag.ValueRO.Speed;
                tag.ValueRW.Curr += addScale;

                if (properties.ValueRO.BombRadius > 0)
                {
                    properties.ValueRW.BombRadius += addScale;    
                }

                var targetScale = triggerData.ValueRO.ScaleFactor + addScale;
                triggerData.ValueRW.ScaleFactor = targetScale;
            }
        }

        [BurstCompile]
        private partial struct BulletScaleJob : IJobEntity
        {
            public Entity CacheEntity;
            [ReadOnly] public ComponentLookup<BulletTriggerData> TriggerDataLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;

            [BurstCompile]
            private void Execute(RefRW<BulletProperties> properties, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetBulletConfig(properties.ValueRO.BulletId, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                var addFactor = AttrHelper.GetDamageRangeFactor(properties.ValueRO.MasterCreature, SummonLookup, AttrLookup, AttrModifyLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, true);
                addFactor += BuffHelper.GetBuffAddFactor(properties.ValueRO.MasterCreature, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.BulletScale, config.Id, config.ClassId);
               
                var newScale = BuffHelper.CalcFactor(properties.ValueRO.SourceScale, addFactor);
                if (TriggerDataLookup.TryGetComponent(entity, out var triggerData))
                {
                    newScale = BuffHelper.CalcFactor(newScale, triggerData.ScaleFactor);
                }

                if (math.abs(localTransform.ValueRO.Scale - newScale) > 0.01f)
                {
                    localTransform.ValueRW.Scale = newScale;
                }
            }
        }
    }
}