using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(DropItemSystemGroup))]
    [UpdateAfter(typeof(DropItemPickupSystem))]
    public partial struct DropItemFlySystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();

            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
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

            _localToWorldLookup.Update(ref state);
            _cacheLookup.Update(ref state);

            if (SystemAPI.TryGetSingletonEntity<MainServantTag>(out var targetEntity))
            {
                var deltaTime = SystemAPI.Time.DeltaTime;
                var ecb = new EntityCommandBuffer(Allocator.TempJob);
                var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

                new FlyJob
                {
                    GlobalEntity = global.Entity,
                    Factory = global.Entity,
                    TargetEntity = targetEntity,
                    DeltaTime = deltaTime,
                    Ecb = ecb.AsParallelWriter(),
                    LocalToWorldLookup = _localToWorldLookup,
                    CacheLookup = _cacheLookup,
                    CacheEntity = cacheEntity,
                }.ScheduleParallel();
                state.Dependency.Complete();

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }

        [BurstCompile]
        private partial struct FlyJob : IJobEntity
        {
            public Entity GlobalEntity;
            public Entity CacheEntity;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity TargetEntity;
            public Entity Factory;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;

            [BurstCompile]
            private void Execute(DropItemProperties properties, RefRW<DropItemFlyTag> tag, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetDropItemConfig(properties.Id, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                if (!LocalToWorldLookup.TryGetComponent(TargetEntity, out var playerTrans))
                {
                    return;
                }

                //开始先做一个向后移动的动画
                if (!tag.ValueRO.BackAniFlag)
                {
                    tag.ValueRW.TimeSpent = tag.ValueRO.TimeSpent + DeltaTime;

                    if (tag.ValueRO.Speed <= 0)
                    {
                        tag.ValueRW.TimeSpent = 0;
                        tag.ValueRW.BackAniFlag = true;
                    }

                    var dir = math.normalizesafe(localTransform.ValueRO.Position - playerTrans.Position);

                    //move back process
                    localTransform.ValueRW.Position = localTransform.ValueRO.Position + dir * tag.ValueRO.Speed * DeltaTime;
                    var targetSpeed = tag.ValueRO.Speed - config.Acceleration * tag.ValueRO.TimeSpent;
                    tag.ValueRW.Speed = targetSpeed;
                }
                else
                {
                    var distanceSq = math.distancesq(playerTrans.Position, localTransform.ValueRO.Position);
                    if (distanceSq <= 0.25f)
                    {
                        //播放拾取音效
                        if (config.PickupSound > 0)
                        {
                            Ecb.AppendToBuffer(sortKey, Factory, new PlaySoundBuffer { SoundId = config.PickupSound });
                        }

                        //添加buff给玩家处理
                        Ecb.AppendToBuffer(sortKey, TargetEntity, new PickupBuffer
                        {
                            DropItemId = config.Id,
                            Action = config.Action,
                            Param1 = config.Param1,
                            Param2 = config.Param2,
                            Param3 = config.Param3,
                            Param4 = config.Param4,
                            Value = properties.Value,
                        });
                        Ecb.SetComponentEnabled<PickupBuffer>(sortKey, TargetEntity, true);

                        //destroy entity
                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });
                    }
                    else
                    {
                        //飞翔玩家处理
                        var dir = math.normalizesafe(playerTrans.Position - localTransform.ValueRO.Position);
                        localTransform.ValueRW.Position = localTransform.ValueRO.Position + dir * tag.ValueRO.Speed * DeltaTime;

                        //加速度处理
                        tag.ValueRW.TimeSpent = tag.ValueRO.TimeSpent + DeltaTime;
                        tag.ValueRW.Speed = tag.ValueRO.Speed + config.Acceleration * tag.ValueRO.TimeSpent;

                        if (tag.ValueRO.Speed > 15)
                        {
                            tag.ValueRW.Speed = 15;
                        }
                    }
                }
            }
        }
    }
}