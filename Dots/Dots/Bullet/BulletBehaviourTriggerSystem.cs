using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletHitSystem))] 
    public partial struct BulletBehaviourTriggerSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CacheProperties>();
            
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>(true);
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
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

            _destroyLookup.Update(ref state);
            _creatureTag.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _creatureLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            
            //BehaviourTriggerJob
            new BehaviourTriggerJob
            {
                Ecb = ecb.AsParallelWriter(),
                CurrTime = global.Time,
                ScreenBound = global.ScreenBound,
                DeltaTime = SystemAPI.Time.DeltaTime,
                GlobalEntity = global.Entity,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                DestroyLookup = _destroyLookup,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                TransformLookup = _transformLookup,
                CreatureLookup = _creatureLookup,
                World = collisionWorld,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct BehaviourTriggerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float CurrTime;
            public float DeltaTime;
            public Entity GlobalEntity;
            public float4 ScreenBound;
            public Entity CacheEntity;
            
            [ReadOnly] public CollisionWorld World;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<BulletDestroyTag> DestroyLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;

            [BurstCompile]
            private void Execute(DynamicBuffer<BulletBehaviourBuffer> buffers, LocalTransform localTransform, DynamicBuffer<BulletTriggerBuffer> triggerBuffers, RefRW<BulletProperties> properties, RefRW<RandomSeed> random,
                Entity entity, [EntityIndexInQuery] int sortKey)
            {
                var list = new NativeArray<BulletBehaviourBuffer>(buffers.Length, Allocator.Temp);
                for (var i = 0; i < buffers.Length; i++)
                {
                    list[i] = buffers[i];
                }

                for (var i = list.Length - 1; i >= 0; i--)
                {
                    //get config
                    var behaviour = list[i];

                    if (behaviour.TriggerFinished)
                    {
                        continue;
                    }

                    if (!CacheHelper.GetBulletBehaviourConfig(behaviour.Id, CacheEntity, CacheLookup, out var info))
                    {
                        continue;
                    }

                    //延迟处理
                    if (behaviour.InDelay)
                    {
                        behaviour.Timer += DeltaTime;
                        if (behaviour.Timer >= behaviour.WaitSec)
                        {
                            Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                            Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                            behaviour.InDelay = false;
                        }
                    }

                    //cd处理
                    if (behaviour.InCd)
                    {
                        behaviour.Timer += DeltaTime;
                        if (behaviour.Timer >= behaviour.WaitSec)
                        {
                            behaviour.Timer = 0;
                            behaviour.InCd = false;
                        }
                    }

                    if (!behaviour.InCd && !behaviour.InDelay)
                    {
                        switch (info.TriggerAction)
                        {
                            case EBulletBehaviourTrigger.Delay:
                            {
                                var delayTime = info.TriggerParam1;
                                behaviour.Timer += DeltaTime;
                                if (behaviour.Timer >= delayTime)
                                {
                                    Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                    Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                                    behaviour.TriggerFinished = true;
                                }

                                break;
                            }
                            case EBulletBehaviourTrigger.Interval:
                            {
                                var cd = info.TriggerParam1;
                                var delay = info.TriggerParam2;
                                if (CurrTime - properties.ValueRO.CreateTime >= delay)
                                {
                                    //CD触发
                                    behaviour.Timer -= DeltaTime;
                                    if (behaviour.Timer <= 0)
                                    {
                                        behaviour.Timer = cd;
                                        Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                        Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                                    }
                                }

                                break;
                            }
                            case EBulletBehaviourTrigger.OnVelocityLess:
                            {
                                var checkValue = info.TriggerParam1;
                                if (properties.ValueRO.V1 < checkValue)
                                {
                                    Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                    Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                                    behaviour.TriggerFinished = true;
                                }

                                break;
                            }
                            case EBulletBehaviourTrigger.OnVelocityGreater:
                            {
                                var checkValue = info.TriggerParam1;
                                if (properties.ValueRO.V1 >= checkValue)
                                {
                                    Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                    Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                                    behaviour.TriggerFinished = true;
                                }

                                break;
                            }
                            case EBulletBehaviourTrigger.OnFlyDist:
                            {
                                var needDist = info.TriggerParam1.ToInt();
                                var prob = info.TriggerParam2;
                                var maxTriggerCount = info.TriggerParam3.ToInt();
                                var delay = info.TriggerParam4;

                                if (prob <= 0)
                                {
                                    prob = 1;
                                }

                                if (CurrTime - properties.ValueRO.CreateTime >= delay)
                                {
                                    foreach (var trigger in triggerBuffers)
                                    {
                                        if (trigger.Type != EBulletBehaviourTrigger.OnFlyDist)
                                        {
                                            continue;
                                        }
                                        behaviour.CountFlag += trigger.FlyDist;
                                    }

                                    if (behaviour.CountFlag >= needDist)
                                    {
                                        behaviour.CountFlag = 0;
                                        if (random.ValueRW.Value.NextFloat(0f, 1f) < prob)
                                        {
                                            Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                            Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                                            behaviour.TriggerCount++;

                                            if (maxTriggerCount > 0 && behaviour.TriggerCount >= maxTriggerCount)
                                            {
                                                behaviour.TriggerFinished = true;
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                            case EBulletBehaviourTrigger.OnFlyOver:
                            {
                                if (properties.ValueRO.ContTime > 0 && properties.ValueRO.RunningTime >= properties.ValueRO.ContTime)
                                {
                                    Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                    Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                                    behaviour.TriggerFinished = true;
                                }

                                break;
                            }

                            case EBulletBehaviourTrigger.OnHitEnemy:
                            {
                                var needCount = info.TriggerParam1.ToInt();
                                var needBuffType = info.TriggerParam2.ToInt();
                                var maxTriggerCount = info.TriggerParam3.ToInt();
                                
                                foreach (var trigger in triggerBuffers)
                                {
                                    if (trigger.Type != EBulletBehaviourTrigger.OnHitEnemy)
                                    {
                                        continue;
                                    }
                                    var enemy = trigger.Enemy;

                                    //check buff
                                    if (needBuffType != 0)
                                    {
                                        if (!BuffHelper.CheckHasBuffType((EBuffType)needBuffType, enemy, BuffEntitiesLookup, BuffTagLookup))
                                        {
                                            continue;
                                        }
                                    }

                                    behaviour.CountFlag += 1;
                                    if (behaviour.CountFlag >= needCount)
                                    {
                                        Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                        Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });

                                        behaviour.TriggerCount += 1;
                                        if (maxTriggerCount > 0 && behaviour.TriggerCount >= maxTriggerCount)
                                        {
                                            behaviour.TriggerFinished = true;
                                        }
                                    }
                                }

                                break;
                            }
                            //追踪到目标时
                            case EBulletBehaviourTrigger.HeadingTargetOver:
                            { 
                                var minDist = info.TriggerParam1;
                                if (minDist < 0.3f) minDist = 0.3f;
                                if (TransformLookup.TryGetComponent(properties.ValueRO.HeadingToTarget, out var targetTransform) &&
                                    CreatureLookup.TryGetComponent(properties.ValueRO.HeadingToTarget, out var targetCreature))
                                {
                                    var tar = properties.ValueRO.HeadingOffset + CreatureHelper.getCenterPos(targetTransform.Position, targetCreature, targetTransform.Value.Scale().x);
                                    if (math.distance(localTransform.Position, tar) < minDist)
                                    {
                                        Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                        Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, });
                                        behaviour.TriggerFinished = true;
                                    }
                                }

                                break;
                            }
                            //判断是否出屏幕了
                            case EBulletBehaviourTrigger.OnOutScreen:
                            {
                                var maxCount = info.TriggerParam1.ToInt();

                                if (MathHelper.CheckOutScreen(localTransform.Position, 0, ScreenBound, out var normal))
                                {
                                    Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                    Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, HitNormal = normal });

                                    behaviour.CountFlag += 1;
                                    behaviour.InCd = true;
                                    behaviour.WaitSec = 0.1f;

                                    if (maxCount > 0 && behaviour.CountFlag >= maxCount)
                                    {
                                        behaviour.TriggerFinished = true;
                                    }
                                }

                                break;
                            }
                            case EBulletBehaviourTrigger.OnHitObstacle:
                            {
                                var maxCount = info.TriggerParam1.ToInt();

                                if (PhysicsHelper.RayCast(World, PhysicsLayers.DontMove, localTransform.Position, properties.ValueRO.RunningDirection, 0.2f, out var hit))
                                {
                                    Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                    Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id, HitNormal = hit.SurfaceNormal});
                                    
                                    behaviour.CountFlag += 1;
                                    behaviour.InCd = true;
                                    behaviour.WaitSec = 0.1f;
                                    if (maxCount > 0 && behaviour.CountFlag >= maxCount)
                                    {
                                        behaviour.TriggerFinished = true;
                                    }
                                }
                                break;
                            }
                            case EBulletBehaviourTrigger.BeforeDestroy:
                            {
                                if (DestroyLookup.HasComponent(entity) && DestroyLookup.IsComponentEnabled(entity))
                                {
                                    Ecb.SetComponentEnabled<BulletActionBuffer>(sortKey, entity, true);
                                    Ecb.AppendToBuffer(sortKey, entity, new BulletActionBuffer { BehaviourId = info.Id});
                                    behaviour.TriggerFinished = true;
                                }

                                break;
                            }
                        }
                    }

                    list[i] = behaviour;
                }

                buffers.Clear();
                buffers.CopyFrom(list);
                list.Dispose();
                triggerBuffers.Clear();
            }
        }
    }
}