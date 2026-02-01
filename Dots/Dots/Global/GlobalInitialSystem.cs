using Lobby;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Dots
{
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct GlobalInitialSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitTag>();
            state.RequireForUpdate<CacheInitialed>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            //在战斗中
            if (SystemAPI.HasSingleton<GlobalInitTag>())
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                var entity = SystemAPI.GetSingletonEntity<GlobalInitTag>();
                ecb.AddComponent<GlobalInitialized>(entity);

                //remove tag
                ecb.RemoveComponent<GlobalInitTag>(entity);

                var deltaTime = SystemAPI.Time.DeltaTime;
                var missionDeploy = Table.GetMission(FightData.MissionId);

                //设置物理帧50
                state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().RateManager.Timestep = 0.02f;

                //随机种子
                ecb.AddComponent(entity, new RandomSeed { Value = Random.CreateFromIndex((uint)(deltaTime * 10000000)) });

                //input system
                ecb.AddComponent<InputProperties>(entity);

                //全局数据
                InitGlobalData(missionDeploy, ecb, entity);

                //先InitMap, MapInit完成后在发MissionStart
                var sceneDeploy = missionDeploy.GetSceneDeploy();
                ecb.AddComponent(entity, new MapProperties
                {
                    MapResId = sceneDeploy.MapResId,
                    Scale = sceneDeploy.Scale,
                });
                ecb.AddBuffer<MapGeneratedChunk>(entity);
                
                CameraController.Init(missionDeploy.Id);
                
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }

        private static void InitGlobalData(Deploys.Mission missionDeploy, EntityCommandBuffer ecb, Entity entity)
        {
            var spawnTab = missionDeploy.GetSpawnTimelineList();
            
            //全局数据
            var globalData = new GlobalData
            {
                MissionId = missionDeploy.Id,
                LocalPlayerId = FightData.LocalPlayerId,
                ShowDamageNumber = !Setting.BanHurtNumber,
                
                //关卡参数
                PlayerProps = FightData.PlayerProps,
                MonsterProps = FightData.MonsterProps,
                
                //wave
                WaveId = FightData.Wave <= 0 ? 1 : FightData.Wave,
                WaveTotal = spawnTab.Count,
            };

            ecb.AddComponent(entity, globalData);

            //时停
            ecb.AddComponent<GlobalMonsterPauseData>(entity);

            //boss信息
            ecb.AddComponent<GlobalBossInfo>(entity);
        }
    }
}