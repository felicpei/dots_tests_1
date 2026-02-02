using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SkillSystemGroup))]
    public partial struct SkillOtherSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _cacheLookup  = state.GetComponentLookup<CacheProperties>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            
            
            //叠加Skill层数
            new SkillLayerChangeJob { Ecb = ecb.AsParallelWriter() }.ScheduleParallel();
            state.Dependency.Complete();

            //append end
            new AppendEndBufferJob
            {
                Ecb = ecb.AsParallelWriter(), 
                SkillEntitiesLookup =  _skillEntitiesLookup,
                SkillTagLookup = _skillTagLookup,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct SkillLayerChangeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;

            [BurstCompile]
            private void Execute(DynamicBuffer<SkillLayerAddBuffer> addLayerBuffer, RefRW<SkillProperties> properties, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                for (var i = 0; i < addLayerBuffer.Length; i++)
                {
                    var layerAdd = addLayerBuffer[i].Value;
                    properties.ValueRW.CurrLayer = properties.ValueRO.CurrLayer + layerAdd;
                    properties.ValueRW.AddedLayer = properties.ValueRO.AddedLayer + layerAdd;
                }

                addLayerBuffer.Clear();

                Ecb.SetComponentEnabled<SkillLayerAddBuffer>(sortKey, entity, false);
            }
        }

        [BurstCompile]
        private partial struct AppendEndBufferJob : IJobEntity
        {
            public Entity CacheEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            
            [BurstCompile]
            private void Execute(DynamicBuffer<SkillEndBuffer> endBuffers, SkillProperties properties, MasterCreature master, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetSkillConfig(properties.Id, CacheEntity, CacheLookup, out var config))
                {
                    endBuffers.Clear();
                    return;
                }
                
                for (var i = endBuffers.Length - 1; i >= 0; i--)
                {
                    var buffer = endBuffers[i];
                    endBuffers.RemoveAt(i);

                    for (var j = 1; j <= 6; j++)
                    {
                        SkillActionConfig actionConfig = default;
                        if (j == 1) actionConfig = config.End1;
                        else if (j == 2) actionConfig = config.End2;
                        else if (j == 3) actionConfig = config.End3;
                        else if (j == 4) actionConfig = config.End4;
                        else if (j == 5) actionConfig = config.End5;
                        else if (j == 6) actionConfig = config.End6; 

                        if (actionConfig.Action == ESkillAction.None)
                        {
                            continue;
                        }
                        
                        if (SkillTagLookup.HasComponent(entity))
                        {
                            Ecb.AppendToBuffer(sortKey, entity, new SkillActionBuffer
                            {
                                Idx = j,
                                Entity = buffer.Entity,
                                StartPos = buffer.StartPos,
                                Pos = buffer.Pos,
                                AtkValue = buffer.AtkValue,
                                CastIndex = buffer.CastIndex,
                                Param1 = buffer.Param1,
                            });
                            Ecb.SetComponentEnabled<SkillActionBuffer>(sortKey, entity, true);
                        }
                    }
                 
                    //SkillTrigger
                    SkillHelper.DoSkillTrigger(master.Value, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.AfterSkillCast)
                    {
                        IntValue1 = config.ClassId,
                        IntValue2 = config.Id,
                        Entity = buffer.Entity,
                        Pos = buffer.Pos,
                    }, Ecb, sortKey);
                }

                Ecb.SetComponentEnabled<SkillEndBuffer>(sortKey, entity, false);
            }
        }
    }
}