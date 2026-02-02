using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BulletSystemGroup))]
    [UpdateAfter(typeof(BulletHitSystem))]
    public partial struct BulletBombSystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<DisableBulletHitTag> _disableBulletHitLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<BuffAppendRandomIds> _buffAppendBuffToBullet;
        [ReadOnly] private ComponentLookup<BulletBombTag> _bombLookup;
        [ReadOnly] private ComponentLookup<BulletDestroyTag> _destroyLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private ComponentLookup<BulletRepel> _bulletRepelLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CacheProperties>();

            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _bombLookup = state.GetComponentLookup<BulletBombTag>(true);
            _destroyLookup = state.GetComponentLookup<BulletDestroyTag>(true);
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _disableBulletHitLookup = state.GetComponentLookup<DisableBulletHitTag>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _buffAppendBuffToBullet = state.GetComponentLookup<BuffAppendRandomIds>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
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
            _creatureTag.Update(ref state);
            _deadLookup.Update(ref state);
            _localToWorldLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _disableBulletHitLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _buffAppendBuffToBullet.Update(ref state);
            _skillTagLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _bulletRepelLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);

            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            //爆炸
            new BulletBombJob
            {
                Ecb = ecb.AsParallelWriter(),
                GlobalEntity = global.Entity,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                SkillEntitiesLookup = _skillEntitiesLookup,
                CreatureTag = _creatureTag,
                DeadLookup = _deadLookup,
                TransformLookup = _localToWorldLookup,
                SummonLookup = _summonLookup,
                CollisionWorld = collisionWorld,
                DisableBulletHitLookup = _disableBulletHitLookup,
                BuffAppendBuffToBullet = _buffAppendBuffToBullet,
                DestroyLookup = _destroyLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                SkillTagLookup = _skillTagLookup,
                BulletRepelLookup = _bulletRepelLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct BulletBombJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity GlobalEntity;
            public Entity CacheEntity;

            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTag;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<DisableBulletHitTag> DisableBulletHitLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<BuffAppendRandomIds> BuffAppendBuffToBullet;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public ComponentLookup<BulletDestroyTag> DestroyLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public ComponentLookup<BulletRepel> BulletRepelLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;

            [BurstCompile]
            private void Execute(RefRW<BulletBombTag> tag, BulletProperties properties, BulletAtkValue atkValue, DynamicBuffer<BulletHitCreature> hitCreatures,
                RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetBulletConfig(properties.BulletId, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                if (DestroyLookup.IsComponentEnabled(entity))
                {
                    return;
                }

                if (tag.ValueRO.Frame > 0)
                {
                    tag.ValueRW.Frame -= 1;
                    return;
                }

                Ecb.SetComponentEnabled<BulletBombTag>(sortKey, entity, false);

                if (!TransformLookup.TryGetComponent(entity, out var localTransform))
                {
                    return;
                }

                //对范围内怪物伤害
                if (properties.BombRadius > 0)
                {
                    var hitEntities = PhysicsHelper.OverlapEnemies(properties.MasterCreature, CollisionWorld,
                        localTransform.Position, properties.BombRadius, CreatureTag, DeadLookup, properties.Team, config.HitSameTeam);

                    for (var i = 0; i < hitEntities.Length; i++)
                    {
                        var hitEntity = hitEntities[i];

                        //排除已经伤害过的
                        if (DisableBulletHitLookup.IsComponentEnabled(hitEntity))
                        {
                            continue;
                        }

                        //已经打过了
                        if (BulletHelper.CheckAlreadyHit(hitCreatures, hitEntity))
                        {
                            continue;
                        }

                        BulletHelper.HitCreature(properties, entity, BulletRepelLookup, GlobalEntity, CacheEntity, CacheLookup,
                            random, hitCreatures, Ecb, sortKey, hitEntity, true,
                            SkillEntitiesLookup, SkillTagLookup, BuffEntitiesLookup, BuffTagLookup, SummonLookup, BuffCommonLookup, BuffAppendBuffToBullet, AttrLookup, AttrModifyLookup);
                    }

                    hitEntities.Dispose();
                }

                //爆炸特效
                if (config.BombEffectId > 0)
                {
                    //爆炸特效
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                    {
                        ResourceId = config.BombEffectId,
                        Pos = localTransform.Position,
                        Scale = properties.BombRadius * 2f,
                        DelayDestroy = 2f,
                    });
                }

                //爆炸音效
                if (config.BombSound > 0)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new PlaySoundBuffer { SoundId = config.BombSound });
                }

                //相机震动
                if (config.BombShakeRadius > 0 && config.BombShakeTime > 0)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new PlayCameraShakeBuffer
                    {
                        Radius = config.BombShakeRadius,
                        Time = config.BombShakeTime,
                        Pos = localTransform.Position,
                    });
                }

                if (properties.AllowSkillEndAction && SkillTagLookup.TryGetComponent(properties.SkillEntity, out var _))
                {
                    var atk = new AtkValue(atkValue.Atk, atkValue.Crit, atkValue.CritDamage, properties.Team);
                    Ecb.AppendToBuffer(sortKey, properties.SkillEntity, new SkillEndBuffer(properties.ShootPos, localTransform.Position, Entity.Null, atk, properties.SkillEndActionIndex));
                    Ecb.SetComponentEnabled<SkillEndBuffer>(sortKey, properties.SkillEntity, true);
                }

                //触发技能的Trigger OnBulletExplosion
                SkillHelper.DoSkillTrigger(properties.MasterCreature, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnBulletExplosion)
                {
                    IntValue1 = config.Id,
                    IntValue2 = config.ClassId,
                    Pos = localTransform.Position,
                }, Ecb, sortKey);

                //destroy bullet
                Ecb.SetComponentEnabled<BulletDestroyTag>(sortKey, entity, true);
            }
        }
    }
}