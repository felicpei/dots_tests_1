using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    //刷怪System
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSpawnSystemGroup))]
    public partial struct SpawnMonsterSystem : ISystem
    {
        private EntityQuery _query;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _query = state.GetEntityQuery(ComponentType.ReadOnly<MonsterProperties>());
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
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

            _monsterLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);

            var currCount = _query.CalculateEntityCount();
            global.TotalMonsterCount = currCount;

            //同屏最多1000个怪
            if (currCount > 2000)
            {
                return;
            }
       
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            var localPlayerTrans = SystemAPI.GetComponent<LocalToWorld>(localPlayer);
            var playerPosition = localPlayerTrans.Position;
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            foreach (var (spawn, entity) in SystemAPI.Query<RefRW<SpawnMonsterProperties>>().WithEntityAccess())
            {
                //没到时间的不刷
                if (spawn.ValueRO.StartTime > global.WaveCurTime)
                {
                    continue;
                }

                switch (spawn.ValueRO.Mode)
                {
                    //Point模式
                    case ESpawnMode.RandomPoint:
                    {
                        var data = spawn.ValueRO.PointData;

                        //如果是Point模式, 数量够了就destroy掉
                        if (spawn.ValueRO.RefreshedCount >= data.TotalCount)
                        {
                            spawn.ValueRW.RefreshFinished = true;
                            continue;
                        }

                        //point模式按时间刷新
                        if (spawn.ValueRO.Timer > 0)
                        {
                            spawn.ValueRW.Timer = spawn.ValueRO.Timer - deltaTime;
                            break;
                        }

                        if (spawn.ValueRO.MonsterId.Length <= 0)
                        {
                            Debug.LogError($"RandomPoint 刷怪配置中没有MonsterId, id: {spawn.ValueRO.Id}");
                            break;
                        }

                        var monsterId = spawn.ValueRO.MonsterId[global.Random.ValueRW.Value.NextInt(0, spawn.ValueRO.MonsterId.Length)];
                        if (cache.GetMonsterConfig(monsterId, out var monsterConfig))
                        {
                            //随机下一次的刷新时间
                            spawn.ValueRW.Timer = data.TimeInterval;
                            spawn.ValueRW.RefreshedCount = spawn.ValueRO.RefreshedCount + 1;

                            //alive不计算障碍物类型的怪物
                            spawn.ValueRW.AliveCount = spawn.ValueRO.AliveCount + 1;
                            spawn.ValueRW.ReadyCount = 0;

                            //刷怪
                            var hpFac = spawn.ValueRO.Hp;
                            var atkFac = spawn.ValueRO.Atk;
                            var speedFac = spawn.ValueRO.SpeedFac;

                            //刷怪点权值处理
                            var monsterProps = global.MonsterProps;
                            monsterProps.Atk *= atkFac;
                            monsterProps.Hp *= hpFac;
                            monsterProps.SpeedFac += speedFac;

                            var bornPos = GetBornPos(global, playerPosition, data.Shape, data.Width, data.Height, data.RandomRange);
                            var monster = FactoryHelper.CreateMonster(collisionWorld, global, cache, monsterId, spawn.ValueRO, Entity.Null, 
                                monsterProps, spawn.ValueRO.Team, bornPos, bornPos, ecb);
                            
                            if (monster == Entity.Null)
                            {
                                Debug.LogError($"刷怪失败, 刷怪表ID:{spawn.ValueRO.Id}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"spawn monster error, monsterId不存在:{monsterId}");
                        }

                        break;
                    }

                    //Point模式，一次刷一群, 一次性
                    case ESpawnMode.RandomPointGroup:
                    {
                        if (spawn.ValueRO.MonsterId.Length <= 0)
                        {
                            Debug.LogError($"RandomPointGroup 刷怪配置中没有MonsterId, id: {spawn.ValueRO.Id}");
                            break;
                        }

                        var monsterId = spawn.ValueRO.MonsterId[global.Random.ValueRW.Value.NextInt(0, spawn.ValueRO.MonsterId.Length)];
                        var data = spawn.ValueRO.PointData;

                        //StartPos
                        var startPos = CreatureHelper.GetRandomShapePosition(data.Shape, playerPosition, global.Random, data.Width, data.Height, data.RandomRange);
                        var team = spawn.ValueRO.Team;

                        //刷怪点权值处理
                        var monsterProps = global.MonsterProps;
                        monsterProps.Atk *= spawn.ValueRO.Atk;
                        monsterProps.Hp *= spawn.ValueRO.Hp;
                        monsterProps.SpeedFac += spawn.ValueRO.SpeedFac;


                        var circleCenterPos = startPos;
                        var circleId = 0;
                        var circleFinished = true;
                        var circleCurCount = 0;
                        for (var i = 0; i < data.TotalCount; i++)
                        {
                            //先刷新中心点的
                            var monster = FactoryHelper.CreateMonster(collisionWorld, global, cache, monsterId, spawn.ValueRO, Entity.Null, monsterProps, team, startPos, circleCenterPos, ecb);
                            if (monster == Entity.Null)
                            {
                                Debug.LogError($"刷怪失败, 刷怪表ID:{spawn.ValueRO.Id}");
                                continue;
                            }

                            if (circleFinished)
                            {
                                circleId += 1;
                                startPos = circleCenterPos + new float3(0, data.DistInterval * circleId, 0);
                                circleFinished = false;
                                circleCurCount = 1;
                            }
                            else
                            {
                                //绕圈
                                var angle = 60f / circleId;
                                var x = data.DistInterval * circleId * math.sin(math.radians(angle) * circleCurCount);
                                var y = data.DistInterval * circleId * math.cos(math.radians(angle) * circleCurCount);
                                var pos = circleCenterPos + new float3(x, y, 0);
                                startPos = pos;
                                circleCurCount++;

                                var maxCircleCount = 360f / angle;
                                if (circleCurCount > maxCircleCount)
                                {
                                    circleCurCount = 0;
                                    circleFinished = true;
                                }
                            }
                        }

                        //阵型模式一次性刷出，刷新后立即destroy掉
                        ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                        break;
                    }
                    //阵型模式
                    case ESpawnMode.Formation:
                    {
                        if (spawn.ValueRO.MonsterId.Length <= 0)
                        {
                            Debug.LogError($"Formation 刷怪配置中没有MonsterId, id: {spawn.ValueRO.Id}");
                            break;
                        }

                        var monsterId = spawn.ValueRO.MonsterId[global.Random.ValueRW.Value.NextInt(0, spawn.ValueRO.MonsterId.Length)];
                        var data = spawn.ValueRO.FormationData;
                        var team = spawn.ValueRO.Team;

                        //刷怪点权值处理
                        var monsterProps = global.MonsterProps;
                        monsterProps.Atk *= spawn.ValueRO.Atk;
                        monsterProps.Hp *= spawn.ValueRO.Hp;
                        monsterProps.SpeedFac += spawn.ValueRO.SpeedFac;

                        var position = playerPosition;

                        //刷新出不同形状
                        switch (data.Shape)
                        {
                            case ESpawnShape.Rectangle:
                            {
                                //矩形阵型
                                CreatureHelper.SpawnFormationRect(collisionWorld, global, cache, monsterId, spawn.ValueRO, monsterProps, team, position, data, ecb);
                                break;
                            }
                            case ESpawnShape.Ellipse:
                            {
                                //椭圆阵型
                                CreatureHelper.SpawnFormationOval(collisionWorld, global, cache, monsterId, spawn.ValueRO, monsterProps, team, position, data, ecb);
                                break;
                            }
                            case ESpawnShape.Horiz:
                            {
                                //左右阵型
                                CreatureHelper.SpawnFormationRect(collisionWorld, global, cache, monsterId, spawn.ValueRO, monsterProps, team, position, data, ecb, false);
                                break;
                            }
                            case ESpawnShape.Vert:
                            {
                                //上下阵型
                                CreatureHelper.SpawnFormationRect(collisionWorld, global, cache, monsterId, spawn.ValueRO, monsterProps, team, position, data, ecb, true, false);
                                break;
                            }
                            default:
                            {
                                Debug.LogError($"不支持的阵型Shape:{data.Shape}");
                                break;
                            }
                        }

                        //阵型模式一次性刷出，刷新后立即destroy掉
                        ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                        break;
                    }

                    //线性增加的随机点刷怪
                    case ESpawnMode.Linear:
                    {
                        var data = spawn.ValueRO.LinearData;

                        //当前场上怪物大于等于同屏总数限制, 不处理
                        if (data.MaxAliveLimit > 0 && spawn.ValueRO.AliveCount >= data.MaxAliveLimit)
                        {
                            continue;
                        }

                        //优先刷cache中的(每次1个)
                        if (spawn.ValueRO.LinearData.CachingCount > 0)
                        {
                            SpawnLinearMonster(spawn, global, cache, playerPosition, ecb, collisionWorld);
                            spawn.ValueRW.LinearData.CachingCount--;

                            //Debug.LogError($"刷怪: from cache:{data.CachingCount}"); 
                            continue;
                        }

                        //超时的destroy掉
                        if (spawn.ValueRO.CurrWaveId >= data.WaveCount)
                        {
                            spawn.ValueRW.RefreshFinished = true;
                            continue;
                        }

                        //刷怪
                        var timeInterval = data.TotalSec / data.WaveCount;
                        if (spawn.ValueRO.Timer > 0)
                        {
                            spawn.ValueRW.Timer = spawn.ValueRO.Timer - deltaTime;
                            break;
                        }

                        if (spawn.ValueRO.MonsterId.Length <= 0)
                        {
                            Debug.LogError($"Linear 刷怪配置中没有MonsterId, id: {spawn.ValueRO.Id}");
                        }
                        else
                        {
                            //改变wave
                            var waveId = spawn.ValueRO.CurrWaveId;
                            spawn.ValueRW.CurrWaveId = waveId + 1;

                            //下一次的刷新时间
                            spawn.ValueRW.Timer = timeInterval;

                            var wantSpawnCount = data.StartCount + waveId * data.CountInterval;
                            var monsterCount = wantSpawnCount;

                            //同屏怪物数量限制
                            if (data.MaxAliveLimit > 0)
                            {
                                if (spawn.ValueRO.AliveCount + wantSpawnCount > data.MaxAliveLimit)
                                {
                                    monsterCount = wantSpawnCount - data.MaxAliveLimit;
                                    spawn.ValueRW.LinearData.CachingCount = wantSpawnCount - monsterCount;
                                }
                            }

                            //Debug.LogError($"刷怪:WaveId:{waveId} curTime:{global.time} count:{monsterCount}");
                            for (var m = 0; m < monsterCount; m++)
                            {
                                SpawnLinearMonster(spawn, global, cache, playerPosition, ecb, collisionWorld);
                            }
                        }

                        break;
                    }

                    //线性增长的群怪生成
                    case ESpawnMode.LinearGroup:
                    {
                        var data = spawn.ValueRO.GroupLinearData;

                        //超时的destroy掉
                        if (spawn.ValueRO.CurrWaveId >= data.WaveCount)
                        {
                            ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                            continue;
                        }

                        if (spawn.ValueRO.Timer > 0)
                        {
                            spawn.ValueRW.Timer = spawn.ValueRO.Timer - deltaTime;
                            break;
                        }

                        if (spawn.ValueRO.MonsterId.Length <= 0)
                        {
                            Debug.LogError($"LinearGroup 刷怪配置中没有MonsterId, id: {spawn.ValueRO.Id}");
                            break;
                        }

                        //改变wave
                        var waveId = spawn.ValueRO.CurrWaveId;
                        spawn.ValueRW.CurrWaveId = waveId + 1;

                        //下一次的刷新时间
                        spawn.ValueRW.Timer = data.TimeInterval;

                        var maxHpFactor = spawn.ValueRO.Hp + waveId * data.IntervalAddHp;
                        var monsterCount = data.StartCount + waveId * data.IntervalAddCount;
                        var monsterId = spawn.ValueRO.MonsterId[global.Random.ValueRW.Value.NextInt(0, spawn.ValueRO.MonsterId.Length)];

                        //StartPos
                        var startPos = CreatureHelper.GetRandomShapePosition(data.Shape, playerPosition, global.Random, data.Width, data.Height, data.RandomRange);

                        //Debug.LogError($"刷怪group:monsterCount:{monsterCount} maxHp:{maxHp} monsterId:{monsterId} startPos:{startPos} {global.time}");
                        var atk = spawn.ValueRO.Atk;
                        var speedFac = spawn.ValueRO.SpeedFac;
                        var team = spawn.ValueRO.Team;

                        var monsterProps = global.MonsterProps;
                        monsterProps.Atk *= atk;
                        monsterProps.Hp *= maxHpFactor;
                        monsterProps.SpeedFac += speedFac;

                        var circleCenterPos = startPos;
                        var circleId = 0;
                        var circleFinished = true;
                        var circleCurCount = 0;

                        for (var i = 0; i < monsterCount; i++)
                        {
                            //先刷新中心点的
                            var monster = FactoryHelper.CreateMonster(collisionWorld, global, cache, monsterId, spawn.ValueRO, Entity.Null, monsterProps, team, startPos, circleCenterPos, ecb);
                            if (monster == Entity.Null)
                            {
                                Debug.LogError($"刷怪失败, 刷怪表ID:{spawn.ValueRO.Id}");
                                continue;
                            }

                            if (circleFinished)
                            {
                                circleId += 1;
                                startPos = circleCenterPos + new float3(0, data.DistInterval * circleId, 0);
                                circleFinished = false;
                                circleCurCount = 1;
                            }
                            else
                            {
                                //绕圈
                                var angle = 60f / circleId;
                                var x = data.DistInterval * circleId * math.sin(math.radians(angle) * circleCurCount);
                                var y = data.DistInterval * circleId * math.cos(math.radians(angle) * circleCurCount);
                                var pos = circleCenterPos + new float3(x, y, 0);
                                startPos = pos;
                                circleCurCount++;

                                var maxCircleCount = 360f / angle;
                                if (circleCurCount > maxCircleCount)
                                {
                                    circleCurCount = 0;
                                    circleFinished = true;
                                }
                            }
                        }

                        break;
                    }
                    //战斗公告
                    case ESpawnMode.UINotice:
                    {
                        ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                        ecb.AppendToBuffer(localPlayer, new UIUpdateBuffer
                        {
                            Value = new EventData
                            {
                                Command = EEventCommand.OnNotice,
                                Param1 = (int)spawn.ValueRO.NoticeId,
                            }
                        });
                        break;
                    }
                    case ESpawnMode.FovChange:
                    {
                        ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                        Debug.LogError("todo change camera fov");
                        break;
                    }
                    default:
                    {
                        Debug.LogError($"配置了不存在的刷怪模式:{(int)spawn.ValueRO.Mode}");
                        break;
                    }
                }
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static void SpawnLinearMonster(RefRW<SpawnMonsterProperties> spawn, GlobalAspect global,
            CacheAspect cache, float3 position, EntityCommandBuffer ecb, CollisionWorld collisionWorld)
        {
            var monsterId = spawn.ValueRO.MonsterId[global.Random.ValueRW.Value.NextInt(0, spawn.ValueRO.MonsterId.Length)];
            if (!cache.GetMonsterConfig(monsterId, out var monsterConfig))
            {
                Debug.LogError($"SpawnLinearMonster error, monsterId 不存在:{monsterId}");
                return;
            }

            var data = spawn.ValueRO.LinearData;

            //增加数量
            spawn.ValueRW.RefreshedCount = spawn.ValueRO.RefreshedCount + 1;

            //生存数量
            spawn.ValueRW.AliveCount = spawn.ValueRO.AliveCount + 1;

            //随机出生点
            var bornPos = GetBornPos(global, position, data.Shape, data.Width, data.Height, data.RandomRange);

            var monsterProps = global.MonsterProps;
            monsterProps.Atk *= spawn.ValueRO.Atk;
            monsterProps.Hp *= spawn.ValueRO.Hp;

            var team = spawn.ValueRO.Team;

            //刷怪
            var monster = FactoryHelper.CreateMonster(collisionWorld, global, cache, monsterId, spawn.ValueRO, Entity.Null, monsterProps, team, bornPos, bornPos, ecb);
            if (monster != Entity.Null)
            {
            }
            else
            {
                Debug.LogError($"刷怪失败, 刷怪表ID:{spawn.ValueRO.Id}");
            }
        }


        private static float3 GetBornPos(GlobalAspect global, float3 playerPos, ESpawnShape shape, float width, float height, float randomRange)
        {
            return CreatureHelper.GetRandomShapePosition(shape, playerPos, global.Random, width, height, randomRange);
        }
    }
}