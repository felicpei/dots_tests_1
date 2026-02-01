using Dots;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct FactoryDropItemSystem : ISystem
    {
        private const int GOLD_DROPITEM_ID = 6;
        private const int EXP_1 = 11;
        private const int EXP_2 = 12;
        private const int EXP_3 = 13;
        private const int EXP_4 = 14;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            //掉落物品（一帧最大10个）
            var dropCount = 0;
            for (var i = global.DropItemCreateBuffer.Length - 1; i >= 0; i--)
            {
                if (global.DropItemCreateBuffer[i].DropItemId > 0)
                {
                    for (var j = 0; j < global.DropItemCreateBuffer[i].Count; j++)
                    {
                        FactoryHelper.CreateDropItem(global, cache, global.DropItemCreateBuffer[i], ecb, collisionWorld);
                    }

                    dropCount++;
                }

                global.DropItemCreateBuffer.RemoveAt(i);

                if (dropCount > 10)
                {
                    break;
                }
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public static void AutoDropGold(int totalCount, float3 pos, RefRW<RandomSeed> random, Entity globalEntity, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            var randCount = random.ValueRW.Value.NextInt(1, 3);
            var dropCount = Mathf.CeilToInt((float)totalCount / randCount);

            for (var i = 0; i < randCount; i++)
            {
                ecb.AppendToBuffer(sortKey, globalEntity, new DropItemCreateBuffer
                {
                    Pos = pos,
                    DropItemId = GOLD_DROPITEM_ID,
                    Value = dropCount,
                    Count = 1,
                    RandomRange = 1.5f,
                });
            }
        }

        public static void AutoDropMaterial(int totalCount, float3 pos, RefRW<RandomSeed> random, Entity globalEntity, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            var randCount = random.ValueRW.Value.NextInt(1, 4);
            var dropCount = Mathf.CeilToInt((float)totalCount / randCount);

            for (var i = 0; i < randCount; i++)
            {
                if (dropCount <= 5) //1-5
                {
                    ecb.AppendToBuffer(sortKey, globalEntity, new DropItemCreateBuffer
                    {
                        Pos = pos,
                        DropItemId = EXP_1,
                        Value = dropCount,
                        Count = 1,
                        RandomRange = 1.5f,
                    });
                }
                else if (dropCount <= 20) //5-20
                {
                    ecb.AppendToBuffer(sortKey, globalEntity, new DropItemCreateBuffer
                    {
                        Pos = pos,
                        DropItemId = EXP_2,
                        Value = dropCount,
                        Count = 1,
                        RandomRange = 1.5f,
                    });
                }
                else if (dropCount <= 100) // 20-100
                {
                    ecb.AppendToBuffer(sortKey, globalEntity, new DropItemCreateBuffer
                    {
                        Pos = pos,
                        DropItemId = EXP_3,
                        Value = dropCount,
                        Count = 1,
                        RandomRange = 1.5f,
                    });
                }
                else //100+
                {
                    ecb.AppendToBuffer(sortKey, globalEntity, new DropItemCreateBuffer
                    {
                        Pos = pos,
                        DropItemId = EXP_4,
                        Value = dropCount,
                        Count = 1,
                        RandomRange = 1.5f,
                    });
                }
            }
        }
    }
}