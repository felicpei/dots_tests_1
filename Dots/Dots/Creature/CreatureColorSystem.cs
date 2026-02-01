using Dots;
using Dots;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureColorSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InFreezeTag> _inFreezeLookup;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _inFreezeLookup = state.GetComponentLookup<InFreezeTag>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
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

            _inFreezeLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            
            //闪红恢复
            new ColorUpdateJob
            {
                CacheLookup = _cacheLookup,
                CacheEntity = cacheEntity,
                DeltaTime = deltaTime,
                DeadLookup = _deadLookup,
                InFreezeLookup = _inFreezeLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();
        }


        [BurstCompile]
        private partial struct ColorUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public Entity CacheEntity;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public ComponentLookup<InFreezeTag> InFreezeLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
  
            [BurstCompile]
            private void Execute(RefRW<StatusColor> statusColor, RefRW<MaterialBlend> blend, Entity entity, [EntityIndexInQuery] int sortKey)           
            {
                if (ColorHelper.GetCreatureColor(entity, BuffEntitiesLookup, BuffTagLookup, CacheEntity, CacheLookup, out var alpha, out var buffColor))
                {
                    statusColor.ValueRW.Color = new float4(buffColor.r, buffColor.g, buffColor.b, 1f);
                    statusColor.ValueRW.Alpha = alpha;
                    statusColor.ValueRW.InBuffColor = true;
                }
                else
                {
                    statusColor.ValueRW.Color = new float4(1, 1, 1, 1);
                    statusColor.ValueRW.Alpha = 0f;
                    statusColor.ValueRW.InBuffColor = false;
                }

                var speed = 2f;
                if (math.abs(blend.ValueRO.Color.x - statusColor.ValueRO.Color.x) > 0.001f ||
                    math.abs(blend.ValueRO.Color.y - statusColor.ValueRO.Color.y) > 0.001f ||
                    math.abs(blend.ValueRO.Color.z - statusColor.ValueRO.Color.z) > 0.001f ||
                    math.abs(blend.ValueRO.Color.w - statusColor.ValueRO.Color.w) > 0.001f)
                {
                    var color = math.lerp(blend.ValueRO.Color, statusColor.ValueRO.Color, DeltaTime * speed);
                    blend.ValueRW.Color = color;
                }   

                if (math.abs(blend.ValueRO.Value - statusColor.ValueRO.Alpha) > 0.01f)
                {
                    float opacity;
                    if (blend.ValueRO.Value > statusColor.ValueRO.Alpha)
                    {
                        opacity = blend.ValueRO.Value - DeltaTime * speed;
                        if (opacity < statusColor.ValueRO.Alpha)
                        {
                            opacity = statusColor.ValueRO.Alpha;
                        }
                    }
                    else
                    {
                        opacity = blend.ValueRO.Value + DeltaTime * speed;
                        if (opacity > statusColor.ValueRO.Alpha)
                        {
                            opacity = statusColor.ValueRO.Alpha;
                        }
                    }

                    blend.ValueRW.Value = opacity;
                }
            }
        }
    }
}