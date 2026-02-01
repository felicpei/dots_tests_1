using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct MonsterDieEventSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
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
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            
            //怪物Die Event, 扣除数量
            for (var i = global.MonsterDieEvent.Length - 1; i >= 0; i--)
            {
                var eventInfo = global.MonsterDieEvent[i];

                //减boss数量
                if (eventInfo.Type == ECreatureType.Boss)
                {
                    //当前boss死亡
                    if (eventInfo.MonsterId == global.CurrBossId)
                    {
                        ecb.SetComponent(global.Entity, new GlobalBossInfo
                        {
                            MonsterId = 0,
                            HpPercent = 0,
                        });
                    }
                }

                //销毁刷怪点
                foreach (var (spawn, entity) in SystemAPI.Query<RefRW<SpawnMonsterProperties>>().WithEntityAccess())
                {
                    //刷怪点数量修改
                    if (spawn.ValueRO.Id != eventInfo.SpawnPointId)
                    {
                        continue;
                    }

                    spawn.ValueRW.AliveCount = spawn.ValueRO.AliveCount - 1;

                    //刷完且无怪物存活的销毁
                    if (spawn.ValueRO.AliveCount <= 0 && spawn.ValueRO.RefreshFinished)
                    {
                        ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                    }
                    break;
                }

                global.MonsterDieEvent.RemoveAt(i);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}