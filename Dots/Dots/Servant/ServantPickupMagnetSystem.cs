using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    [UpdateAfter(typeof(ServantPickupSystem))]
    public partial struct ServantPickupMagnetSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (tag, playerTrans, playerEntity) in SystemAPI.Query<PickupMagnetTag, LocalTransform>().WithEntityAccess())
            {
                //遍历所有的dropitem,找距离小于的
                foreach (var (idleTag, dropItemTrans, dropItemEntity) in SystemAPI.Query<DropItemIdleTag, LocalTransform>().WithEntityAccess())
                {
                    if (cache.GetDropItemConfig(idleTag.Id, out var dropItemConfig))
                    {
                        if (!dropItemConfig.CanMagnet)
                        {
                            continue;
                        }

                        if (math.distancesq(dropItemTrans.Position, playerTrans.Position) < tag.Radius * tag.Radius)
                        {
                            ecb.SetComponentEnabled<DropItemIdleTag>(dropItemEntity, false);

                            ecb.SetComponent(dropItemEntity, new DropItemFlyTag { Speed = dropItemConfig.Speed, TimeSpent = 0, BackAniFlag = false, });
                            ecb.SetComponentEnabled<DropItemFlyTag>(dropItemEntity, true);
                        }
                    }
                }

                ecb.SetComponentEnabled<PickupMagnetTag>(playerEntity, false);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}