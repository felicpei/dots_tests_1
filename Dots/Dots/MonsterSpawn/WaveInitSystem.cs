using System.Collections.Generic;
using System.Linq;
using Deploys;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    [UpdateInGroup(typeof(MonsterSpawnSystemGroup))]
    public partial struct WaveInitSystem : ISystem
    {
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();

            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SpawnWaveInitTag>())
            {
                return;
            }

            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);

            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();

            var missionDeploy = Table.GetMission(global.MissionId);
            var poolDeploy = Table.GetSpawnPool(global.MissionId);
            if (poolDeploy == null)
            {
                Debug.LogError($"SpawnMonster/Pool.tab中找不到关卡ID:{global.MissionId}, 没配置?");
                return;
            }

            //计算总掉落经验和金币
            var waveId = global.WaveId;
            if (waveId <= 0)
            {
                waveId = 1;
            }

            var currSpawn = missionDeploy.GetSpawnTimelineWave(waveId);
            if (currSpawn == null)
            {
                Debug.LogError($"SpawnMonster error, 找不到对应的waveId:{waveId}, missionId:{global.MissionId} tabName:" + missionDeploy.SpawnTimeline);
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.RemoveComponent<SpawnWaveInitTag>(global.Entity);

            var startLevel = 0;
            var startGold = 0f;
            var endLevel = currSpawn.Level;
            var endGold = currSpawn.Gold;

            if (waveId > 1)
            {
                var prevSpawn = missionDeploy.GetSpawnTimelineWave(waveId - 1);
                if (prevSpawn != null)
                {
                    startLevel = prevSpawn.Level;
                    startGold = prevSpawn.Gold;
                }
            }

            //计算本wave掉落经验
            var totalDropExpCount = 0;
            for (var i = startLevel + 1; i <= endLevel; i++)
            {
                var needExp = Table.GetBattleLevel(i).NeedExp;
                totalDropExpCount += needExp;
            }

            //计算本wave掉落金币
            var totalDropGoldCount = 0;
            if (endGold > startGold && FightData.TotalDropGold > 0)
            {
                var percent = endGold - startGold;
                totalDropGoldCount = (int)(FightData.TotalDropGold * percent);
            }

            var idSeq = global.WaveId * 100;
            var totalMonsterCount = 0;

            global.WaveCurTime = 0;
            global.WaveTotalTime = currSpawn.Sec;

            //普通小怪 材料,金币只给小怪掉落
            var normalPool = GetNormalPool(global, currSpawn, missionDeploy.Id);
            var totalSec = currSpawn.Sec;

            var extraCount = BuffHelper.GetBuffAddFactor(localPlayer, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, EBuffType.MissionBuff_MonsterCount, 1);
            var multiple = (int)(1 + extraCount);

            //attr monsterCount
            var attrEnemyCountFac = AttrHelper.GetAttr(localPlayer, EAttr.EnemyCount, _attrLookup, _attrModifyLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
            var minFac = AttrHelper.GetMin(EAttr.EnemyCount);
            if (attrEnemyCountFac < minFac)
            {
                attrEnemyCountFac = minFac;
            }

            for (var i = 0; i < multiple; i++)
            {
                var count1 = Mathf.CeilToInt(BuffHelper.CalcFactor(currSpawn.Count1, attrEnemyCountFac));
                if (count1 > 0 && currSpawn.MonsterIdIndex1.Count > 0)
                {
                    totalMonsterCount += count1;
                    CreateNormalMonster(global, 0, totalSec, count1, currSpawn.MonsterIdIndex1, normalPool, currSpawn, ecb, ref idSeq);
                }

                var count2 = Mathf.CeilToInt(BuffHelper.CalcFactor(currSpawn.Count2, attrEnemyCountFac));
                if (count2 > 0 && currSpawn.MonsterIdIndex2.Count > 0)
                {
                    totalMonsterCount += count2;
                    CreateNormalMonster(global, 0, totalSec, count2, currSpawn.MonsterIdIndex2, normalPool, currSpawn, ecb, ref idSeq);
                }
            }

            //模板怪, 精英, boss, etc
            for (var i = 0; currSpawn.DefaultTemplate != null && i < currSpawn.DefaultTemplate.Count; i++)
            {
                var templateId = currSpawn.DefaultTemplate[i];
                DoTemplate(global, cache, ecb, templateId, currSpawn.Sec, currSpawn.Hp, currSpawn.Speed, poolDeploy, localPlayer, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup, ref idSeq, out var templateMonsterCount);
                totalMonsterCount += templateMonsterCount;
            }

            if (totalMonsterCount > 0)
            {
                global.WavePerGold = totalDropGoldCount / totalMonsterCount;
                global.WavePerGoldProb = 0.5f;
                global.WavePerMaterial = totalDropExpCount / (float)totalMonsterCount;
            }
            else
            {
                global.WavePerGold = 0;
                global.WavePerMaterial = 0;
            }

            global.NoDropMaterialCount = 0;
            global.InWave = true;

            //通知UI waveStart 
            ecb.AppendToBuffer(localPlayer, new UIUpdateBuffer
            {
                Value = new EventData
                {
                    Command = EEventCommand.WaveStart,
                    Param1 = waveId,
                    Param2 = global.WaveTotalTime
                }
            });

            FightData.IsReconnect = false;
            ecb.AddComponent<MissionWaveInited>(global.Entity);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static float CalcSpawnTotalSec(GlobalAspect global, float waveTime, float end)
        {
            var totalSec = waveTime - end;
            return totalSec;
        }

        private static void CreateNormalMonster(GlobalAspect global, float startSec, float totalSec, int count, List<int> monsterIndex,
            List<int> monsterIdList, SpawnTimeline deploy, EntityCommandBuffer ecb, ref int idSeq)
        {
            var atk = 1f;
            var spawnProperties = CreateSpawnPoint(global, ECreatureType.Small, ESpawnMode.Linear, ecb, ref idSeq, startSec, monsterIndex, monsterIdList,
                deploy.Hp, atk, deploy.Speed, out var spawnEntity);

            var shape = GetShape(out var width, out var height);

            spawnProperties.LinearData = new SpawnLinear
            {
                TotalSec = totalSec,
                StartCount = 1,
                EndCount = 1,
                WaveCount = count - 1,
                Shape = shape,
                Width = width,
                Height = height,
                RandomRange = 1f,
            };

            //random point 模式
            spawnProperties.TimelineId = deploy.Id;
            ecb.AddComponent(spawnEntity, spawnProperties);
        }

        private static void CreateNotice(GlobalAspect global, EntityCommandBuffer ecb, float sec, EBattleNotice noticeId)
        {
            var spawnEntity = ecb.Instantiate(global.Empty);
            var spawnProperties = new SpawnMonsterProperties
            {
                Mode = ESpawnMode.UINotice,
                StartTime = sec,
                NoticeId = noticeId
            };
            ecb.AddComponent(spawnEntity, spawnProperties);
        }

        public static List<int> GetNormalPool(GlobalAspect global, SpawnTimeline deploy, int missionId)
        {
            //随机小怪需要特殊处理下
            var poolDeploy = Table.GetSpawnPool(global.MissionId);
            var normalPool = poolDeploy.GetSpawnPoolInfo(ESpawnPool.Normal);
            return normalPool;
        }

        private static void DoTemplate(GlobalAspect global, CacheAspect cache, EntityCommandBuffer ecb,
            int templateId, float totalSec, float baseHp, float speed, SpawnPool poolDeploy,
            Entity localPlayer, ComponentLookup<CreatureProperties> creatureLookup, BufferLookup<BuffEntities> buffEntitiesLookup,
            ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            ref int idSeq, out int totalMonsterCount)
        {
            totalMonsterCount = 0;

            var templateDeploy = Table.GetSpawnTemplate(templateId);
            List<int> monsterPool = null;
            if (templateDeploy.PoolName != ESpawnPool.None)
            {
                monsterPool = poolDeploy.GetSpawnPoolInfo(templateDeploy.PoolName);
                if (monsterPool == null)
                {
                    Debug.LogError("SpawnMonsterPoolDeploy get column error, pool name:" + templateDeploy.PoolName);
                    return;
                }
            }

            var waveCount = templateDeploy.Wave;
            var moveConfig = new MonsterMoveConfig
            {
                MoveMode = templateDeploy.MoveMode,
                MoveParam1 = templateDeploy.MoveParam1,
                MoveParam2 = templateDeploy.MoveParam2,
                MoveParam3 = templateDeploy.MoveParam3,
                MoveParam4 = templateDeploy.MoveParam4,
                MoveParam5 = templateDeploy.MoveParam5,
                MoveParam6 = templateDeploy.MoveParam6,
            };

            if (waveCount <= 0)
            {
                waveCount = 1;
            }

            var startSec = 0f;
            for (var wave = 0; wave < waveCount; wave++)
            {
                var delaySec = startSec + wave * templateDeploy.Interval;

                switch (templateDeploy.API)
                {
                    case ESpawnTemplate.Normal:
                    {
                        //血量权值
                        var hp = baseHp * templateDeploy.HpFac;

                        //计算每波的起始时间
                        var spawnProperties = CreateSpawnPoint(global, ECreatureType.Small, ESpawnMode.RandomPoint, ecb, ref idSeq, delaySec, null, monsterPool,
                            hp, 1f, 1f, out var spawnEntity);

                        //random point 模式
                        spawnProperties.PointData = CreateRandomPointData();
                        spawnProperties.MoveConfig = moveConfig;
                        ecb.AddComponent(spawnEntity, spawnProperties);

                        totalMonsterCount += 1;
                        break;
                    }
                    case ESpawnTemplate.Elite:
                    {
                        //血量权值
                        var hp = baseHp * templateDeploy.HpFac;
                        var extraCount = BuffHelper.GetBuffAddFactor(localPlayer, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.MissionBuff_MonsterCount, 2);
                        var eliteCount = (int)(1 + extraCount);

                        //将小怪自动变为精英
                        var eliteList = new List<int>();
                        for (var i = 0; i < monsterPool.Count; i++)
                        {
                            var normalMonsterId = monsterPool[i];
                            if (cache.GetMonsterConfig(normalMonsterId, out var monsterConfig))
                            {
                                if (monsterConfig.EliteId > 0)
                                {
                                    eliteList.Add(monsterConfig.EliteId);
                                }
                            }
                        }

                        if (eliteList.Count > 0)
                        {
                            //处理精英变boss的buff
                            var bChangeToBoss = BuffHelper.GetHasBuff(localPlayer, creatureLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.MissionBuff_EliteToBoss);
                            var monsterIds = bChangeToBoss ? poolDeploy.GetSpawnPoolAllBossId() : eliteList.ToList();

                            for (var i = 0; i < eliteCount; i++)
                            {
                                //看是否需要变成boss
                                var creatureType = bChangeToBoss ? ECreatureType.Boss : ECreatureType.Elite;
                                var spawnProperties = CreateSpawnPoint(global, creatureType, ESpawnMode.RandomPoint, ecb, ref idSeq, delaySec, null, monsterIds,
                                    hp, 1f, speed, out var spawnEntity);

                                //random point 模式
                                spawnProperties.PointData = CreateRandomPointData();
                                spawnProperties.MoveConfig = moveConfig;

                                ecb.AddComponent(spawnEntity, spawnProperties);
                                totalMonsterCount += 1;
                            }
                        }
                        else
                        {
                            Debug.LogError("找不到可以出现的精英ID");
                        }
                        break;
                    }
                    case ESpawnTemplate.Boss:
                    {
                        //血量权值
                        var hp = baseHp * templateDeploy.HpFac;

                        //提前个5秒播放boss来袭
                        {
                            CreateNotice(global, ecb, delaySec - 5, EBattleNotice.BossComing);
                        }

                        var extraCount = BuffHelper.GetBuffAddFactor(localPlayer, creatureLookup,
                            buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.MissionBuff_MonsterCount, 3);

                        var bossCount = (int)(1 + extraCount);
                        for (var i = 0; i < bossCount; i++)
                        {
                            var spawnProperties = CreateSpawnPoint(global, ECreatureType.Boss, ESpawnMode.RandomPoint,
                                ecb, ref idSeq, delaySec, null, monsterPool,
                                hp, 1f, 1f, out var spawnEntity);

                            //random point 模式
                            spawnProperties.PointData = CreateRandomPointData();
                            spawnProperties.MoveConfig = moveConfig;
                            ecb.AddComponent(spawnEntity, spawnProperties);
                            totalMonsterCount += 1;
                        }

                        break;
                    }

                    case ESpawnTemplate.Rush:
                    {
                        //血量权值
                        var hp = baseHp * templateDeploy.HpFac;
                        var spawnProperties = CreateSpawnPoint(global, ECreatureType.Small, ESpawnMode.RandomPointGroup, ecb, ref idSeq, delaySec, null, monsterPool,
                            hp, 1f, 1f, out var spawnEntity);

                        //冲锋怪的质量加大
                        spawnProperties.Mass = 50;

                        //冲锋，用RandomPointGroup模式(之后的左右冲锋再处理，可能需要竖条？）
                        var shape = GetShape(out var width, out var height);
                        spawnProperties.PointData = new SpawnMonsterPoint
                        {
                            Shape = shape,
                            Width = width,
                            Height = height,
                            TotalCount = templateDeploy.Count, //调试模式是否强制数量
                            DistInterval = 0.6f,
                        };
                        spawnProperties.MoveConfig = moveConfig;
                        ecb.AddComponent(spawnEntity, spawnProperties);

                        totalMonsterCount += templateDeploy.Count;
                        break;
                    }
                    case ESpawnTemplate.Group:
                    {
                        //血量权值
                        var hp = baseHp * templateDeploy.HpFac;
                        var spawnProperties = CreateSpawnPoint(global, ECreatureType.Small, ESpawnMode.RandomPointGroup, ecb, ref idSeq, delaySec, null, monsterPool,
                            hp, 1f, speed, out var spawnEntity);

                        var shape = GetShape(out var width, out var height);
                        spawnProperties.PointData = new SpawnMonsterPoint
                        {
                            Shape = shape,
                            Width = width,
                            Height = height,
                            TotalCount = templateDeploy.Count,
                            DistInterval = 0.5f,
                        };
                        spawnProperties.MoveConfig = moveConfig;
                        ecb.AddComponent(spawnEntity, spawnProperties);
                        totalMonsterCount += templateDeploy.Count;
                        break;
                    }

                    case ESpawnTemplate.Surround:
                    {
                        //血量权值
                        var hp = baseHp * templateDeploy.HpFac;
                        var spawnProperties = CreateSpawnPoint(global, ECreatureType.Small, ESpawnMode.Formation, ecb, ref idSeq, delaySec, null, monsterPool,
                            hp, 1f, 1f, out var spawnEntity);

                        var shape = ESpawnShape.Ellipse;
                        var width = templateDeploy.Radius * 0.5f;
                        var height = templateDeploy.Radius;

                        //椭圆的情况将间隔转为角度
                        var split = templateDeploy.DistInterval <= 0 ? 20f : templateDeploy.DistInterval;
                        spawnProperties.FormationData = new SpawnMonsterFormation
                        {
                            Shape = shape,
                            Width = width,
                            Height = height,
                            SplitInterval = split,
                            DelayDestroy = templateDeploy.ContTime,
                        };
                        spawnProperties.MoveConfig = moveConfig;
                        ecb.AddComponent(spawnEntity, spawnProperties);
                        break;
                    }
                    case ESpawnTemplate.Warning:
                    {
                        CreateNotice(global, ecb, delaySec - 3f, EBattleNotice.GroupEnemy);
                        break;
                    }
                    case ESpawnTemplate.Fov:
                    {
                        var spawnEntity = ecb.Instantiate(global.Empty);
                        var spawnProperties = new SpawnMonsterProperties
                        {
                            Mode = ESpawnMode.FovChange,
                            StartTime = delaySec,
                            CameraFov = templateDeploy.Param1
                        };
                        ecb.AddComponent(spawnEntity, spawnProperties);
                        break;
                    }
                }
            }
        }

        public static SpawnMonsterProperties CreateSpawnPoint(GlobalAspect global, ECreatureType monsterType, ESpawnMode mode, EntityCommandBuffer ecb,
            ref int idSeq, float sec, List<int> monsterIdx, List<int> monsterIdList, float hp, float atk, float speed, out Entity spawnEntity)
        {
            spawnEntity = ecb.Instantiate(global.Empty);

            idSeq++;
            var spawnProperties = new SpawnMonsterProperties
            {
                Id = idSeq,
                Mode = mode,
                MonsterId = new NativeList<int>(Allocator.Persistent),
                Hp = hp,
                Atk = atk,
                SpeedFac = speed > 0 ? speed - 1 : 0,
                Team = ETeamId.Monster,
                StartTime = sec,
                MonsterType = monsterType,
            };

            //随机怪物ID
            //如果monsterIdx未传入，则在monsterIdList全选随机
            if (monsterIdx == null || monsterIdx.Count <= 0)
            {
                for (var i = 0; i < monsterIdList.Count; i++)
                {
                    spawnProperties.MonsterId.Add(monsterIdList[i]);
                }
            }
            else
            {
                //如果传入索引值，则只在传入的索引里随机索引
                for (var i = 0; i < monsterIdx.Count; i++)
                {
                    var idx = monsterIdx[i];

                    //防错处理
                    if (idx >= monsterIdList.Count)
                    {
                        idx = 0;
                    }

                    spawnProperties.MonsterId.Add(monsterIdList[idx]);
                }
            }

            return spawnProperties;
        }

        public static SpawnMonsterPoint CreateRandomPointData()
        {
            //只按照randomPoint刷
            var shape = GetShape(out var width, out var height);
            return new SpawnMonsterPoint
            {
                Shape = shape, //不同的地图限制默认用不同的形状
                Width = width,
                Height = height,
                TotalCount = 1,
                RandomRange = 0.5f,
                TimeInterval = 100,
            };
        }

        private static ESpawnShape GetShape(out float width, out float height)
        {
            //宽度限制
            var offset = 5;
            width = offset * 2f;
            height = 30f;
            return ESpawnShape.Top;
        }
    }
}