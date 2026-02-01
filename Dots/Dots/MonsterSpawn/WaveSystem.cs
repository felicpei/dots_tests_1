using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSpawnSystemGroup))]
    public partial struct WaveSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<MissionWaveInited>();
            state.RequireForUpdate<LocalPlayerTag>();

            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _summonLookup = state.GetBufferLookup<SummonEntities>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _creatureLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _localToWorldLookup.Update(ref state);

            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var deltaTime = SystemAPI.Time.DeltaTime;
            global.WaveCurTime += deltaTime;

            var isLastWave = global.WaveId == global.WaveTotal;

            var waveTimerOver = global.WaveCurTime >= global.WaveTotalTime;
            if (waveTimerOver || isLastWave)
            {
                var totalCount = 0;
                foreach (var spawn in SystemAPI.Query<SpawnMonsterProperties>())
                {
                    totalCount += spawn.AliveCount;
                }
                
                var bWaveEnd = totalCount <= 0;
                var delayDestroySec = 1f;
                
                if (bWaveEnd)
                {
                    ecb.AddComponent(global.Entity, new ClearMonsterTag { ContainBoss = true, BanDrop = true, Delay = delayDestroySec });

                    //拾取所有掉落物品(仅EndFly)
                    foreach (var (idle, entity) in SystemAPI.Query<DropItemIdleTag>().WithEntityAccess().WithNone<DropItemFlyTag>())
                    {
                        if (idle.EndFly)
                        {
                            ecb.SetComponentEnabled<DropItemIdleTag>(entity, false);
                            ecb.SetComponent(entity, new DropItemFlyTag { Speed = 10f, TimeSpent = 0, });
                            ecb.SetComponentEnabled<DropItemFlyTag>(entity, true);
                        }
                    }

                    //ui event
                    ecb.AppendToBuffer(localPlayer, new UIUpdateBuffer
                    {
                        Value = new EventData
                        {
                            Command = EEventCommand.WaveEnd,
                            Param1 = global.WaveId,
                        }
                    });

                    //local player skill trigger
                    /*SkillHelper.DoSkillTrigger(localPlayer, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnWaveEnd)
                    {
                        IntValue1 = global.WaveId
                    }, ecb);*/

                    global.InWave = false;
                    ecb.RemoveComponent<MissionWaveInited>(global.Entity);
                }
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}