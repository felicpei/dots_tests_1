using Deploys;
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
    public partial struct DropItemPickupSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
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

            if (SystemAPI.TryGetSingletonEntity<MainServantTag>(out var mainServant))
            {
                
                _localToWorldLookup.Update(ref state);
                _buffEntitiesLookup.Update(ref state);
                _buffTagLookup.Update(ref state);
                _buffCommonLookup.Update(ref state);
                _cacheLookup.Update(ref state);
                _creatureLookup.Update(ref state);
                _attrLookup.Update(ref state);
                _attrModifyLookup.Update(ref state);

                
                var ecb = new EntityCommandBuffer(Allocator.TempJob);
                var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
                var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
                var delteTime = SystemAPI.Time.DeltaTime;
                
                new PickupJob
                {
                    GlobalEntity = global.Entity,
                    Ecb = ecb.AsParallelWriter(),
                    DeltaTime = delteTime,
                    LocalPlayer = localPlayer,
                    MainServant = mainServant,
                    LocalToWorldLookup = _localToWorldLookup,
                    BuffEntitiesLookup = _buffEntitiesLookup,
                    BuffTagLookup = _buffTagLookup,
                    BuffCommonLookup = _buffCommonLookup,
                    CacheLookup = _cacheLookup,
                    CacheEntity = cacheEntity,
                    CreatureLookup = _creatureLookup,
                    AttrLookup = _attrLookup,
                    AttrModifyLookup = _attrModifyLookup,
                }.ScheduleParallel();
                state.Dependency.Complete();

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }

        [BurstCompile]
        private partial struct PickupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity GlobalEntity;
            public Entity CacheEntity;
            public Entity LocalPlayer;
            public Entity MainServant;
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            
            [BurstCompile]
            private void Execute(RefRW<DropItemIdleTag> tag, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetDropItemConfig(tag.ValueRO.Id, CacheEntity, CacheLookup, out var dropItemConfig))
                {
                    return;
                }

                //self rotate  
                if (dropItemConfig.RotateSpeed > 0)
                {
                    localTransform.ValueRW.Rotation = MathHelper.RotateQuaternion(localTransform.ValueRO.Rotation, dropItemConfig.RotateSpeed * DeltaTime);
                }
                
                var radius = 2f;

                if (!dropItemConfig.CanMagnet)
                {
                    radius = 1.2f;
                }
                else
                {
                    //检查buff
                    var addFactor = AttrHelper.GetAttr(LocalPlayer, EAttr.PickupRange, AttrLookup, AttrModifyLookup, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                    radius = BuffHelper.CalcFactor2(radius, addFactor);
                }
                
                if (LocalToWorldLookup.TryGetComponent(MainServant, out var playerTrans))
                {
                    var sqrDist = math.distancesq(localTransform.ValueRO.Position, playerTrans.Position);
                    if (sqrDist < radius * radius || dropItemConfig.AutoFly)
                    {
                        Ecb.SetComponent(sortKey, entity, new DropItemFlyTag
                        {
                            Speed = dropItemConfig.Speed,
                            TimeSpent = 0,
                            BackAniFlag = false,
                        });
                        Ecb.SetComponentEnabled<DropItemFlyTag>(sortKey, entity, true);
                        Ecb.SetComponentEnabled<DropItemIdleTag>(sortKey, entity, false);
                    }
                    else
                    {
                        tag.ValueRW.Timer += DeltaTime;
                        
                        //如果一致没有人拾取, 超时destroy掉
                        if (dropItemConfig.DestroyDelay > 0)
                        {
                            if (tag.ValueRO.Timer >= dropItemConfig.DestroyDelay)
                            {
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });    
                            }
                            else if (tag.ValueRO.Timer >= dropItemConfig.DestroyDelay - 5f)
                            {
                                if (!tag.ValueRO.StartFlash)
                                {
                                    tag.ValueRW.StartFlash = true;    
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}