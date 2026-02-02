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
    [UpdateInGroup(typeof(SkillSystemGroup))]
    [UpdateAfter(typeof(SkillTriggerSystem))]
    public partial struct SkillTargetSystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<StatusHp> _hpLookup;
        [ReadOnly] private ComponentLookup<StatusForward> _forwardLookup;
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private ComponentLookup<DisableAutoTargetTag> _disableAutoTargetLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonsLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<MonsterTarget> _monsterTargetLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CacheProperties>();

            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _hpLookup = state.GetComponentLookup<StatusHp>(true);
            _forwardLookup = state.GetComponentLookup<StatusForward>(true);
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _disableAutoTargetLookup = state.GetComponentLookup<DisableAutoTargetTag>(true);
            _summonsLookup = state.GetBufferLookup<SummonEntities>(true);
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _monsterTargetLookup = state.GetComponentLookup<MonsterTarget>(true);
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
            _summonLookup.Update(ref state);
            _hpLookup.Update(ref state);
            _forwardLookup.Update(ref state);
            _creatureTag.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _disableAutoTargetLookup.Update(ref state);
            _summonsLookup.Update(ref state);
            _monsterLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _monsterTargetLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

            SystemAPI.TryGetSingletonEntity<LocalPlayerTag>(out var localPlayer);

            var deltaTime = SystemAPI.Time.DeltaTime;
            new SkillTargetJob
            {
                ScreenBound = global.ScreenBound,
                DeltaTime = deltaTime,
                CurrTime = global.Time,
                Ecb = ecb.AsParallelWriter(),
                LocalPlayerEntity = localPlayer,
                InMonsterPause = global.InMonsterPause,
                CollisionWorld = collisionWorld,
                SkillEntitiesLookup = _skillEntitiesLookup,
                SummonLookup = _summonLookup,
                HpLookup = _hpLookup, 
                ForwardLookup = _forwardLookup,
                CreatureTag = _creatureTag,
                DeadLookup = _deadLookup,
                TransformLookup = _transformLookup,
                DisableAutoTargetLookup = _disableAutoTargetLookup,
                SummonsLookup = _summonsLookup,
                MonsterLookup = _monsterLookup,
                SkillTagLookup = _skillTagLookup,
                CacheEntity = cacheEntity,
                CacheLookup = _cacheLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                MonsterTargetLookup = _monsterTargetLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct SkillTargetJob : IJobEntity
        {
            public float4 ScreenBound;
            public float DeltaTime;
            public float CurrTime;
            public Entity CacheEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public bool InMonsterPause;
            public Entity LocalPlayerEntity;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<StatusSummon> SummonLookup;
            [ReadOnly] public ComponentLookup<StatusHp> HpLookup;
            [ReadOnly] public ComponentLookup<StatusForward> ForwardLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTag;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<DisableAutoTargetTag> DisableAutoTargetLookup;
            [ReadOnly] public BufferLookup<SummonEntities> SummonsLookup;
            [ReadOnly] public ComponentLookup<MonsterProperties> MonsterLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<MonsterTarget> MonsterTargetLookup;

            [BurstCompile]
            private void Execute(DynamicBuffer<SkillTargetTags> tags, MasterCreature master, RefRW<SkillProperties> properties, RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetSkillConfig(properties.ValueRO.Id, CacheEntity, CacheLookup, out var config))
                {
                    return;
                }

                if (CreatureTag.TryGetComponent(master.Value, out var masterTag))
                {
                    if (InMonsterPause && masterTag.TeamId == ETeamId.Monster)
                    {
                        return;
                    }
                }

                var startInfo = properties.ValueRO.StartInfo;
                if (tags.Length > 0)
                {
                    //onCastSkill的Trigger
                    SkillHelper.DoSkillTrigger(master.Value, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnSkillCast)
                    {
                        IntValue1 = config.ClassId,
                        IntValue2 = config.Id,
                    }, Ecb, sortKey);
                }

                for (var t = tags.Length - 1; t >= 0; t--)
                {
                    var tagInfo = tags[t];
                    if (tagInfo.CastDelay > 0 && CurrTime - tagInfo.CreateTime < tagInfo.CastDelay)
                    {
                        continue;
                    }

                    //还在delay中，等delay时间结束
                    tags.RemoveAt(t);

                    var createTime = CurrTime;
                    var startPos = TransformLookup.TryGetComponent(startInfo.StartEntity, out var trans) ? trans.Position : startInfo.StartPos;
                    var tempEntities = new NativeList<Entity>(Allocator.Temp);

                    //看释放几次, 看是否需要叠加层数释放
                    int castCount;
                    if (tagInfo.ForceCastCount > 0)
                    {
                        castCount = tagInfo.ForceCastCount;
                    }
                    else
                    {
                        castCount = config.CastCount * properties.ValueRO.CurrLayer;

                        var addValue = BuffHelper.GetBuffAddValue(master.Value, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillCastCount, config.ClassId, config.Id);
                        castCount += (int)addValue;
                    }

                    for (var i = 0; i < castCount; i++)
                    {
                        var targetConfig = config.Target;
                        switch (targetConfig.Method)
                        {
                            //释放者自己
                            case ESkillTarget.Self:
                            {
                                if (TransformLookup.TryGetComponent(master.Value, out var masterTransform))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startInfo.StartPos, masterTransform.Position, master.Value, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }
                              
                                break;
                            }
                            //释放者的召唤物
                            case ESkillTarget.SelfSummon:
                            {
                                var monsterId = targetConfig.Param1.ToInt();
                                var bFindParent = targetConfig.Param2.ToInt() == 1;
                                var selfBackDist = targetConfig.Param3;
                                
                                
                                var findEntity = master.Value;
                                if (bFindParent)
                                {
                                    if (SummonLookup.TryGetComponent(master.Value, out var masterSummon))
                                    {
                                        findEntity = masterSummon.SummonParent;
                                    }
                                }

                                //遍历唤物
                                if (SummonsLookup.TryGetBuffer(findEntity, out var summonList))
                                {
                                    for (var j = 0; j < summonList.Length; j++)
                                    {
                                        var summon = summonList[j];
                                        if (MonsterLookup.TryGetComponent(summon.Value, out var monster))
                                        {
                                            if (monsterId == 0 || monsterId == monster.Id)
                                            {
                                                if (TransformLookup.TryGetComponent(summon.Value, out var summonParent))
                                                {
                                                    if (selfBackDist == 0)
                                                    {
                                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, summonParent.Position, summon.Value, createTime));
                                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                                    }
                                                    else
                                                    {
                                                        if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                                                        {
                                                            var dir = math.normalizesafe(summonParent.Position - masterTrans.Position);
                                                            var targetPos = masterTrans.Position - dir * selfBackDist;
                                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetPos, Entity.Null, createTime));
                                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                            //用当前枪械射出的子弹命中的敌人作为目标
                            case ESkillTarget.BulletHitCreature:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.BulletHitEnemy && TransformLookup.TryGetComponent(tagInfo.EntityValue, out var enemyTrans))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, enemyTrans.Position, tagInfo.EntityValue, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }
                            //用召唤物命中的敌人作为目标
                            case ESkillTarget.SummonHitCreature:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.SummonHitEnemy && TransformLookup.TryGetComponent(tagInfo.EntityValue, out var summonHitEnemyTrans))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, summonHitEnemyTrans.Position, tagInfo.EntityValue, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }

                            //用当前枪械射出的子弹命中的敌人作为目标
                            case ESkillTarget.AddedBuffEnemy:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.AddBuffToEnemy && TransformLookup.TryGetComponent(tagInfo.EntityValue, out var enemyTrans))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, enemyTrans.Position, tagInfo.EntityValue, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }
                            //用当前枪械射出的子弹命中的敌人作为目标
                            case ESkillTarget.KilledEnemy:
                            {
                                if ((tagInfo.TriggerType == ESkillTrigger.KillEnemy) && TransformLookup.TryGetComponent(tagInfo.EntityValue, out var enemyTrans))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, enemyTrans.Position, tagInfo.EntityValue, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }
                            case ESkillTarget.KilledEnemyNearestEnemy:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.KillEnemy && TransformLookup.TryGetComponent(tagInfo.EntityValue, out var enemyTrans))
                                {
                                    if (PhysicsHelper.GetNearestEnemy(master.Value, CollisionWorld, enemyTrans.Position, 10, CreatureTag, DeadLookup, DisableAutoTargetLookup, properties.ValueRO.AtkValue.Team, out var nearest))
                                    {
                                        if (TransformLookup.TryGetComponent(nearest, out var nearestTrans))
                                        {
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, nearestTrans.Position, nearest, createTime));
                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        }
                                    }
                                }

                                break;
                            }
                            case ESkillTarget.HitMeEnemy:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.OnHit && TransformLookup.TryGetComponent(tagInfo.EntityValue, out var enemyTrans))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, enemyTrans.Position, tagInfo.EntityValue, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }
                            case ESkillTarget.FarEnemyLR:
                            {
                                var maxDist = targetConfig.Param1;
                                var splitAngle = targetConfig.Param2;
                                var centerPos = startPos;

                                if (splitAngle <= 0)
                                {
                                    splitAngle = 30f;
                                }
                                
                                //找到第一个最远的, 第二个必须与第一个间隔角度>x
                                if (PhysicsHelper.GetFarEnemy(master.Value, CollisionWorld, centerPos, maxDist, CreatureTag, DeadLookup, DisableAutoTargetLookup, properties.ValueRO.AtkValue.Team, out var nearEntity))
                                {
                                    if (TransformLookup.TryGetComponent(nearEntity, out var nearTrans))
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, nearTrans.Position, nearEntity, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        
                                        //over lap 第二个
                                        var enemies = PhysicsHelper.OverlapEnemies(master.Value, CollisionWorld, centerPos, maxDist, CreatureTag, DeadLookup, properties.ValueRO.AtkValue.Team);
                                        var farEnemy = Entity.Null;
                                        var farEnemyPos = float3.zero;
                                        var bFind2 = false;
                                        var maxFlag = 0f;
                                        
                                        var firstDir = math.normalizesafe(nearTrans.Position - centerPos);
                                        foreach (var enemy in enemies)
                                        {
                                            if (TransformLookup.TryGetComponent(enemy, out var enemyPos))
                                            {
                                                var dir =  math.normalizesafe(enemyPos.Position - centerPos);
                                                var ang = MathHelper.Angle(firstDir, dir);
                                                if (ang > splitAngle)
                                                {
                                                    var dist = math.distance(centerPos, enemyPos.Position);
                                                    if (dist > maxFlag)
                                                    {
                                                        maxFlag = dist;
                                                        bFind2 = true;
                                                        farEnemy = enemy;
                                                        farEnemyPos = enemyPos.Position;
                                                    }
                                                }
                                            }
                                        }

                                        if (bFind2)
                                        {
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, farEnemyPos, farEnemy, createTime));
                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        }
                                        else
                                        {
                                            var newDir = MathHelper.RotateForward(firstDir, 5);
                                            var pos = centerPos + newDir * math.distance(nearTrans.Position, centerPos);
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, pos, Entity.Null, createTime));
                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        }
                                        enemies.Dispose();
                                    }
                                }
                                
                                break;
                            }
                            case ESkillTarget.FarEnemy:
                            {
                                var maxDist = targetConfig.Param1;
                                var useMasterCenter = targetConfig.Param2.ToInt() == 1;

                                var centerPos = startPos;
                                if (useMasterCenter)
                                {
                                    if (SummonLookup.TryGetComponent(master.Value, out var masterSummon) &&
                                        TransformLookup.TryGetComponent(masterSummon.SummonParent, out var parentTrans))
                                    {
                                        centerPos = parentTrans.Position;
                                    }
                                }

                                //找到目标了
                                if (PhysicsHelper.GetFarEnemy(master.Value, CollisionWorld, centerPos, maxDist, CreatureTag, DeadLookup, DisableAutoTargetLookup, properties.ValueRO.AtkValue.Team, out var nearEntity))
                                {
                                    if (TransformLookup.HasComponent(nearEntity))
                                    {
                                        var pos = TransformLookup.GetRefRO(nearEntity).ValueRO.Position;
                                        var target = (int)targetConfig.Param2 == 1 ? Entity.Null : nearEntity;

                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, pos, target, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            //最近的敌人
                            case ESkillTarget.NearEnemy:
                            {
                                var maxDist = targetConfig.Param1;
                                var useMasterCenter = targetConfig.Param2.ToInt() == 1;

                                var targetEntity = GetNearEnemy(master.Value, config, properties.ValueRO, CollisionWorld, startPos, maxDist, useMasterCenter,
                                    SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, TransformLookup, CreatureTag, DeadLookup, DisableAutoTargetLookup);

                                if (TransformLookup.TryGetComponent(targetEntity, out var targetTrans))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetTrans.Position, targetEntity, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }
                            //最近的敌人
                            case ESkillTarget.NearEnemyCircle:
                            {
                                var maxDist = targetConfig.Param1;
                                var radiusMin = targetConfig.Param2;
                                var radiusMax = targetConfig.Param3;
                                var count = targetConfig.Param4.ToInt();
                                if (count <= 0) count = 1;

                                var addValue = BuffHelper.GetBuffAddValue(master.Value, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillTargetCount, config.ClassId, config.Id);
                                count += (int)addValue;

                                //找到目标了
                                if (PhysicsHelper.GetNearestEnemy(master.Value, CollisionWorld, startPos, maxDist, CreatureTag, DeadLookup, DisableAutoTargetLookup, properties.ValueRO.AtkValue.Team, out var nearEntity))
                                {
                                    if (TransformLookup.TryGetComponent(nearEntity, out var enemyTrans))
                                    {
                                        var centerPos = enemyTrans.Position;
                                        for (var j = 0; j < count; j++)
                                        {
                                            var angle = 360f / count * j;
                                            var randomRadius = random.ValueRW.Value.NextFloat(radiusMin, radiusMax);
                                            var pos = centerPos + MathHelper.GetCirclePos(angle, randomRadius);
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, pos, Entity.Null, createTime));
                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        }
                                    }
                                }

                                break;
                            }
                            case ESkillTarget.NearEnemyRandomPoint:
                            {
                                var maxDist = targetConfig.Param1;
                                var min = targetConfig.Param2;
                                var max = targetConfig.Param3;

                                //防错
                                if (min == 0 && max == 0)
                                {
                                    max = 0.01f;
                                }

                                //找到目标了
                                if (PhysicsHelper.GetNearestEnemy(master.Value, CollisionWorld, startPos, maxDist, CreatureTag, DeadLookup, DisableAutoTargetLookup, properties.ValueRO.AtkValue.Team, out var nearEntity))
                                {
                                    if (TransformLookup.TryGetComponent(nearEntity, out var enemyTrans))
                                    {
                                        var pos = enemyTrans.Position;

                                        //随机一个方向
                                        var angle = random.ValueRW.Value.NextFloat(0f, 360f);
                                        var forward = MathHelper.Angle2Forward(angle);
                                        var randomDist = random.ValueRW.Value.NextFloat(min, max);
                                        var randomPos = pos + randomDist * forward;

                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, randomPos, Entity.Null, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            //范围内随机敌人
                            case ESkillTarget.RandomEnemy:
                            {
                                var min = targetConfig.Param1;
                                var max = targetConfig.Param2;
                                var disableSame = targetConfig.Param3.ToInt() == 1;
                                var forcePos = targetConfig.Param4.ToInt() == 1;
                                var randomRange = targetConfig.Param5;
                                var useMasterCenter = targetConfig.Param6.ToInt() == 1;
                                var noExistBuffType = targetConfig.Param7.ToInt();
                                var noEnemyUsePoint = targetConfig.Param8.ToInt() == 1;

                                //防错
                                if (min == 0 && max == 0)
                                {
                                    max = 0.01f;
                                }

                                var centerPos = startPos;
                                if (useMasterCenter)
                                {
                                    if (SummonLookup.TryGetComponent(master.Value, out var masterSummon) &&
                                        TransformLookup.TryGetComponent(masterSummon.SummonParent, out var parentTrans))
                                    {
                                        centerPos = parentTrans.Position;
                                    }
                                }

                                //如果多次随机则需要不相同
                                var enemies = PhysicsHelper.OverlapEnemies(master.Value, CollisionWorld, centerPos, max, CreatureTag, DeadLookup, properties.ValueRO.AtkValue.Team);
                                var randomEnemies = new NativeList<Entity>(Allocator.Temp);

                                var totalCount = 0;
                                var temp = Entity.Null;

                                for (var e = 0; e < enemies.Length; e++)
                                {
                                    var enemy = enemies[e];
                                    if (!TransformLookup.HasComponent(enemy))
                                    {
                                        continue;
                                    }

                                    if (DisableAutoTargetLookup.HasComponent(enemy) && DisableAutoTargetLookup.IsComponentEnabled(enemy))
                                    {
                                        continue;
                                    }

                                    //是否必须不存在某个buff
                                    if (noExistBuffType != 0)
                                    {
                                        if (BuffHelper.CheckHasBuffType((EBuffType)noExistBuffType, enemy, BuffEntitiesLookup, BuffTagLookup))
                                        {
                                            continue;
                                        }
                                    }

                                    var enemyPos = TransformLookup.GetRefRO(enemy).ValueRO.Position;
                                    if (math.distancesq(enemyPos, centerPos) > min * min)
                                    {
                                        //记录一个允许重复的
                                        temp = enemy;

                                        //排除已经有的
                                        var bFind = false;
                                        for (var j = 0; j < tempEntities.Length; j++)
                                        {
                                            if (tempEntities[j] == enemy)
                                            {
                                                bFind = true;
                                                break;
                                            }
                                        }

                                        if (!bFind)
                                        {
                                            randomEnemies.Add(enemy);
                                            totalCount++;
                                        }
                                    }
                                }

                                if (!disableSame)
                                {
                                    if (randomEnemies.Length == 0 && temp != Entity.Null)
                                    {
                                        randomEnemies.Add(temp);
                                        totalCount++;
                                    }
                                }

                                enemies.Dispose();

                                if (totalCount > 0)
                                {
                                    var idx = random.ValueRW.Value.NextInt(0, totalCount);
                                    var target = randomEnemies[idx];
                                    if (TransformLookup.TryGetComponent(target, out var targetTransform))
                                    {
                                        tempEntities.Add(target);

                                        if (!forcePos)
                                        {
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetTransform.Position, target, createTime));
                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        }
                                        else
                                        {
                                            var targetPos = MathHelper.RandomRangePos(random, targetTransform.Position, -randomRange, randomRange);
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetPos, Entity.Null, createTime));
                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogError($"TargetMethod RandomEnemy 随出来的目标没有Transform?");
                                    }
                                }
                                else
                                {
                                    if (noEnemyUsePoint)
                                    {
                                        //随机一个方向
                                        var angle = random.ValueRW.Value.NextFloat(0f, 360f);
                                        var forward = MathHelper.Angle2Forward(angle);

                                        var randomDist = random.ValueRW.Value.NextFloat(min, max);
                                        var randomPos = startPos + randomDist * forward;

                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, randomPos, Entity.Null, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }
                                randomEnemies.Dispose();
                                break;
                            }
                            case ESkillTarget.MinHpEnemy:
                            case ESkillTarget.MaxHpEnemy:
                            {
                                var min = targetConfig.Param1;
                                var max = targetConfig.Param2;
                                var useMasterCenter = targetConfig.Param3.ToInt() == 1;

                                //防错
                                if (min == 0 && max == 0)
                                {
                                    max = 0.01f;
                                }

                                var targetEntity = GetMaxMinHpEnemy(targetConfig.Method == ESkillTarget.MaxHpEnemy,
                                    master.Value, config, properties.ValueRO, 
                                    CollisionWorld, startPos, min, max, useMasterCenter, 
                                    SummonLookup, HpLookup, BuffEntitiesLookup, BuffTagLookup,
                                    BuffCommonLookup, TransformLookup, CreatureTag,
                                    DeadLookup, DisableAutoTargetLookup, random);

                                if (TransformLookup.TryGetComponent(targetEntity, out var targetTransform))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetTransform.Position, targetEntity, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }
                            //范围内随机Pos
                            case ESkillTarget.RandomPoint:
                            {
                                var min = targetConfig.Param1;
                                var max = targetConfig.Param2;

                                //防错
                                if (min == 0 && max == 0)
                                {
                                    max = 0.01f;
                                }

                                //随机一个方向
                                var angle = random.ValueRW.Value.NextFloat(0f, 360f);
                                var forward = MathHelper.Angle2Forward(angle);

                                var randomDist = random.ValueRW.Value.NextFloat(min, max);
                                var randomPos = startPos + randomDist * forward;

                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, randomPos, Entity.Null, createTime));
                                Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);

                                break;
                            }
                            case ESkillTarget.UpLRCounter:
                            {
                                var upDist = targetConfig.Param1;
                                var lrDist = targetConfig.Param2;
                                var count = tagInfo.Counter;
                                
                                var newStartPos = startPos + MathHelper.Up * upDist;
                                var isL = (count) % 2 == 0;
                                var dist = Mathf.Ceil((count)  / 2f) * lrDist;

                                var lrFwd = MathHelper.RotateForward(MathHelper.Up, isL ? -90f : 90f);
                                var targetPos = newStartPos + lrFwd * dist;
                                
                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetPos, Entity.Null, createTime));
                                Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                break;
                            }
                            case ESkillTarget.Up:
                            {
                                var dist = targetConfig.Param1;
                                var horizRange  = targetConfig.Param2;
                                
                                //防错
                                if (dist <= 0)
                                {
                                    dist = 0.5f;
                                }

                                var forward = MathHelper.Up;
                                var newStartPos = startPos;
                                var offset = 0f;
                                float3 targetPos;
                                
                                if (horizRange > 0)
                                {
                                    var leftForward = MathHelper.RotateForward(forward, -90f);
                                    offset = random.ValueRW.Value.NextFloat(-horizRange, horizRange);
                                    newStartPos = startPos + leftForward * offset;
                                    targetPos = newStartPos + forward * dist;
                                    properties.ValueRW.StartInfo.StartPos = newStartPos;
                                }
                                else
                                {
                                    targetPos = startPos + forward * dist;
                                }

                                Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(newStartPos, targetPos, Entity.Null, createTime)
                                {
                                    Param1 = offset
                                });
                                Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                break;
                            }
                            //屏幕方向
                            case ESkillTarget.ScreenForward:
                            {
                                var min = targetConfig.Param1;
                                var max = targetConfig.Param2;
                                var initAngle = targetConfig.Param3;
                                var minAngle = targetConfig.Param4;
                                var maxAngle = targetConfig.Param5;
                                var totalCount = targetConfig.Param6.ToInt();
                                var banPlayerForward = targetConfig.Param7.ToInt() == 1;

                                //防错
                                if (min <= 0)
                                {
                                    min = 0.2f;
                                }

                                if (max < min)
                                {
                                    max = min;
                                }

                                if (ForwardLookup.TryGetComponent(master.Value, out var masterForward))
                                {
                                    if (totalCount <= 0)
                                    {
                                        totalCount = 1;
                                    }

                                    var addValue = BuffHelper.GetBuffAddValue(master.Value, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillTargetCount, config.ClassId, config.Id);
                                    totalCount += (int)addValue;


                                    var angleOffset = banPlayerForward ? 0f : 90f;// (masterForward.LookingLeft ? 270 : 90f);
                                    var startAngle = (initAngle + angleOffset) % 360f;
                                    for (var j = 0; j < totalCount; j++)
                                    {
                                        var angleResult = startAngle + (360f / totalCount) * j;

                                        //随机角度
                                        var randAngle = random.ValueRW.Value.NextFloat(minAngle, maxAngle);
                                        angleResult += randAngle;

                                        var forward = MathHelper.Angle2Forward(angleResult);
                                        var randomDist = random.ValueRW.Value.NextFloat(min, max);
                                        var randomPos = startPos + randomDist * forward;
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, randomPos, Entity.Null, createTime)
                                        {
                                            Forward = masterForward.FaceForward
                                        });
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Skill Target Error, no Component CreatureForward");
                                }

                                break;
                            }
                            case ESkillTarget.ScreenEdge:
                            {
                                var side = (ESide)targetConfig.Param1.ToInt();
                                var offset = targetConfig.Param2;
                                var count = targetConfig.Param3.ToInt();
                                var countSplit = targetConfig.Param4.ToInt();

                                if (count <= 0)
                                {
                                    count = 1;
                                }

                                var screenWidth = math.abs(ScreenBound.z - ScreenBound.x);
                                var screenHeight = math.abs(ScreenBound.w - ScreenBound.y);

                                float3 edgePos;
                                switch (side)
                                {
                                    case ESide.Down:
                                        edgePos = new float3(ScreenBound.x + screenWidth / 2f, ScreenBound.y, 0);
                                        edgePos.x += offset;
                                        break;
                                    case ESide.Up:
                                        edgePos = new float3(ScreenBound.x + screenWidth / 2f, ScreenBound.w, 0);
                                        edgePos.x += offset;
                                        break;
                                    case ESide.Left:
                                        edgePos = new float3(ScreenBound.x, ScreenBound.y + screenHeight / 2f, 0);
                                        edgePos.y += offset;
                                        break;
                                    default:
                                        edgePos = new float3(ScreenBound.z, ScreenBound.y + screenHeight / 2f, 0);
                                        edgePos.y += offset;
                                        break;
                                }

                                for (var c = 0; c < count; c++)
                                {
                                    if (side == ESide.Down || side == ESide.Up)
                                    {
                                        var pos = new float3(edgePos.x + c * countSplit, edgePos.y, 0);
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, pos, Entity.Null, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                    else
                                    {
                                        var pos = new float3(edgePos.x, edgePos.y + c * countSplit, 0);
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, pos, Entity.Null, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            //选择释放者前方X米的坐标作为目标。
                            case ESkillTarget.SelfMoveForward:
                            case ESkillTarget.SelfFaceForward:
                            case ESkillTarget.SelfBackForward:
                            case ESkillTarget.SelfLeftForward:
                            case ESkillTarget.SelfRightForward:
                            {
                                var min = targetConfig.Param1;
                                var max = targetConfig.Param2;
                                var randomOffset = targetConfig.Param3;

                                if (min == 0 && max == 0)
                                {
                                    max = 0.01f;
                                }

                                if (ForwardLookup.TryGetComponent(master.Value, out var masterForward))
                                {
                                    var randomDist = random.ValueRW.Value.NextFloat(min, max);
                                    var forward = targetConfig.Method == ESkillTarget.SelfMoveForward ? masterForward.MoveForward : masterForward.FaceForward;
                                    switch (targetConfig.Method)
                                    {
                                        case ESkillTarget.SelfBackForward:
                                            forward = -forward;
                                            break;
                                        case ESkillTarget.SelfLeftForward:
                                            forward = MathHelper.RotateForward(forward, 90f);
                                            break;
                                        case ESkillTarget.SelfRightForward:
                                            forward = MathHelper.RotateForward(forward, -90f);
                                            break;
                                    }

                                    //修改startPos
                                    var newStartPos = startPos;
                                    float3 targetPos;
                                    if (randomOffset > 0)
                                    {
                                        var leftForward = MathHelper.RotateForward(forward, -90f);
                                        var offset = random.ValueRW.Value.NextFloat(-randomOffset, randomOffset);
                                        newStartPos = startPos + leftForward * offset;
                                        targetPos = newStartPos + forward * randomDist;
                                        properties.ValueRW.StartInfo.StartPos = newStartPos;
                                    }
                                    else
                                    {
                                        targetPos = startPos + forward * randomDist;
                                    }


                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(newStartPos, targetPos, Entity.Null, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }
                                else
                                {
                                    Debug.LogError("Skill Target Error, no Component CreatureForward");
                                }

                                break;
                            }
                            //选择上一个技能的目标
                            case ESkillTarget.PrevTarget:
                            {
                                if (properties.ValueRO.PrevTarget.Enable)
                                {
                                    //是否仅选择坐标点
                                    float3 targetPos;
                                    Entity target;

                                    if (TransformLookup.TryGetComponent(properties.ValueRO.PrevTarget.Target, out var targetTrans))
                                    {
                                        targetPos = targetTrans.Position;
                                        target = (int)targetConfig.Param1 == 1 ? Entity.Null : properties.ValueRO.PrevTarget.Target;
                                    }
                                    else
                                    {
                                        target = Entity.Null;
                                        targetPos = properties.ValueRO.PrevTarget.Pos;
                                    }

                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetPos, target, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }
                                else
                                {
                                    Debug.LogError($"PrevTarget技能API,找不到PrevTarget, 确认上一个技能是否有有效目标:{config.Id}");
                                }

                                break;
                            }
                            //子弹弹射目标
                            case ESkillTarget.BulletBouncePos:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.OnBulletBounce)
                                {
                                    if (MathHelper.IsValid(tagInfo.PosValue))
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, tagInfo.PosValue, Entity.Null, createTime)
                                        {
                                            Forward = tagInfo.Forward
                                        });
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            //子弹爆炸
                            case ESkillTarget.BulletExplosionPos:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.OnBulletExplosion)
                                {
                                    if (MathHelper.IsValid(tagInfo.PosValue))
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, tagInfo.PosValue, Entity.Null, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            case ESkillTarget.CritPos:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.OnCrit)
                                {
                                    if (MathHelper.IsValid(tagInfo.PosValue))
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, tagInfo.PosValue, Entity.Null, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            case ESkillTarget.LocalPlayer:
                            {
                                if (TransformLookup.TryGetComponent(LocalPlayerEntity, out var playerTransform))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, playerTransform.Position, LocalPlayerEntity, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }

                            //召唤物的master
                            case ESkillTarget.SummonMaster:
                            {
                                if (SummonLookup.TryGetComponent(master.Value, out var masterSummon) && 
                                    TransformLookup.TryGetComponent(masterSummon.SummonParent, out var summonParentTrans))
                                {
                                    Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, summonParentTrans.Position, masterSummon.SummonParent, createTime));
                                    Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                }

                                break;
                            }
                            case ESkillTarget.BornPosCircle:
                            case ESkillTarget.SummonMasterCircle:
                            case ESkillTarget.SelfCircle:
                            {
                                var minDist = targetConfig.Param1;
                                var maxDist = targetConfig.Param2;
                                var count = targetConfig.Param3.ToInt();
                                var yOffset = targetConfig.Param4;
                                
                                if (count <= 0) count = 1;
                                if (maxDist < minDist) maxDist = minDist;

                                var addValue = BuffHelper.GetBuffAddValue(master.Value, SummonLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillTargetCount, config.ClassId, config.Id);
                                count += (int)addValue;

                                var bValid = false;
                                var centerPos = float3.zero;

                                if (targetConfig.Method == ESkillTarget.BornPosCircle)
                                {
                                  
                                    if (MonsterLookup.TryGetComponent(master.Value, out var monster))
                                    {
                                        bValid = true;
                                        centerPos = monster.BornPos;
                                        centerPos.y += yOffset;
                                    }
                                }
                                else
                                {
                                    var targetEntity = Entity.Null;
                                    if (targetConfig.Method == ESkillTarget.SummonMasterCircle)
                                    {
                                        if (SummonLookup.TryGetComponent(master.Value, out var summonMaster))
                                        {
                                            targetEntity = summonMaster.SummonParent;
                                        }
                                    }
                                    else
                                    {
                                        targetEntity = master.Value;
                                    }
                                    
                                    if (TransformLookup.TryGetComponent(targetEntity, out var entityTrans))
                                    {
                                        bValid = true;
                                        centerPos = entityTrans.Position;
                                    }
                                }

                                if (bValid)
                                {
                                    //以主角为圆心，向外偏移N米的N个坐标点
                                    var angleOffset = 360f / count;
                                    for (var angle = 0f; angle < 360f; angle += angleOffset)
                                    {
                                        var dist = random.ValueRW.Value.NextFloat(minDist, maxDist);
                                        var pos = centerPos + MathHelper.GetCirclePos(angle, dist);
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, pos, Entity.Null, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            case ESkillTarget.HatredEnemy:
                            {
                                var usePos = targetConfig.Param1.ToInt() == 1;
                                var min = targetConfig.Param2;
                                var max = targetConfig.Param3;
                                if (MonsterTargetLookup.TryGetComponent(master.Value, out var monsterTarget))
                                {
                                    if (TransformLookup.TryGetComponent(monsterTarget.HatredEnemy, out var hatredTrans))
                                    {
                                        var targetPos = hatredTrans.Position;
                                        var targetEntity = usePos ? Entity.Null : monsterTarget.HatredEnemy;
                                        if (usePos)
                                        {
                                            targetPos = MathHelper.RandomRangePos(random, targetPos, min, max);
                                        }

                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetPos, targetEntity, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            case ESkillTarget.HatredEnemyBack:
                            {
                                var min = targetConfig.Param1;
                                var max = targetConfig.Param2;

                                if (ForwardLookup.TryGetComponent(master.Value, out var masterForward))
                                {
                                    if (MonsterTargetLookup.TryGetComponent(master.Value, out var monsterTarget))
                                    {
                                        if (TransformLookup.TryGetComponent(monsterTarget.HatredEnemy, out var hatredTrans) && SummonLookup.TryGetComponent(monsterTarget.HatredEnemy, out var hatredCreature))
                                        {
                                            var targetPos = hatredTrans.Position;
                                            targetPos += -masterForward.MoveForward * random.ValueRW.Value.NextFloat(min, max);
                                            Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, targetPos, Entity.Null, createTime));
                                            Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Skill Target Error, no Component CreatureForward");
                                }

                                break;
                            }
                            case ESkillTarget.SummonedMonster:
                            {
                                if (tagInfo.TriggerType == ESkillTrigger.OnSummonMonster)
                                {
                                    if (TransformLookup.TryGetComponent(tagInfo.EntityValue, out var summonTrans))
                                    {
                                        Ecb.AppendToBuffer(sortKey, entity, new SkillTargetBuffer(startPos, summonTrans.Position, tagInfo.EntityValue, createTime));
                                        Ecb.SetComponentEnabled<SkillTargetBuffer>(sortKey, entity, true);
                                    }
                                }

                                break;
                            }
                            //暴击时
                            case ESkillTarget.None:
                            {
                                Debug.LogError($"SkillTarget沒配置, id:{properties.ValueRO.Id}");
                                break;
                            }
                            default:
                            {
                                Debug.LogError($"配置了不存在的SkillTarget:{targetConfig.Method} skillId:{properties.ValueRO.Id}");
                                break;
                            }
                        }
                    }

                    //记录生效次数
                    var triggeredCount = properties.ValueRO.TriggeredCount + 1;
                    properties.ValueRW.TriggeredCount = triggeredCount;
                    if (config.MaxTriggerCount > 0 && triggeredCount >= config.MaxTriggerCount)
                    {
                        properties.ValueRW.IsOver = true;
                    }

                    tempEntities.Dispose();
                }

                //disable tag
                Ecb.SetComponentEnabled<SkillTargetTags>(sortKey, entity, tags.Length > 0);
            }

            private static Entity GetNearEnemy(Entity master, SkillConfig config, SkillProperties properties, CollisionWorld collisionWorld, float3 startPos, float max, bool useMasterCenter,
                ComponentLookup<StatusSummon> summonLookup, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
                ComponentLookup<LocalToWorld> transformLookup, ComponentLookup<CreatureTag> creatureTag, ComponentLookup<InDeadState> deadLookup, ComponentLookup<DisableAutoTargetTag> disableAutoTargetLookup)
            {
                var centerPos = startPos;
                if (useMasterCenter)
                {
                    if (summonLookup.TryGetComponent(master, out var masterCreature))
                    {
                        if (transformLookup.TryGetComponent(masterCreature.SummonParent, out var parentTrans))
                        {
                            centerPos = parentTrans.Position;
                        }
                    }
                }

                //找到目标了
                if (PhysicsHelper.GetNearestEnemy(master, collisionWorld, centerPos, max, creatureTag, deadLookup, disableAutoTargetLookup, properties.AtkValue.Team, out var nearEntity))
                {
                    return nearEntity;
                }

                return Entity.Null;
            }


            private static Entity GetMaxMinHpEnemy(bool isMaxHp, Entity master, SkillConfig config, SkillProperties properties, 
                CollisionWorld collisionWorld, float3 startPos, float min, float max, bool useMasterCenter,
                ComponentLookup<StatusSummon> summonLookup, 
                ComponentLookup<StatusHp> hpLookup, 
                BufferLookup<BuffEntities> buffEntitiesLookup, 
                ComponentLookup<BuffTag> buffTagLookup, 
                ComponentLookup<BuffCommonData> buffCommonLookup,
                ComponentLookup<LocalToWorld> transformLookup, 
                ComponentLookup<CreatureTag> creatureTag,
                ComponentLookup<InDeadState> deadLookup,
                ComponentLookup<DisableAutoTargetTag> disableAutoTargetLookup,
                RefRW<RandomSeed> random)
            {
                var centerPos = startPos;
                if (useMasterCenter)
                {
                    if (summonLookup.TryGetComponent(master, out var masterCreature))
                    {
                        if (transformLookup.TryGetComponent(masterCreature.SummonParent, out var parentTrans))
                        {
                            centerPos = parentTrans.Position;
                        }
                    }
                }

                //如果多次随机则需要不相同
                var enemies = PhysicsHelper.OverlapEnemies(master, collisionWorld, centerPos, max, creatureTag, deadLookup, properties.AtkValue.Team);
                var randomEnemies = new NativeList<Entity>(Allocator.Temp);
                var maxHpFlag = float.MinValue;
                var minHpFlag = float.MaxValue;

                for (var e = 0; e < enemies.Length; e++)
                {
                    var enemy = enemies[e];
                    if (!transformLookup.HasComponent(enemy))
                    {
                        continue;
                    }

                    if (disableAutoTargetLookup.HasComponent(enemy) && disableAutoTargetLookup.IsComponentEnabled(enemy))
                    {
                        continue;
                    }

                    var enemyPos = transformLookup.GetRefRO(enemy).ValueRO.Position;
                    if (math.distancesq(enemyPos, centerPos) > min * min)
                    {
                        if (hpLookup.TryGetComponent(enemy, out var enemyHp))
                        {
                            if (isMaxHp)
                            {
                                if (enemyHp.CurHp > maxHpFlag)
                                {
                                    maxHpFlag = enemyHp.CurHp;
                                    randomEnemies.Clear();
                                    randomEnemies.Add(enemy);
                                }
                                else if (Mathf.Approximately(enemyHp.CurHp, minHpFlag))
                                {
                                    randomEnemies.Add(enemy);
                                }
                            }
                            else
                            {
                                if (enemyHp.CurHp < minHpFlag)
                                {
                                    minHpFlag = enemyHp.CurHp;
                                    randomEnemies.Clear();
                                    randomEnemies.Add(enemy);
                                }
                                else if (Mathf.Approximately(enemyHp.CurHp, minHpFlag))
                                {
                                    randomEnemies.Add(enemy);
                                }
                            }
                        }
                    }
                }

                enemies.Dispose();

                if (randomEnemies.Length > 0)
                {
                    var idx = random.ValueRW.Value.NextInt(0, randomEnemies.Length);
                    var target = randomEnemies[idx];
                    randomEnemies.Dispose();
                    return target;
                }

                randomEnemies.Dispose();
                return Entity.Null;
            }
        }
    }
}