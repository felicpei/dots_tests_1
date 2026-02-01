using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletMoveSystem))]
    public partial struct BulletSplitSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<BulletBombTag> _bombLookup;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            
            _bombLookup = state.GetComponentLookup<BulletBombTag>();
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>();
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

            _bombLookup.Update(ref state);
            _destroyLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            //分裂 
            new BulletSplitJob
            {
                Factory = global.Entity,
                Ecb = ecb.AsParallelWriter(),
                BombLookup = _bombLookup,
                DestroyLookup = _destroyLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct BulletSplitJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity Factory;
            [ReadOnly] public ComponentLookup<BulletBombTag> BombLookup;
            [ReadOnly] public ComponentLookup<BulletDestroyTag> DestroyLookup;

            
            [BurstCompile]
            private void Execute(BulletSplit splitInfo, BulletProperties properties, BulletAtkValue atkValue, LocalTransform transform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (BombLookup.IsComponentEnabled(entity) || DestroyLookup.IsComponentEnabled(entity))
                {
                    return;
                }
                
                //if (properties.RunningTime >= 0)
                {
                    var splitAngle = splitInfo.SplitAngle;
                    var splitCount = splitInfo.SplitCount;

                    //先计算角度分裂
                    if (splitCount > 1)
                    {
                        for (var i = 0; i < splitCount; i++)
                        {
                            var shootForward = MathHelper.CalcSectorSplitForward(splitCount, i, splitAngle, properties.D1);
                            var shootPos = transform.Position;

                            //如果水平分裂数量 > 1, 要再处理一下水平分裂
                            if (splitInfo.HorizCount > 1)
                            {
                                BulletHelper.SplitHoriz(splitInfo, properties, atkValue, shootForward, shootPos, Factory, Ecb, sortKey);
                            }
                            else
                            {
                                var createBuffer = new BulletCreateBuffer
                                {
                                    BulletId = splitInfo.SplitBulletId,
                                    DisableSplit = !splitInfo.NextCanSplit,
                                    Direction = shootForward,
                                    ShootPos = shootPos,
                                    AtkValue = new AtkValue(atkValue.Atk, atkValue.Crit, atkValue.CritDamage, properties.Team),
                                    ParentCreature = properties.MasterCreature,
                                    SkillEntity = properties.SkillEntity,
                                }; 
                                Ecb.AppendToBuffer(sortKey, Factory, createBuffer);
                            }
                        }
                    }
                    else
                    {
                        //只有水平分裂的情况   
                        if (splitInfo.HorizCount > 1)
                        {
                            BulletHelper.SplitHoriz(splitInfo, properties, atkValue, properties.D1, transform.Position, Factory, Ecb, sortKey);
                        }
                        else
                        {
                            Debug.LogError("Bullet Split Error, 即没有水平也没有角度分裂, 怎么跑到SplitJob的?");
                        }
                    }

                    //分裂完的直接destroy掉
                    Ecb.SetComponentEnabled<BulletDestroyTag>(sortKey, entity, true);
                }
            }
        }
    }
}