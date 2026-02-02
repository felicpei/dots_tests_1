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
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureScaleSyncSystem : ISystem
    {
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<CreatureProps> _propsLookup;
        [ReadOnly] private ComponentLookup<CreatureRepelPosition> _repelLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _propsLookup = state.GetComponentLookup<CreatureProps>(true);
            _repelLookup = state.GetComponentLookup<CreatureRepelPosition>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _repelLookup.Update(ref state);
            _propsLookup.Update(ref state);
              
            new ScaleSyncJob
            {
                SummonLookup = _summonLookup,
                PropsLookup = _propsLookup,
                BuffCommonLookup = _buffCommonLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                RepelLookup = _repelLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();
        }

        [BurstCompile]
        private partial struct ScaleSyncJob : IJobEntity
        {
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<CreatureProps> PropsLookup;
            [ReadOnly] public ComponentLookup<CreatureRepelPosition> RepelLookup;
            
            [BurstCompile]
            private void Execute(CreatureTag tag, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!PropsLookup.TryGetComponent(entity, out var creature))
                {
                    return;
                }
                
                //scale
                var addFactor = BuffHelper.GetBuffAddFactor(entity, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.CreatureScale);
                var newScale = BuffHelper.CalcFactor(creature.OriginScale, addFactor);
                    
                //击飞的scale
                if (RepelLookup.TryGetComponent(entity, out var repelInfo) && RepelLookup.IsComponentEnabled(entity))
                {
                    newScale += repelInfo.ExtraScale;
                }
                    
                if (math.abs(localTransform.ValueRO.Scale - newScale) > 0.01f)
                {
                    localTransform.ValueRW.Scale = newScale;
                }
            }
        }
    }
}