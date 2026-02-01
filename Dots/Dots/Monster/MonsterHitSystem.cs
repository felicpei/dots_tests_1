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
    [UpdateAfter(typeof(MonsterHatredSystem))]
    public partial struct MonsterHitSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<StatusColor> _colorLookup;
        [ReadOnly] private ComponentLookup<MaterialBlend> _blendLookup;
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
            state.RequireForUpdate<CacheProperties>();
                
            _blendLookup = state.GetComponentLookup<MaterialBlend>(true);
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _colorLookup = state.GetComponentLookup<StatusColor>(true);
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

            _deadLookup.Update(ref state);
            _colorLookup.Update(ref state);
            _blendLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            //被击状态处理
            new MonsterHitJob
            {
                Ecb = ecb.AsParallelWriter(),
                GlobalEntity = global.Entity,
                DeadLookup = _deadLookup,
                ColorLookup = _colorLookup,
                BlendLookup = _blendLookup,
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
        private partial struct MonsterHitJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity GlobalEntity;
           
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public ComponentLookup<StatusColor> ColorLookup;
            [ReadOnly] public ComponentLookup<MaterialBlend> BlendLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            
            [BurstCompile]
            private void Execute(EnterHitTag _, RefRW<MonsterProperties> monster, MonsterTarget target, LocalTransform localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                Ecb.SetComponentEnabled<EnterHitTag>(sortKey, entity, false);

                if (DeadLookup.HasComponent(entity) && DeadLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                if (!CreatureLookup.TryGetComponent(entity, out var creature))
                {
                    return;
                }
                
                //如果是boss, 更新全局boss信息
                if (creature.Type == ECreatureType.Boss)
                {
                    Ecb.SetComponent(sortKey, GlobalEntity, new GlobalBossInfo
                    {
                        MonsterId = monster.ValueRO.Id,
                        HpPercent = creature.CurHp / AttrHelper.GetMaxHp(entity, AttrLookup, AttrModifyLookup, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup)
                    });
                }

                //闪红
                var inBuffColor = ColorLookup.TryGetComponent(entity, out var color) && color.InBuffColor;
                if (!inBuffColor)
                {
                    if (BlendLookup.HasComponent(entity))
                    {
                        Ecb.SetComponent(sortKey, entity, new MaterialBlend
                        {
                            Color = new float4(1,0,0,1) ,
                            Value = 1F,
                        });
                    }
                }
            }
        }
    }
}