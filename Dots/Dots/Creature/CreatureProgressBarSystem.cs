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
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
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
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
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
            _creatureLookup.Update(ref state);
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
                CreatureLookup = _creatureLookup,
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
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            
            [BurstCompile]
            private void Execute(ProgressBarComponent tag, LocalTransform localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (LocalToWorldLookup.HasComponent(tag.Value) && CreatureLookup.TryGetComponent(entity, out var creature))
                {
                    //同步血量
                    var hp = creature.CurHp / AttrHelper.GetMaxHp(entity, AttrLookup, AttrModifyLookup, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                    Ecb.SetComponent(sortKey, tag.Value, new MaterialFillRate
                    {
                        Value = hp - 0.5f,
                    });

                    //同步位置
                    Ecb.SetComponent(sortKey, tag.Value, new LocalTransform
                    {
                        Position = CreatureHelper.getHeadPos( localTransform.Position, creature, localTransform.Scale),
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