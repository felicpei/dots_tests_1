using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MapSystemGroup))]
    public partial struct MapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapProperties>();
            state.RequireForUpdate<MapGeneratedChunk>();
            state.RequireForUpdate<GlobalInitialized>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            //Init

            foreach (var (map, mapChunk, entity) 
                     in SystemAPI.Query<RefRW<MapProperties>, DynamicBuffer<MapGeneratedChunk>>().WithEntityAccess())
            {
                if (!map.ValueRO.Inited)
                {
                    map.ValueRW.Inited = true;
                    var chunkIndex = MapHelper.GetChunkIndex(global.PlayerBornPos, map.ValueRO.Scale);
                    MapHelper.GenerateRangeChunk(global, chunkIndex, map, mapChunk, ecb);
                    
                    //start event
                    ecb.AddComponent<MissionStartTag>(entity);
                    continue;
                }
                
                if (SystemAPI.HasSingleton<LocalPlayerTag>())
                {
                    var playerTransform = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<LocalPlayerTag>());

                    //检查玩家坐标，发现附近 +-1 的Chunk有没有生成的，则动态生成
                    var chunkIndex = MapHelper.GetChunkIndex(playerTransform.Position, map.ValueRO.Scale);

                    if (math.abs(map.ValueRO.CurChunkIndex.x - chunkIndex.x) < 0.1f && math.abs(map.ValueRO.CurChunkIndex.y - chunkIndex.y) < 0.1f)
                    {
                        //same chunk do nothing
                    }
                    else
                    {
                        //检测附近的每次生成25个区块
                        map.ValueRW.CurChunkIndex = chunkIndex;
                        MapHelper.GenerateRangeChunk(global, chunkIndex, map, mapChunk, ecb);
                    }
                }
            }
          

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}