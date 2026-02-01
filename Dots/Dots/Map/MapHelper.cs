using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    public class MapHelper
    {
        public static float2 GetChunkIndex(float3 pos, float scale)
        {
            //0,0点chunk为0, chunk范围为 +- chunkSize
            var chunkX = Mathf.FloorToInt((pos.x + scale / 2f) / scale);
            var chunkY = Mathf.FloorToInt((pos.y + scale / 2f) / scale);
            return new float2(chunkX, chunkY);
        }

        
        public static void GenerateRangeChunk(GlobalAspect global, float2 chunkIndex, RefRW<MapProperties> mapInfo, DynamicBuffer<MapGeneratedChunk> generatedChunks, EntityCommandBuffer ecb)
        {
            var chunkGenCount = (int)(5f / mapInfo.ValueRO.Scale * 15);
            for (var i = chunkIndex.x - chunkGenCount; i <= chunkIndex.x + chunkGenCount; i++)
            {
                for (var j = chunkIndex.y - chunkGenCount; j <= chunkIndex.y + chunkGenCount; j++)
                {
                    var wantGeneChunkId = new float2(i, j);
                    if (!CheckGenerated(wantGeneChunkId, generatedChunks))
                    {
                        GenerateChunk(global, mapInfo, wantGeneChunkId, ecb, generatedChunks);
                    }
                }
            }
        }
        
        private static void GenerateChunk(GlobalAspect global, RefRW<MapProperties> mapInfo, float2 chunkIndex, EntityCommandBuffer ecb, DynamicBuffer<MapGeneratedChunk> generatedChunks)
        {
            var entity = ecb.Instantiate(global.Prefabs.MapGround);

            var pos = new float3(chunkIndex.x * mapInfo.ValueRO.Scale, 0, chunkIndex.y * mapInfo.ValueRO.Scale);
            
            ecb.AddComponent(entity, new MapTag
            {
                Idx = chunkIndex,
            });
            
            ecb.AddComponent(entity, new LocalTransform
            {
                Scale = mapInfo.ValueRO.Scale,
                Rotation = quaternion.RotateY(math.radians(180f)),
                Position = pos
            });

            ecb.AddComponent(entity, new HybridLinkTag
            {
                Type = EHybridType.Map,
                ResId = mapInfo.ValueRO.MapResId,
                Scale = mapInfo.ValueRO.Scale,
                InitPos = pos,
            });
            //Debug.LogError($"pos:{pos}   scale:{mapInfo.ValueRO.Scale}");
            generatedChunks.Add(new MapGeneratedChunk { Value = chunkIndex });
        }

        
        public static bool CheckGenerated(float2 chunkIndex, DynamicBuffer<MapGeneratedChunk> generatedChunks)
        {
            foreach (var idx in generatedChunks)
            {
                if (math.abs(idx.Value.x - chunkIndex.x) < 0.1f && math.abs(idx.Value.y - chunkIndex.y) < 0.1f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}