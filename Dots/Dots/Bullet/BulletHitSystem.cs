using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletSplitSystem))]
    public partial struct BulletHitSystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<DisableBulletHitTag> _disableBulletHitLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<BuffAppendRandomIds> _buffAppendBuffToBullet;
        [ReadOnly] private ComponentLookup<BulletBombTag> _bombLookup;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private BufferLookup<BulletTriggerBuffer> _bulletTriggerBuffer;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<BulletCollisionTag> _bulletCollisionTagLookup;
        [ReadOnly] private ComponentLookup<BulletRepel> _bulletRepelLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTagLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CacheProperties>();
                
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _bombLookup = state.GetComponentLookup<BulletBombTag>(true);
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _creatureTagLookup = state.GetComponentLookup<CreatureTag>(true);
            _disableBulletHitLookup = state.GetComponentLookup<DisableBulletHitTag>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _buffAppendBuffToBullet = state.GetComponentLookup<BuffAppendRandomIds>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _bulletTriggerBuffer = state.GetBufferLookup<BulletTriggerBuffer>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _bulletCollisionTagLookup = state.GetComponentLookup<BulletCollisionTag>(true);
            _bulletRepelLookup = state.GetComponentLookup<BulletRepel>(true);
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

            _skillEntitiesLookup.Update(ref state);
            _bombLookup.Update(ref state);
            _destroyLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _creatureTagLookup.Update(ref state);
            _disableBulletHitLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _buffAppendBuffToBullet.Update(ref state);
            _skillTagLookup.Update(ref state);
            _bulletTriggerBuffer.Update(ref state);
            _cacheLookup.Update(ref state);
            _bulletCollisionTagLookup.Update(ref state);
            _bulletRepelLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            //Hit
            new BulletHitJob
            {
                Ecb = ecb.AsParallelWriter(),
                GlobalEntity = global.Entity,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                SkillEntitiesLookup = _skillEntitiesLookup,
                DeadLookup = _deadLookup,
                TransformLookup = _transformLookup,
                SummonLookup = _summonLookup,
                DisableBulletHitLookup = _disableBulletHitLookup,
                CollisionWorld = collisionWorld,
                BuffAppendBuffToBullet = _buffAppendBuffToBullet,
                BombLookup = _bombLookup,
                DestroyLookup = _destroyLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                SkillTagLookup = _skillTagLookup,
                BulletTriggerBuffer = _bulletTriggerBuffer,
                BulletCollisionTagLookup = _bulletCollisionTagLookup,
                BulletRepelLookup = _bulletRepelLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
                CreatureTagLookup = _creatureTagLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct BulletHitJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity GlobalEntity;
            public Entity CacheEntity;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTagLookup;
            [ReadOnly] public ComponentLookup<DisableBulletHitTag> DisableBulletHitLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<BuffAppendRandomIds> BuffAppendBuffToBullet;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public ComponentLookup<BulletBombTag> BombLookup;
            [ReadOnly] public ComponentLookup<BulletDestroyTag> DestroyLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public BufferLookup<BulletTriggerBuffer> BulletTriggerBuffer;
            [ReadOnly] public ComponentLookup<BulletCollisionTag> BulletCollisionTagLookup;
            [ReadOnly] public ComponentLookup<BulletRepel> BulletRepelLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            
            [BurstCompile]
            private void Execute(RefRW<BulletProperties> properties, RefRW<BulletAtkValue> atkValue, DynamicBuffer<BulletHitCreature> hitCreatures,
                RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetBulletConfig(properties.ValueRO.BulletId, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                if (BombLookup.IsComponentEnabled(entity) || DestroyLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                if (!TransformLookup.TryGetComponent(entity, out var localTransform))
                {
                    return;
                }
                
                //如果是直接命中目标，那么不需要飞行轨迹，命中后就直接销毁完事
                if (TransformLookup.HasComponent(properties.ValueRO.ForceHitCreature))
                {
                    BulletHelper.HitCreature(properties.ValueRO, entity, BulletRepelLookup, GlobalEntity, CacheEntity, CacheLookup, random, hitCreatures, Ecb, sortKey,
                        properties.ValueRO.ForceHitCreature, false, SkillEntitiesLookup, SkillTagLookup, BuffEntitiesLookup, BuffTagLookup,
                        SummonLookup, BuffCommonLookup, BuffAppendBuffToBullet, AttrLookup, AttrModifyLookup);

                    Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                    return;
                }
                
                //buff子弹
                if (properties.ValueRO.From == EBulletFrom.Buff)
                {
                    var hitEntity = properties.ValueRO.TransformParent;

                    if (!BulletHelper.CheckAlreadyHit(hitCreatures, hitEntity))
                    {
                        //hit trigger
                        if (BulletTriggerBuffer.HasBuffer(entity))
                        {
                            Ecb.AppendToBuffer(sortKey, entity, new BulletTriggerBuffer { Enemy = hitEntity, Type = EBulletBehaviourTrigger.OnHitEnemy });
                        }
                        BulletHelper.HitCreature(properties.ValueRO, entity, BulletRepelLookup, GlobalEntity, CacheEntity, CacheLookup, random, hitCreatures,
                            Ecb, sortKey, hitEntity, false, SkillEntitiesLookup, SkillTagLookup, BuffEntitiesLookup, BuffTagLookup,
                            SummonLookup, BuffCommonLookup, BuffAppendBuffToBullet, AttrLookup, AttrModifyLookup);
                    }
                    return;
                }

                var scale = localTransform.Value.Scale().x / properties.ValueRO.ResourceScale;
                var enemyLayer = PhysicsHelper.GetLayer(properties.ValueRO.Team, config.HitSameTeam);
                var filter = PhysicsLayers.GetFilter(enemyLayer);
                
                //碰撞子弹
                if (properties.ValueRO.From == EBulletFrom.Collision)
                {
                    var hitEntities = new NativeList<DistanceHit>(Allocator.Temp);
                    var checkRadius =  scale / 2f;

                    if (CollisionWorld.OverlapSphere(localTransform.Position, checkRadius * 3f, ref hitEntities, filter))
                    {
                        for (var i = 0; i < hitEntities.Length; i++)
                        {
                            var hitInfo = hitEntities[i];
                            var hitEntity = hitInfo.Entity;

                            if (SummonLookup.HasComponent(hitEntity) && TransformLookup.TryGetComponent(hitEntity, out var hitTransform))
                            {
                                var dist = math.distance(localTransform.Position, hitTransform.Position);
                                var validDist = checkRadius + hitTransform.Value.Scale().x / 2f;
                                if (dist <= validDist)
                                {
                                    if (!BulletHelper.CheckAlreadyHit(hitCreatures, hitEntity))
                                    {
                                        //hit trigger
                                        if (BulletTriggerBuffer.HasBuffer(entity))
                                        {
                                            Ecb.AppendToBuffer(sortKey, entity, new BulletTriggerBuffer { Enemy = hitEntity, Type = EBulletBehaviourTrigger.OnHitEnemy });
                                        }

                                        BulletHelper.HitCreature(properties.ValueRO, entity, BulletRepelLookup, GlobalEntity, CacheEntity, CacheLookup, random, hitCreatures,
                                            Ecb, sortKey, hitEntity, false, SkillEntitiesLookup, SkillTagLookup, BuffEntitiesLookup, BuffTagLookup,
                                            SummonLookup, BuffCommonLookup, BuffAppendBuffToBullet, AttrLookup, AttrModifyLookup);
                                    }
                                }
                            }
                        }
                    }
                    return;
                }

                //buff banHit (有buff则优先判断buff，不再判断config里的）
                var banHitBuffs = BuffHelper.GetBuffAttachInt(properties.ValueRO.MasterCreature, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.BulletChangeBanHit, config.Id, config.ClassId);
                if (banHitBuffs.Length > 0)
                {
                    foreach (var value in banHitBuffs)
                    {
                        if (value == 1)
                        {
                            return;
                        }
                    }

                    banHitBuffs.Dispose();
                }
                else
                {
                    if (config.BanHit)
                    {
                        return;
                    }
                }

                /*//判断障碍
                if (properties.CheckObstacle && PhysicsHelper.RayCast(CollisionWorld, PhysicsLayers.ObstacleAll, localTransform.Position, runningData.ValueRO.Direction, castDist, out var hitObstacle))
                {
                    //判断弹射
                    if (!BulletHelper.CheckBounce(entity, hitObstacle.SurfaceNormal, localTransform.Position, runningData, hitCreatures, Ecb, sortKey))
                    {
                        Ecb.AddComponent<BulletBombTag>(sortKey, entity);
                    }

                    //show hit effect
                    if (config.HitEffectId > 0)
                    {
                        Ecb.AppendToBuffer(sortKey, Global.Entity, new EffectCreateBuffer
                        {
                            ResourceId = config.HitEffectId,
                            Pos = localTransform.Position
                        });
                    }
                    return;
                }*/
             
                var checkDist = scale * 0.25f;
                if (checkDist > 100)
                {
                    checkDist = 100;
                }

                //查找是否碰到其他子弹
                var bulletCheckRay = new RaycastInput
                {
                    Start = localTransform.Position,
                    End = localTransform.Position + properties.ValueRO.RunningDirection * checkDist,
                    Filter = PhysicsLayers.GetFilter(PhysicsLayers.Bullet),
                };

                //如果其他子弹拥有消灭子弹的属性，则销毁自己
                //如果其他子弹用可反弹子弹，则反弹自己, 只吸收、反弹有速度的子弹（排除doodad）
                if (BulletHelper.CheckCanReflect(properties.ValueRO))
                {
                    if (CollisionWorld.CastRay(bulletCheckRay, out var hit))
                    {
                        if (BulletCollisionTagLookup.TryGetComponent(hit.Entity, out var hitTag) && hitTag.Team != properties.ValueRO.Team)
                        {
                            if (hitTag.CollisionWithBullet == ECollisionBulletType.DestroyEnemyBullet)
                            {
                                Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, true);
                                return;
                            }

                            if (hitTag.CollisionWithBullet is ECollisionBulletType.BounceEnemyBullet or ECollisionBulletType.BounceEnemyBullet_DontChangeTeam)
                            {
                                //隐形的子弹不反弹
                                if (config.ResourceId > 0)
                                {
                                    ReflectBullet(properties, atkValue, hit.SurfaceNormal, hitTag.Team, localTransform, hitTag.CollisionWithBullet == ECollisionBulletType.BounceEnemyBullet_DontChangeTeam);
                                }
                                return;
                            }
                        }
                    }
                }

                //完全静止的子弹，用overlap
                if (properties.ValueRO.V1 == 0 && properties.ValueRO.V2 == 0 && properties.ValueRO.VTurn == 0)
                {
                    var hitEntities = new NativeList<DistanceHit>(Allocator.Temp);
                    var checkRadius = properties.ValueRO.DamageShape == EDamageShape.Sector ? scale : scale / 2f;

                    if (CollisionWorld.OverlapSphere(localTransform.Position, checkRadius, ref hitEntities, filter))
                    {
                        for (var i = 0; i < hitEntities.Length; i++)
                        {
                            var hitInfo = hitEntities[i];
                            var hitEntity = hitInfo.Entity;

                            //扇形区域则判断角度
                            if (properties.ValueRO.DamageShape == EDamageShape.Sector)
                            {
                                if (TransformLookup.TryGetComponent(hitEntity, out var hitTransform))
                                {
                                    var hitForward = math.normalizesafe(hitTransform.Position - localTransform.Position);
                                    var hitAngle = MathHelper.Angle(properties.ValueRO.RunningDirection, hitForward);
                                    if (hitAngle > properties.ValueRO.DamageShapeParam / 2f)
                                    {
                                        continue;
                                    }
                                }
                            }

                            if (TryBulletHit(entity, hitEntity, hitInfo.SurfaceNormal, properties, atkValue, localTransform, config, random,
                                    GlobalEntity, CacheLookup, CacheEntity,
                                    SkillEntitiesLookup, SkillTagLookup, BulletRepelLookup, AttrLookup, AttrModifyLookup,
                                    BuffAppendBuffToBullet, BulletTriggerBuffer, hitCreatures, SummonLookup, CreatureTagLookup, DeadLookup,
                                    DisableBulletHitLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, Ecb, sortKey))
                            {
                                break;
                            }
                        }
                    }

                    hitEntities.Dispose();
                }
                else
                {
                    var hitEntities = new NativeList<ColliderCastHit>(Allocator.Temp);
                    if (CollisionWorld.SphereCastAll(localTransform.Position, checkDist, properties.ValueRO.RunningDirection, 0.1f, ref hitEntities, filter))
                    {
                        for (var i = 0; i < hitEntities.Length; i++)
                        {
                            var hitInfo = hitEntities[i];
                            var hitEntity = hitInfo.Entity;

                            if (TryBulletHit(entity, hitEntity,  hitInfo.SurfaceNormal, properties, atkValue, localTransform, config, random, 
                                    GlobalEntity, CacheLookup, CacheEntity,SkillEntitiesLookup, SkillTagLookup, BulletRepelLookup, AttrLookup, AttrModifyLookup,
                                    BuffAppendBuffToBullet, BulletTriggerBuffer, hitCreatures, SummonLookup, CreatureTagLookup,  DeadLookup, DisableBulletHitLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, Ecb, sortKey))
                            {
                                break;
                            }
                        }
                    }

                    hitEntities.Dispose();
                }
            }

            private static bool TryBulletHit(Entity bulletEntity, Entity hitEntity, float3 surfaceNormal, 
                RefRW<BulletProperties> properties, RefRW<BulletAtkValue> atkValue, LocalToWorld localTransform, 
                BulletConfig config, RefRW<RandomSeed> random,
                Entity globalEntity, ComponentLookup<CacheProperties> cacheLookup, Entity cacheEntity, 
                BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup,
                ComponentLookup<BulletRepel> bulletRepelLookup, ComponentLookup<PlayerAttrData> attrLookup, 
                BufferLookup<PlayerAttrModify> modifyLookup,
                ComponentLookup<BuffAppendRandomIds> buffAppendBuffToBullet, BufferLookup<BulletTriggerBuffer> bulletTriggerBuffer,
                DynamicBuffer<BulletHitCreature> hitCreatures, ComponentLookup<StatusSummon> summonLookup, 
                ComponentLookup<CreatureTag> creatureTagLookup, 
                ComponentLookup<InDeadState> deadLookup, ComponentLookup<DisableBulletHitTag> disableBulletHitLookup,
                BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup,
                ComponentLookup<BuffCommonData> buffCommonLookup, EntityCommandBuffer.ParallelWriter ecb, int sortKey)
            {

                if (!creatureTagLookup.TryGetComponent(hitEntity, out var hitCreature))
                {
                    return false;
                }
                
                if (deadLookup.IsComponentEnabled(hitEntity))
                {
                    return false;
                }

                //禁止Hit
                if (disableBulletHitLookup.IsComponentEnabled(hitEntity))
                {
                    return false;
                }

                if (!DamageHelper.CheckHitRule(config.HitSameTeam, 
                        properties.ValueRO.MasterCreature, properties.ValueRO.Team, hitEntity, hitCreature.TeamId))
                {
                    return false;
                }

                //看这个creature是否有反弹子弹的buff，如果有则反弹
                if (BulletHelper.CheckCanReflect(properties.ValueRO))
                {
                    if (BuffHelper.CheckBuffProb(hitEntity, summonLookup, random, EBuffType.ReflectBullet, buffEntitiesLookup, buffTagLookup, buffCommonLookup, out var attachInt))
                    {
                        //反弹子弹
                        var noChangeTeam = attachInt == 1;
                        ReflectBullet(properties, atkValue, surfaceNormal, hitCreature.TeamId, localTransform, noChangeTeam);
                        return true;
                    }
                }

                //已经打过了
                if (BulletHelper.CheckAlreadyHit(hitCreatures, hitEntity))
                {
                    return false;
                }

                //hit trigger
                if (bulletTriggerBuffer.HasBuffer(bulletEntity))
                {
                    ecb.AppendToBuffer(sortKey, bulletEntity, new BulletTriggerBuffer { Enemy = hitEntity, Type = EBulletBehaviourTrigger.OnHitEnemy });
                }
                
                BulletHelper.HitCreature(properties.ValueRO, bulletEntity, bulletRepelLookup, globalEntity, cacheEntity, cacheLookup, random, hitCreatures,
                    ecb, sortKey, hitEntity, false, skillEntitiesLookup, skillTagLookup, buffEntitiesLookup,
                    buffTagLookup, summonLookup, buffCommonLookup, buffAppendBuffToBullet, attrLookup, modifyLookup);

                //记录伤害次数
                properties.ValueRW.CurrDamageCount = properties.ValueRO.CurrDamageCount + 1;

                //判断伤害次数
                if (properties.ValueRO.MaxDamageCount > 0 && properties.ValueRO.CurrDamageCount >= properties.ValueRO.MaxDamageCount)
                {
                    //弹射判断
                    var velocityForward = properties.ValueRO.V1 < 0 ? -1 : 1;
                    var bounceForward = math.reflect(properties.ValueRO.RunningDirection, surfaceNormal) * velocityForward;
                    var bBounce = BulletHelper.TryBounce(bounceForward, localTransform.Position, properties);
                    if (bBounce)
                    {
                        if (MathHelper.IsValid(properties.ValueRO.D2))
                        {
                            properties.ValueRW.D2 = math.reflect(properties.ValueRW.D2, surfaceNormal);
                        }

                        //强制退出环绕
                        properties.ValueRW.AroundEntity = Entity.Null;
                        properties.ValueRW.AroundPos = float3.zero;

                        //触发弹射技能Trigger
                        SkillHelper.DoSkillTrigger(properties.ValueRO.MasterCreature, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnBulletBounce)
                        {
                            Forward = bounceForward,
                            Pos = localTransform.Position,
                            IntValue1 = config.Id,
                            IntValue2 = config.ClassId
                        }, ecb, sortKey);
                    }
                    else
                    {
                        ecb.SetComponentEnabled<BulletBombTag>(sortKey, bulletEntity, true);
                        return true;
                    }
                }

                return false;
            }

            private static void ReflectBullet(RefRW<BulletProperties> properties,RefRW<BulletAtkValue> atkValue, float3 surfaceNormal, ETeamId newTeam, LocalToWorld localTransform, bool dontChangeTeam)
            {
                var bounceForward = math.reflect(properties.ValueRO.RunningDirection, surfaceNormal);
                properties.ValueRW.D1 = bounceForward;
                properties.ValueRW.RunningDirection = bounceForward;
                properties.ValueRW.ShootPos = localTransform.Position;
                properties.ValueRW.RunningTime = 0;
                properties.ValueRW.Reflected = true;

                //防止子弹不销毁
                if (properties.ValueRO.ContTime <= 0)
                {
                    properties.ValueRW.ContTime = 5f;
                }

                if (!dontChangeTeam)
                {
                    atkValue.ValueRW = new BulletAtkValue
                    {
                        Atk = atkValue.ValueRO.Atk, 
                        Crit = atkValue.ValueRO.Crit, 
                        CritDamage = atkValue.ValueRO.CritDamage
                    };
                    properties.ValueRW.Team = newTeam;
                }
            }

            /*  类似闪电链的弹射算法备份
                
                var bounceTargets = PhysicsHelper.OverlapEnemies(CollisionWorld, localTransform.Position, properties.MaxDist, CreatureLookup, properties.AtkValue.Team);
                var bounceDirection = float3.zero;
                var nearDist = float.MaxValue;
                var bFind = false;
                for (var j = 0; j < bounceTargets.Length; j++)
                {
                    var bounce = bounceTargets[j];

                    //排除自己
                    if (bounce == hitEntity)
                    {
                        continue;
                    }

                    var hitPos = TransformLookup.GetRefRO(bounce).ValueRO.Position;
                    var dist = math.distancesq(hitPos, localTransform.Position);
                    if (dist < nearDist)
                    {
                        bFind = true;
                        nearDist = dist;
                        bounceDirection = math.normalizesafe(hitPos - localTransform.Position);
                    }
                }

                bounceTargets.Dispose();

                //射程内有目标，进行弹射
                if (bFind)
                {
                    runningData.ValueRW.BounceCount = runningData.ValueRO.BounceCount + 1;

                    //进行弹射：重置方向，起始坐标
                    runningData.ValueRW.Direction = bounceDirection;
                    runningData.ValueRW.StartPos = localTransform.Position;

                    //重置时间
                    runningData.ValueRW.RunningTime = 0;

                    //清空已击中的
                    hitCreatures.Clear();
                    break;
                }
            */
        }
    }
}