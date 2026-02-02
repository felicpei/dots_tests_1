using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsSystem
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureProgressBarSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<StatusHp> _hpLookup;
        [ReadOnly] private ComponentLookup<StatusCenter> _centerLookup;
        
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _hpLookup = state.GetComponentLookup<StatusHp>(true);
            _centerLookup = state.GetComponentLookup<StatusCenter>(true);
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
            
            _localToWorldLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _hpLookup.Update(ref state);
            _centerLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            //同步血条位置
            new HudSyncJob
            {
                GlobalEntity = global.Entity,
                Ecb = ecb.AsParallelWriter(), 
                LocalToWorldLookup = _localToWorldLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
                SummonLookup = _summonLookup,
                HpLookup = _hpLookup,
                CenterLookup = _centerLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();
 

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct HudSyncJob : IJobEntity
        {
            public Entity GlobalEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<StatusHp> HpLookup;
            [ReadOnly] public ComponentLookup<StatusCenter> CenterLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            
            [BurstCompile]
            private void Execute(ProgressBarComponent tag, LocalTransform localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (LocalToWorldLookup.HasComponent(tag.Value) 
                    && HpLookup.TryGetComponent(entity, out var creature))
                {
                    //同步血量
                    var hp = creature.CurHp / AttrHelper.GetMaxHp(entity, AttrLookup, AttrModifyLookup, HpLookup, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                    Ecb.SetComponent(sortKey, tag.Value, new MaterialFillRate
                    {
                        Value = hp - 0.5f,
                    });

                    //同步位置
                    var pos = localTransform.Position;
                    if (CenterLookup.TryGetComponent(entity, out var center))
                    {
                        pos = CreatureHelper.GetHeadPos(localTransform.Position, center, localTransform.Scale);
                    }
                    
                    Ecb.SetComponent(sortKey, tag.Value, new LocalTransform
                    {
                        Position = pos,
                        Rotation = quaternion.identity,
                        Scale = localTransform.Scale,
                    });
                }
                else
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });
                }
            }
        }
    }
}