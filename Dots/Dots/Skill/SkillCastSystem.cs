using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SkillSystemGroup))]
    [UpdateAfter(typeof(SkillTargetSystem))]
    public partial struct SkillCastSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _cacheLookup  = state.GetComponentLookup<CacheProperties>(true);
            _creatureLookup = state.GetComponentLookup<CreatureTag>(true);
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
            _cacheLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            new SkillCastJob
            {
                CurrTime = global.Time,
                Ecb = ecb.AsParallelWriter(),
                InMonsterPause = global.InMonsterPause,
                GlobalEntity = global.Entity,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                CreatureLookup = _creatureLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct SkillCastJob : IJobEntity
        {
            public float CurrTime;
            public Entity CacheEntity;
            public Entity GlobalEntity;
            public bool InMonsterPause;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureLookup;
            
            [BurstCompile]
            private void Execute(MasterCreature master, DynamicBuffer<SkillTargetBuffer> targetBuffers, RefRW<SkillProperties> properties, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetSkillConfig(properties.ValueRO.Id, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }
                if (!CreatureLookup.TryGetComponent(master.Value, out var masterCreature))
                {
                    return;
                }

                if (InMonsterPause && masterCreature.TeamId == ETeamId.Monster)
                {
                    return;
                }
                
                var castConfig = config.Cast;
                for (var i = targetBuffers.Length - 1; i >= 0; i--)
                {
                    var buffer = targetBuffers[i];

                    //延迟的模式, 超时时间取配置表
                    var castDelay = castConfig.Method == ESkillCast.DelayCast ? castConfig.Param1 : 0;

                    //检查延迟时间
                    if (CurrTime - buffer.CreateTime < castDelay)
                    {
                        continue;
                    }

                    //满足时间的移除并执行
                    targetBuffers.RemoveAt(i);

                    switch (castConfig.Method)
                    {
                        case ESkillCast.DelayCast: //这里延迟在前面处理过
                        case ESkillCast.Imminently:
                        {
                            Ecb.AppendToBuffer(sortKey, entity, new SkillEndBuffer(buffer.StartPos, buffer.Pos, buffer.Entity, properties.ValueRO.AtkValue, i)
                            {
                                Param1 = buffer.Param1  
                            });
                            Ecb.SetComponentEnabled<SkillEndBuffer>(sortKey, entity, true);
                            break;
                        }
                        //向目标点发射子弹
                        case ESkillCast.Bullet:
                        {
                            //获得子弹ID
                            var bulletId = (int)castConfig.Param1;
                            var lookDir = (int)castConfig.Param2 == 1;
                            var trace = (int)castConfig.Param3 == 1;
                            var extraDist = castConfig.Param4;
                            
                            if (bulletId <= 0)
                            {
                                Debug.LogError($"Bullet id == 0?， skillCast， SkillId:{config.Id}");
                            }
                            else
                            {
                                //向目标点发射子弹
                                var endPos = buffer.Pos;
                                var startPos = buffer.StartPos;
                                var direction = math.normalizesafe(endPos - startPos);
                                if (extraDist > 0)
                                {
                                    endPos += direction * extraDist;
                                }
                                
                                var bTrace = CreatureLookup.HasComponent(buffer.Entity) && trace;
                                //新建一颗子弹 
                                var bulletBuffer = new BulletCreateBuffer
                                {
                                    BulletId = bulletId,
                                    ParentCreature = master.Value,
                                    AtkValue = properties.ValueRO.AtkValue,
                                    Direction = direction, 
                                    ShootPos = startPos,
                                    DisableBounce = true,
                                    DisableSplit = true,
                                    SkillEntity = entity,
                                    AllowSkillEndAction = true,
                                    SkillEndActionIndex = i,
                                    ForceDistance = lookDir ? 0 : math.distance(startPos, endPos),
                                    TraceCreature = bTrace ? buffer.Entity : Entity.Null
                                }; 
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);
                            }

                            break;
                        }
                        default:
                        {
                            Debug.LogError($"配置了不存在的SkillCast:{castConfig.Method}");
                            break;
                        }
                    }
                }
            }
        }
    }
}