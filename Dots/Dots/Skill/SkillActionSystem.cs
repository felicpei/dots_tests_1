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
    [UpdateAfter(typeof(SkillCastSystem))]
    public partial struct SkillActionSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalPlayerTag> _localPlayerLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<CreatureForward> _creatureForwardLookup;
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;
        [ReadOnly] private ComponentLookup<ShieldProperties> _shieldLookup;
        [ReadOnly] private ComponentLookup<MonsterMove> _monsterMoveLookup;
        [ReadOnly] private BufferLookup<Child> _childLookup;
        [ReadOnly] private BufferLookup<SummonEntities> _summonEntitiesLookup;
        [ReadOnly] private ComponentLookup<EffectProperties> _effectLookup;
        [ReadOnly] private ComponentLookup<MonsterProperties> _monsterLookup;
        [ReadOnly] private ComponentLookup<CreatureMuzzlePos> _muzzlePosLookup;
        [ReadOnly] private BufferLookup<ServantWeaponPosList> _weaponListLookup;
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_PlayAnimation> _eventPlayAnimation;
        [ReadOnly] private ComponentLookup<HybridEvent_SetWeaponActive> _eventSetWeaponActive;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CacheProperties>();
            
            _localPlayerLookup = state.GetComponentLookup<LocalPlayerTag>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _creatureForwardLookup = state.GetComponentLookup<CreatureForward>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _cacheLookup = state.GetComponentLookup<CacheProperties>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _shieldLookup = state.GetComponentLookup<ShieldProperties>(true);
            _monsterMoveLookup = state.GetComponentLookup<MonsterMove>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
            _summonEntitiesLookup = state.GetBufferLookup<SummonEntities>(true);
            _effectLookup = state.GetComponentLookup<EffectProperties>(true);
            _monsterLookup = state.GetComponentLookup<MonsterProperties>(true);
            _muzzlePosLookup = state.GetComponentLookup<CreatureMuzzlePos>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _weaponListLookup = state.GetBufferLookup<ServantWeaponPosList>(true);
            _eventPlayAnimation = state.GetComponentLookup<HybridEvent_PlayAnimation>(true);
            _eventSetWeaponActive = state.GetComponentLookup<HybridEvent_SetWeaponActive>(true);
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

            _localPlayerLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _creatureForwardLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _creatureTag.Update(ref state);
            _deadLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _cacheLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _shieldLookup.Update(ref state);
            _monsterMoveLookup.Update(ref state);
            _childLookup.Update(ref state);
            _summonEntitiesLookup.Update(ref state);
            _effectLookup.Update(ref state);
            _monsterLookup.Update(ref state);
            _muzzlePosLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _weaponListLookup.Update(ref state);
            _eventPlayAnimation.Update(ref state);
            _eventSetWeaponActive.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var cacheEntity = SystemAPI.GetSingletonEntity<CacheProperties>();

            //do Actions
            new SkillActionJob
            {
                Ecb = ecb.AsParallelWriter(),
                CacheEntity = cacheEntity,
                GlobalEntity = global.Entity,
                TransformLookup = _transformLookup,
                CreatureLookup = _creatureLookup,
                CreatureForwardLookup = _creatureForwardLookup,
                CreatureTag = _creatureTag,
                DeadLookup = _deadLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                BuffTagLookup = _buffTagLookup,
                BuffCommonLookup = _buffCommonLookup,
                CollisionWorld = collisionWorld,
                CacheLookup = _cacheLookup,
                SkillEntitiesLookup = _skillEntitiesLookup,
                SkillTagLookup = _skillTagLookup,
                LocalPlayerLookup = _localPlayerLookup,
                ShieldLookup = _shieldLookup,
                MonsterMoveLookup = _monsterMoveLookup,
                ChildLookup = _childLookup,
                SummonEntitiesLookup = _summonEntitiesLookup,
                EffectLookup = _effectLookup,
                MonsterLookup = _monsterLookup,
                MuzzlePosLookup = _muzzlePosLookup,
                AttrLookup = _attrLookup,
                AttrModifyLookup = _attrModifyLookup,
                EventPlayAnimation = _eventPlayAnimation,
                EventSetWeaponActive = _eventSetWeaponActive,
                WeaponListLookup = _weaponListLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct SkillActionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity GlobalEntity;
            public Entity CacheEntity;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public ComponentLookup<LocalPlayerTag> LocalPlayerLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public ComponentLookup<CreatureForward> CreatureForwardLookup;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTag;
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public ComponentLookup<CacheProperties> CacheLookup;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
            [ReadOnly] public ComponentLookup<ShieldProperties> ShieldLookup;
            [ReadOnly] public ComponentLookup<MonsterMove> MonsterMoveLookup;
            [ReadOnly] public BufferLookup<Child> ChildLookup;
            [ReadOnly] public BufferLookup<SummonEntities> SummonEntitiesLookup;
            [ReadOnly] public ComponentLookup<EffectProperties> EffectLookup;
            [ReadOnly] public ComponentLookup<MonsterProperties> MonsterLookup;
            [ReadOnly] public ComponentLookup<CreatureMuzzlePos> MuzzlePosLookup;
            [ReadOnly] public ComponentLookup<PlayerAttrData> AttrLookup;
            [ReadOnly] public BufferLookup<PlayerAttrModify> AttrModifyLookup;
            [ReadOnly] public BufferLookup<ServantWeaponPosList> WeaponListLookup;
            [ReadOnly] public ComponentLookup<HybridEvent_PlayAnimation> EventPlayAnimation;
            [ReadOnly] public ComponentLookup<HybridEvent_SetWeaponActive> EventSetWeaponActive;
            
            [BurstCompile]
            private void Execute(DynamicBuffer<SkillActionBuffer> actionBuffers, MasterCreature master, RefRW<SkillProperties> properties, RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CacheHelper.GetSkillConfig(properties.ValueRO.Id, CacheEntity, CacheLookup, out var skillConfig))
                {
                    return;
                }

                var tempShareData = new NativeList<int>(Allocator.Temp);

                for (var i = actionBuffers.Length - 1; i >= 0; i--)
                {
                    var action = actionBuffers[i];
                    actionBuffers.RemoveAt(i);

                    var skillEntity = entity;
                    var actionPos = action.Pos;
                    if (TransformLookup.TryGetComponent(action.Entity, out var actionEntityTrans))
                    {
                        actionPos = actionEntityTrans.Position;
                    }

                    //获取对应的action配置
                    var index = action.Idx;
                    SkillActionConfig actionConfig;
                    if (index == 1) actionConfig = skillConfig.End1;
                    else if (index == 2) actionConfig = skillConfig.End2;
                    else if (index == 3) actionConfig = skillConfig.End3;
                    else if (index == 4) actionConfig = skillConfig.End4;
                    else if (index == 5) actionConfig = skillConfig.End5;
                    else if (index == 6) actionConfig = skillConfig.End6;
                    else
                    {
                        Debug.LogError($"cant find skill end config, idx:{index}");
                        continue;
                    }

                    switch (actionConfig.Action)
                    {
                        //加属性
                        case ESkillAction.AddAttr:
                        {
                            var eAttr = (EAttr)actionConfig.Param1;
                            var addValue = actionConfig.Param2;
                            if (LocalPlayerLookup.HasComponent(master.Value))
                            {
                                Ecb.AppendToBuffer(sortKey, master.Value, new CreatureDataProcess
                                {
                                    Type = ECreatureDataProcess.AddAttr,
                                    IntValue1 = (int)eAttr,
                                    FloatValue1 = addValue,
                                });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, master.Value, true);
                            }
                            else
                            {
                                Debug.LogError("skill add attr only can to localPlayer");
                            }
                            break;
                        }
                        //绑定子弹到生物体
                        case ESkillAction.BindBullet:
                        {
                            var bulletId = (int)actionConfig.Param1;
                            var isMaster = actionConfig.Param2.ToInt() == 1;
                            var targetEntity = isMaster ? master.Value : action.Entity;

                            var shootPos = actionPos;
                            if (TransformLookup.TryGetComponent(targetEntity, out var targetTrans))
                            {
                                shootPos = targetTrans.Position;
                            }

                            //不对坐标生效
                            if (CreatureLookup.HasComponent(targetEntity))
                            {
                                var bulletBuffer = new BulletCreateBuffer
                                {
                                    BulletId = bulletId,
                                    TransformParent = targetEntity,
                                    ShootPos = shootPos,
                                    ParentCreature = master.Value,
                                    AtkValue = properties.ValueRO.AtkValue,
                                    SkillEntity = skillEntity,
                                    DisableSplit = true,
                                    DisableBounce = true,
                                    Direction = new float3(0, 1, 0),
                                };
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);
                            }

                            break;
                        }
                        case ESkillAction.UnbindBullet:
                        {
                            var bulletId = (int)actionConfig.Param1;
                            var isMaster = actionConfig.Param2.ToInt() == 1;
                            var targetEntity = isMaster ? master.Value : action.Entity;

                            //不对坐标生效
                            if (CreatureLookup.HasComponent(targetEntity))
                            {
                                Ecb.AppendToBuffer(sortKey, targetEntity, new CreatureDataProcess
                                {
                                    Type = ECreatureDataProcess.UnbindBullet,
                                    IntValue1 = bulletId,
                                });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, targetEntity, true);
                            }

                            break;
                        }
                        case ESkillAction.ShootBulletCircle:
                        {
                            var bulletId = (int)actionConfig.Param1;
                            var count = (int)actionConfig.Param2;
                            var radius = actionConfig.Param3;

                            var buffResult = BuffHelper.GetBuffFactorAndValue(master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillShootBulletCount, skillConfig.ClassId, skillConfig.Id);
                            count += (int)buffResult.AddValue;
                            count = (int)(BuffHelper.CalcFactor(count, buffResult.AddFactor));

                            if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                            {
                                if (bulletId <= 0)
                                {
                                    bulletId = properties.ValueRO.SavedBulletId;
                                    if (bulletId <= 0)
                                    {
                                        Debug.LogError($"Skill Action ShootBullet Error, bulletId <= 0, 是否未使用BulletHitEnemy配对? skillId:{skillConfig.Id}");
                                        break;
                                    }
                                }

                                var direction = math.normalizesafe(actionPos - masterTrans.Position);

                                //生成圆环子弹
                                for (var j = 0; j < count; j++)
                                {
                                    var startAngle = 360f / count * j;

                                    //防错
                                    if (!MathHelper.IsValid(direction))
                                    {
                                        direction = new float3(0, 1, 0);
                                    }

                                    //新建一颗环绕子弹
                                    var bulletBuffer = new BulletCreateBuffer
                                    {
                                        BulletId = bulletId,
                                        ParentCreature = master.Value,
                                        AtkValue = action.AtkValue,
                                        SkillEntity = skillEntity,
                                        DisableSplit = true,
                                        AroundPos = masterTrans.Position,
                                        AroundRadiusX = radius,
                                        AroundRadiusY = radius,
                                        AroundDefaultAngle = startAngle,
                                        ReverseAround = false,
                                        Direction = direction,
                                        ShootPos = action.Pos,
                                    };
                                    Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);
                                }
                            }

                            break;
                        }
                        case ESkillAction.ShootBulletLR:
                        {
                            var bulletIdL = (int)actionConfig.Param1;
                            var bulletIdR = (int)actionConfig.Param2;
                            var dist = actionConfig.Param3;
                            var count = (int)actionConfig.Param4;
                            var interval = actionConfig.Param5;
                            if (count <= 0) count = 1;

                            var buffResult = BuffHelper.GetBuffFactorAndValue(master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillShootBulletCount, skillConfig.ClassId, skillConfig.Id);
                            count += (int)buffResult.AddValue;
                            count = (int)(BuffHelper.CalcFactor(count, buffResult.AddFactor));


                            if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                            {
                                if (bulletIdL <= 0)
                                {
                                    bulletIdL = properties.ValueRO.SavedBulletId;
                                    if (bulletIdL <= 0)
                                    {
                                        Debug.LogError($"Skill Action ShootBullet Error, bulletId <= 0, 是否未使用BulletHitEnemy配对? skillId:{skillConfig.Id}");
                                        break;
                                    }
                                }

                                if (bulletIdR <= 0)
                                {
                                    bulletIdR = properties.ValueRO.SavedBulletId;
                                    if (bulletIdR <= 0)
                                    {
                                        Debug.LogError($"Skill Action ShootBullet Error, bulletId <= 0, 是否未使用BulletHitEnemy配对? skillId:{skillConfig.Id}");
                                        break;
                                    }
                                }

                                var muzzlePos = float3.zero;
                                if (MuzzlePosLookup.TryGetComponent(master.Value, out var masterCreature))
                                {
                                    muzzlePos = masterCreature.Value;
                                }

                                var direction = math.normalizesafe(actionPos - masterTrans.Position);

                                //防错
                                if (!MathHelper.IsValid(direction))
                                {
                                    direction = new float3(0, 1, 0);
                                }

                                var contTime = interval * count;
                                var everyCount = contTime <= 0 ? count : 1; //如果无持续时间，则一次性射出所有子弹

                                //左
                                var startL = MathHelper.RotateForward(direction, -90f) * dist;
                                Ecb.AppendToBuffer(sortKey, master.Value, new ShootBulletBuffer
                                {
                                    BulletId = bulletIdL,
                                    Direction = direction,
                                    SkillEntity = skillEntity,
                                    MaxContTime = contTime,
                                    ShootInterval = interval,
                                    IntervalShootCount = everyCount,
                                    RotateAngle = 0,
                                    DisableSplit = true,
                                    MuzzlePos = muzzlePos - startL,
                                });
                                Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, master.Value, true);

                                //右
                                var startR = MathHelper.RotateForward(direction, 90f) * dist;
                                Ecb.AppendToBuffer(sortKey, master.Value, new ShootBulletBuffer
                                {
                                    BulletId = bulletIdR,
                                    Direction = direction,
                                    SkillEntity = skillEntity,
                                    MaxContTime = contTime,
                                    ShootInterval = interval,
                                    IntervalShootCount = everyCount,
                                    RotateAngle = 0,
                                    DisableSplit = true,
                                    MuzzlePos = muzzlePos - startR,
                                });
                                Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, master.Value, true);
                            }

                            break;
                        }

                        case ESkillAction.SummonDoodadRandom:
                        {
                            var bulletId1 = (int)actionConfig.Param1;
                            var bulletId2 = (int)actionConfig.Param2;
                            var bulletId3 = (int)actionConfig.Param3;
                            var bulletId4 = (int)actionConfig.Param4;

                            var randomList = new NativeList<int>(Allocator.Temp);
                            if (bulletId1 > 0 && !tempShareData.Contains(bulletId1)) randomList.Add(bulletId1);
                            if (bulletId2 > 0 && !tempShareData.Contains(bulletId2)) randomList.Add(bulletId2);
                            if (bulletId3 > 0 && !tempShareData.Contains(bulletId3)) randomList.Add(bulletId3);
                            if (bulletId4 > 0 && !tempShareData.Contains(bulletId4)) randomList.Add(bulletId4);

                            if (randomList.Length <= 0)
                            {
                                if (bulletId1 > 0) randomList.Add(bulletId1);
                                if (bulletId2 > 0) randomList.Add(bulletId2);
                                if (bulletId3 > 0) randomList.Add(bulletId3);
                                if (bulletId4 > 0) randomList.Add(bulletId4);
                            }

                            if (randomList.Length > 0)
                            {
                                var bulletId = randomList[random.ValueRW.Value.NextInt(0, randomList.Length)];
                                tempShareData.Add(bulletId);

                                Ecb.AppendToBuffer(sortKey, master.Value, new ShootBulletBuffer
                                {
                                    BulletId = bulletId,
                                    Direction = new float3(0, 1, 0),
                                    SkillEntity = skillEntity,
                                    MaxContTime = 0,
                                    ShootInterval = 0,
                                    IntervalShootCount = 1,
                                    RotateAngle = 0,
                                    ForceShootPos = actionPos,
                                    DisableSplit = true,
                                    MuzzlePos = 0,
                                    MuzzleEffectId = 0,
                                });
                                Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, master.Value, true);
                            }

                            break;
                        }
                        case ESkillAction.SummonDoodad:
                        {
                            var bulletId = (int)actionConfig.Param1;
                            var y = actionConfig.Param2;
                            var pos = actionPos;
                            pos.y = y;
                            
                            Ecb.AppendToBuffer(sortKey, master.Value, new ShootBulletBuffer
                            {
                                BulletId = bulletId,
                                Direction = new float3(0, 0, 1),
                                SkillEntity = skillEntity,
                                MaxContTime = 0,
                                ShootInterval = 0,
                                IntervalShootCount = 1,
                                RotateAngle = 0,
                                ForceShootPos = pos,
                                DisableSplit = true,
                                MuzzlePos = 0,
                                MuzzleEffectId = 0,
                            });
                            Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, master.Value, true);
                            break;
                        }
                        //发射子弹
                        case ESkillAction.ShootBullet:
                        case ESkillAction.ShootBulletWeaponSlots:
                        {
                            var bulletId = (int)actionConfig.Param1;
                            var count = (int)actionConfig.Param2;
                            var interval = actionConfig.Param3;
                            var rotateAngle = actionConfig.Param4;
                            var startAngle = actionConfig.Param5;
                            var muzzleEffectId = actionConfig.Param6.ToInt();
                            var playSpellAni = actionConfig.Param7.ToInt() == 1;
                            var allowAttrShootCount = actionConfig.Param8.ToInt() == 1;

                            if (count <= 0)
                            {
                                count = 1;
                            }

                            var buffResult = BuffHelper.GetBuffFactorAndValue(master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillShootBulletCount, skillConfig.ClassId, skillConfig.Id);
                            count += (int)buffResult.AddValue;
                            count = (int)(BuffHelper.CalcFactor(count, buffResult.AddFactor));

                            if (TransformLookup.HasComponent(master.Value))
                            {
                                if (bulletId <= 0)
                                {
                                    bulletId = properties.ValueRO.SavedBulletId;
                                    if (bulletId <= 0)
                                    {
                                        Debug.LogError($"Skill Action ShootBullet Error, bulletId <= 0, 是否未使用BulletHitEnemy配对? skillId:{skillConfig.Id}");
                                        break;
                                    }
                                }

                                var muzzlePos = float3.zero;
                                var forceShootPos = action.StartPos;
                                var direction = math.normalizesafe(actionPos - action.StartPos);

                                if (MuzzlePosLookup.TryGetComponent(master.Value, out var muzzlePosProperties))
                                {
                                    muzzlePos = muzzlePosProperties.Value;
                                }

                                //防错
                                if (!MathHelper.IsValid(direction))
                                {
                                    Debug.LogError($"shoot bullet error, dir is nan：{actionPos} {action.StartPos}");
                                    direction = new float3(0, 1, 0);
                                }

                                var contTime = interval * count;
                                var everyCount = contTime <= 0 ? count : 1; //如果无持续时间，则一次性射出所有子弹

                                if (startAngle != 0)
                                {
                                    direction = MathHelper.RotateForward(direction, startAngle);
                                }

                                var bUseWeaponSlots = actionConfig.Action == ESkillAction.ShootBulletWeaponSlots;
                                if (bUseWeaponSlots)
                                {
                                    if (WeaponListLookup.TryGetBuffer(master.Value, out var weaponList))
                                    {
                                        foreach (var mPos in weaponList)
                                        {
                                            Ecb.AppendToBuffer(sortKey, master.Value, new ShootBulletBuffer
                                            {
                                                BulletId = bulletId,
                                                Direction = direction,
                                                SkillEntity = skillEntity,
                                                MaxContTime = contTime,
                                                ShootInterval = interval,
                                                IntervalShootCount = everyCount,
                                                RotateAngle = rotateAngle,
                                                ForceShootPos = forceShootPos,
                                                DisableSplit = false,
                                                MuzzlePos = mPos.Value,
                                                MuzzleEffectId = muzzleEffectId,
                                                MuzzleScale = 1f,
                                                MuzzleAliveTime = 0.5f,
                                                Offset = action.Param1,
                                                MuzzleLaser = false,
                                                PlaySpellAni = playSpellAni,
                                                bUseWeaponSlots = bUseWeaponSlots,
                                            });
                                            Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, master.Value, true);
                                        }
                                    }
                                }
                                else
                                {
                                    Ecb.AppendToBuffer(sortKey, master.Value, new ShootBulletBuffer
                                    {
                                        BulletId = bulletId,
                                        Direction = direction,
                                        SkillEntity = skillEntity,
                                        MaxContTime = contTime,
                                        ShootInterval = interval,
                                        IntervalShootCount = everyCount,
                                        RotateAngle = rotateAngle,
                                        ForceShootPos = forceShootPos,
                                        DisableSplit = false,
                                        MuzzlePos = muzzlePos,
                                        MuzzleEffectId = muzzleEffectId,
                                        MuzzleScale = 1f,
                                        MuzzleAliveTime = 0.5f,
                                        Offset = action.Param1,
                                        MuzzleLaser = false,
                                        PlaySpellAni = playSpellAni,
                                        bUseWeaponSlots = bUseWeaponSlots,
                                    });
                                    Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, master.Value, true);
                                }
                            }

                            break;
                        }
                        case ESkillAction.ShootLaser:
                        {
                            var bulletId = actionConfig.Param1.ToInt();
                            var damageInterval = actionConfig.Param2;
                            var contTime = actionConfig.Param3;
                            var laserEffect = actionConfig.Param4.ToInt();
                            var rotateSpeed = actionConfig.Param5.ToInt();
                            var initAngle = actionConfig.Param6;
                            var forcePos = actionConfig.Param7.ToInt() == 1;

                            //基础属性影响持续时间
                            contTime = BuffHelper.CalcContTime(contTime, master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);

                            if (TransformLookup.TryGetComponent(master.Value, out var masterTrans) && MuzzlePosLookup.TryGetComponent(master.Value, out var muzzlePos))
                            {
                                var startPos = masterTrans.Position;
                                var shootDirection = math.normalizesafe(actionPos - startPos);
                                shootDirection.y = 0;
                                
                                if (MathHelper.IsValid(shootDirection))
                                {
                                    shootDirection = MathHelper.Angle2Forward(MathHelper.Forward2Angle(shootDirection) + initAngle);
                                    Ecb.AppendToBuffer(sortKey, master.Value, new ShootBulletBuffer
                                    {
                                        BulletId = bulletId,
                                        Direction = shootDirection,
                                        SkillEntity = skillEntity,
                                        MaxContTime = contTime,
                                        ShootInterval = damageInterval,
                                        RotateAngle = 0,
                                        RotateSpeed = rotateSpeed,
                                        DisableSplit = true,
                                        MuzzleEffectId = laserEffect,
                                        MuzzlePos = muzzlePos.Value,
                                        MuzzleScale = 1,
                                        IntervalShootCount = 1,
                                        MuzzleLaser = true,
                                        PlaySpellAni = true, 
                                        ForceShootPos = forcePos ? muzzlePos.Value + masterTrans.Position : Vector3.zero,
                                    });
                                    Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, master.Value, true);
                                }
                                else
                                {
                                    Debug.LogError($"ShootLaser error, shootDirection is zero, 目标起点和终点一样了?  skillId:{skillConfig.Id}");
                                }
                            }

                            break;
                        }
                        //通过子弹对目标立即伤害
                        case ESkillAction.DirectDamage:
                        {
                            //不对坐标生效 
                            if (CreatureLookup.HasComponent(action.Entity))
                            {
                                var bulletId = (int)actionConfig.Param1;
                                if (bulletId > 0)
                                {
                                    if (TransformLookup.TryGetComponent(action.Entity, out var enemyTrans))
                                    {
                                        var bulletBuffer = new BulletCreateBuffer
                                        {
                                            BulletId = bulletId,
                                            ShootPos = enemyTrans.Position,
                                            ParentCreature = master.Value,
                                            AtkValue = properties.ValueRO.AtkValue,
                                            SkillEntity = skillEntity,
                                            ForceHitCreature = action.Entity,
                                            DisableSplit = true,
                                        };
                                        Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);
                                    }
                                    else
                                    {
                                        Debug.LogError($"Skill action error, Damage 中使用的子弹ID为0, skillId:{skillConfig.Id}");
                                    }
                                }
                            }

                            break;
                        }
                        case ESkillAction.DamageAoe:
                        {
                            var bulletId = (int)actionConfig.Param1;
                            var radius = actionConfig.Param2;
                            if (bulletId > 0 && radius > 0)
                            {
                                var enemies = PhysicsHelper.OverlapEnemies(master.Value, CollisionWorld, actionPos, radius, CreatureTag, DeadLookup, properties.ValueRO.AtkValue.Team);
                                for (var e = 0; e < enemies.Length; e++)
                                {
                                    var enemy = enemies[e];
                                    if (TransformLookup.TryGetComponent(enemy, out var enemyTrans))
                                    {
                                        var bulletBuffer = new BulletCreateBuffer
                                        {
                                            BulletId = bulletId,
                                            ShootPos = enemyTrans.Position,
                                            ParentCreature = master.Value,
                                            AtkValue = properties.ValueRO.AtkValue,
                                            SkillEntity = skillEntity,
                                            ForceHitCreature = enemy,
                                            DisableSplit = true,
                                        };

                                        Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);
                                    }
                                }

                                enemies.Dispose();
                            }

                            break;
                        }
                        //产生立即爆炸的子弹
                        case ESkillAction.Explosion:
                        {
                            //新建一颗子弹, 距离为0.01, 立即爆炸
                            var bulletId = (int)actionConfig.Param1;
                            var bombRadius = actionConfig.Param2;

                            var initPos = actionPos;
                            var bulletBuffer = new BulletCreateBuffer
                            {
                                BulletId = bulletId,
                                ShootPos = initPos,
                                ParentCreature = master.Value,
                                AtkValue = properties.ValueRO.AtkValue,
                                SkillEntity = skillEntity,
                                ImmediatelyBomb = true,
                                BombRadius = bombRadius,
                                DisableSplit = true,
                            };
                            Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);

                            break;
                        }

                        //加buff
                        case ESkillAction.AddBuff:
                        {
                            var buffId = (int)actionConfig.Param1;
                            var contTime = actionConfig.Param2;
                            var forceMaster = actionConfig.Param3.ToInt() == 1;

                            //给目标上Buff(不对坐标生效)
                            var addEntity = forceMaster ? master.Value : action.Entity;
                            if (CreatureLookup.HasComponent(addEntity))
                            {
                                BuffHelper.AppendCreateBuffData(GlobalEntity, buffId, addEntity, master.Value, contTime, EBuffFrom.Skill, skillConfig.Id, Ecb, sortKey);
                            }

                            break;
                        }

                        //加随机buff
                        case ESkillAction.AddRandomBuff:
                        {
                            if (CreatureLookup.HasComponent(action.Entity))
                            {
                                var buffId1 = (int)actionConfig.Param1;
                                var buffId2 = (int)actionConfig.Param2;
                                var buffId3 = (int)actionConfig.Param3;
                                var buffId4 = (int)actionConfig.Param4;

                                var buffList = new NativeList<int>(Allocator.Temp);
                                if (buffId1 > 0) buffList.Add(buffId1);
                                if (buffId2 > 0) buffList.Add(buffId2);
                                if (buffId3 > 0) buffList.Add(buffId3);
                                if (buffId4 > 0) buffList.Add(buffId4);

                                var idx = random.ValueRW.Value.NextInt(0, buffList.Length);
                                var randomBuffId = buffList[idx];

                                BuffHelper.AppendCreateBuffData(GlobalEntity, randomBuffId, action.Entity, master.Value, 0, EBuffFrom.Skill, skillConfig.Id, Ecb, sortKey);
                                buffList.Dispose();
                            }

                            break;
                        }

                        //加范围buff
                        case ESkillAction.AddBuffRange:
                        {
                            var buffId = (int)actionConfig.Param1;
                            var contTime = actionConfig.Param2;
                            var radius = actionConfig.Param3;

                            var enemies = PhysicsHelper.OverlapEnemies(master.Value, CollisionWorld, actionPos, radius, CreatureTag, DeadLookup, properties.ValueRO.AtkValue.Team);
                            for (var e = 0; e < enemies.Length; e++)
                            {
                                var enemy = enemies[e];
                                BuffHelper.AppendCreateBuffData(GlobalEntity, buffId, enemy, master.Value, contTime, EBuffFrom.Skill, skillConfig.Id, Ecb, sortKey);
                            }

                            enemies.Dispose();

                            break;
                        }
                        //移除buff
                        case ESkillAction.RemoveBuff:
                        {
                            //给目标上Buff(不对坐标生效)
                            var buffId = (int)actionConfig.Param1;
                            var forceMaster = actionConfig.Param2.ToInt() == 1;
                            var removeEntity = forceMaster ? master.Value : action.Entity;

                            if (CreatureLookup.HasComponent(removeEntity))
                            {
                                BuffHelper.RemoveBuffById(buffId, removeEntity, BuffEntitiesLookup, BuffTagLookup, Ecb, sortKey);
                            }

                            break;
                        }
                        case ESkillAction.RemoveAllBuff:
                        {
                            var forceMaster = actionConfig.Param1.ToInt() == 1;
                            var removeEntity = forceMaster ? master.Value : action.Entity;
                            if (CreatureLookup.HasComponent(removeEntity))
                            {
                                BuffHelper.RemoveAllBuff(removeEntity, BuffEntitiesLookup, BuffTagLookup, Ecb, sortKey);
                            }

                            break;
                        }
                        case ESkillAction.CastBulletBehaviour:
                        {
                            var bulletId = actionConfig.Param1.ToInt();
                            var bulletClassId = actionConfig.Param2.ToInt();
                            var actionId = actionConfig.Param3.ToInt();

                            if (CreatureLookup.HasComponent(action.Entity))
                            {
                                Ecb.AppendToBuffer(sortKey, action.Entity, new CreatureDataProcess
                                {
                                    Type = ECreatureDataProcess.CastBulletAction,
                                    AddValue = actionId,
                                    IntValue1 = bulletId,
                                    IntValue2 = bulletClassId,
                                });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, action.Entity, true);
                            }

                            break;
                        }
                        //再次触发技能Action
                        case ESkillAction.CastSkill:
                        case ESkillAction.CastSkillRandom:
                        {
                            //create skill
                            if (properties.ValueRO.MaxRecursion == 0 || properties.ValueRO.RecursionCount < properties.ValueRO.MaxRecursion)
                            {
                                int skillId;
                                float delay;
                                bool bChangeStartInfo;
                                int maxRecursion;
                                float delayRange;
                                if (actionConfig.Action == ESkillAction.CastSkill)
                                {
                                    skillId = (int)actionConfig.Param1;
                                    delay = actionConfig.Param2;
                                    bChangeStartInfo = (int)actionConfig.Param3 == 1;
                                    maxRecursion = actionConfig.Param4.ToInt();
                                    delayRange = actionConfig.Param5;
                                }
                                else
                                {
                                    var skillId1 = (int)actionConfig.Param1;
                                    var skillId2 = (int)actionConfig.Param2;
                                    var skillId3 = (int)actionConfig.Param3;
                                    var skillId4 = (int)actionConfig.Param4;
                                    delay = actionConfig.Param5;
                                    bChangeStartInfo = (int)actionConfig.Param6 == 1;
                                    maxRecursion = actionConfig.Param7.ToInt();
                                    delayRange = 0f;

                                    var randomList = new NativeList<int>(Allocator.Temp);
                                    if (skillId1 > 0) randomList.Add(skillId1);
                                    if (skillId2 > 0) randomList.Add(skillId2);
                                    if (skillId3 > 0) randomList.Add(skillId3);
                                    if (skillId4 > 0) randomList.Add(skillId4);

                                    var idx = random.ValueRW.Value.NextInt(0, randomList.Length);
                                    skillId = randomList[idx];
                                    randomList.Dispose();
                                }


                                if (skillId > 0)
                                {
                                    if (maxRecursion > 0)
                                    {
                                        var addRecursionCount = BuffHelper.GetBuffAddValue(master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillRecursionCount,
                                            skillConfig.ClassId, skillConfig.Id);
                                        maxRecursion += (int)addRecursionCount;
                                    }

                                    var startEntity = master.Value;
                                    var startPos = action.StartPos;

                                    if (bChangeStartInfo)
                                    {
                                        startEntity = action.Entity;
                                        startPos = actionPos;
                                    }
                                    else
                                    {
                                        if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                                        {
                                            startPos = masterTrans.Position;
                                            startEntity = master.Value;
                                        }
                                    }

                                    var forceInfo = new SkillPrevTarget
                                    {
                                        Enable = true,
                                        Target = action.Entity,
                                        Pos = actionPos
                                    };

                                    if (delay > 0)
                                    {
                                        var buffDelaySec = BuffHelper.GetBuffAddFactor(master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillDelayTime, skillConfig.ClassId, skillConfig.Id);
                                        delay += buffDelaySec;
                                    }

                                    if (delayRange > 0)
                                    {
                                        delay += random.ValueRW.Value.NextFloat(-delayRange, delayRange);
                                    }

                                    var castDelay = delay + action.CastIndex * 0.01f;

                                    //如果自己不是通过CastSkill释放的，则计算为RootSkill, 计算递归用
                                    if (properties.ValueRO.RootSkillId == 0)
                                    {
                                        //Debug.LogError($"cast skill, from:{properties.ValueRO.Id} start:{skillId} {maxRecursion}");
                                        //从投启动
                                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new CreateSkillBuffer(master.Value, skillId, properties.ValueRO.AtkValue, startEntity, startPos, forceInfo, skillId, castDelay, 0, maxRecursion));
                                    }
                                    else
                                    {
                                        //从别的castSkill递归过来的情况
                                        //递归到自己了，增加递归次数
                                        if (properties.ValueRO.RootSkillId == skillConfig.Id)
                                        {
                                            properties.ValueRW.RecursionCount += 1;

                                            //Debug.LogError($"cast skill, from:{properties.ValueRO.Id} cast:{skillId} 递归到自己:{properties.ValueRW.RecursionCount} {properties.ValueRO.MaxRecursion}");
                                        }

                                        //未递归到自己，直接释放技能
                                        //Debug.LogError($"cast skill, 未递归到自己, from {properties.ValueRO.Id} castTo {skillId}");
                                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new CreateSkillBuffer(master.Value, skillId, properties.ValueRO.AtkValue, startEntity, startPos, forceInfo, properties.ValueRO.RootSkillId,
                                            castDelay, properties.ValueRO.RecursionCount, properties.ValueRO.MaxRecursion));
                                    }
                                }
                            }
                            else
                            {
                                //Debug.LogError($"cast skill,技能递归结束, {properties.ValueRO.Id} {properties.ValueRW.RecursionCount} {  properties.ValueRW.MaxRecursion}");
                                if (properties.ValueRO.RootSkillId > 0)
                                {
                                    if (CacheHelper.GetSkillConfig(properties.ValueRO.RootSkillId, CacheEntity, CacheLookup, out var rootConfig))
                                    {
                                        var skillClassId = rootConfig.ClassId;

                                        //技能递归结束
                                        //Debug.LogError($"cast skill,技能递归结束, {skillClassId} {properties.ValueRO.RootSkillId}");
                                        SkillHelper.DoSkillTrigger(master.Value, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnSkillRecursionOver)
                                        {
                                            IntValue1 = skillClassId,
                                            IntValue2 = properties.ValueRO.RootSkillId,
                                        }, Ecb, sortKey);
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"技能递归结束但 rootSkillId == 0?, skillId:{properties.ValueRO.Id}");
                                }
                            }

                            break;
                        }
                        case ESkillAction.CastSkillCount:
                        {
                            var skillId = (int)actionConfig.Param1;
                            var interval = actionConfig.Param2;
                            var count = actionConfig.Param3.ToInt();
                            var delayTime = actionConfig.Param4;

                            if (count <= 0)
                            {
                                count = 1;
                            }

                            if (skillId > 0)
                            {
                                var startEntity = master.Value;
                                var startPos = action.StartPos;
                                if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                                {
                                    startPos = masterTrans.Position;
                                    startEntity = master.Value;
                                }

                                var forceInfo = new SkillPrevTarget
                                {
                                    Enable = true,
                                    Target = action.Entity,
                                    Pos = actionPos
                                };

                                for (var c = 0; c < count; c++)
                                {
                                    var delay = delayTime + interval * c;
                                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new CreateSkillBuffer(master.Value, skillId, properties.ValueRO.AtkValue, startEntity, startPos, forceInfo, skillId, delay));
                                }
                            }

                            break;
                        }
                        //旋转子弹
                        case ESkillAction.CircleBullet:
                        {
                            //生成子弹, 赋予旋转特性
                            var bulletId = (int)actionConfig.Param1;
                            var radiusX = actionConfig.Param2;
                            var radiusY = actionConfig.Param3;
                            var bulletCount = actionConfig.Param4;
                            var usePosition = (int)actionConfig.Param5 == 1;
                            var clockWise = actionConfig.Param6.ToInt();
                            var startMaster = actionConfig.Param7.ToInt() == 1;
                            var startAngle = actionConfig.Param8;

                            if (bulletCount <= 0) bulletCount = 1;

                            var buffResult = BuffHelper.GetBuffFactorAndValue(master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, EBuffType.SkillShootBulletCount, skillConfig.ClassId, skillConfig.Id);
                            bulletCount += (int)buffResult.AddValue;
                            bulletCount = (int)(BuffHelper.CalcFactor(bulletCount, buffResult.AddFactor));

                            var centerEntity = startMaster ? master.Value : action.Entity;
                            var centerPos = actionPos;
                            var direction = float3.zero;
                            if (startMaster)
                            {
                                if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                                {
                                    centerPos = masterTrans.Position;
                                    direction = math.normalizesafe(actionPos - centerPos);
                                }
                            }

                            if (MathHelper.IsZero(centerPos))
                            {
                                centerPos = new float3(0, 0.1f, 0);
                            }

                            var bUsePos = usePosition || !TransformLookup.HasComponent(action.Entity);
                            var aroundEntity = bUsePos ? Entity.Null : centerEntity;
                            var aroundPos = bUsePos ? centerPos : float3.zero;
                            if (bulletId > 0)
                            {
                                for (var j = 0; j < bulletCount; j++)
                                {
                                    var defaultAngle = (startAngle + 360f / bulletCount * j) % 360f;

                                    //新建一颗环绕子弹
                                    var bulletBuffer = new BulletCreateBuffer
                                    {
                                        BulletId = bulletId,
                                        ParentCreature = master.Value,
                                        AtkValue = action.AtkValue,
                                        SkillEntity = skillEntity,
                                        DisableSplit = true,
                                        AroundPos = aroundPos,
                                        AroundEntity = aroundEntity,
                                        AroundRadiusX = radiusX,
                                        AroundRadiusY = radiusY,
                                        AroundDefaultAngle = defaultAngle,
                                        ReverseAround = clockWise == -1,
                                        Direction = startMaster ? direction : MathHelper.Angle2Forward(defaultAngle),
                                        ShootPos = new float3(0, 1000, 0),
                                    };
                                    Ecb.AppendToBuffer(sortKey, GlobalEntity, bulletBuffer);
                                }
                            }

                            break;
                        }

                        //百分比回血
                        case ESkillAction.Cure:
                        {
                            var percent = actionConfig.Param1;
                            var value = (int)actionConfig.Param2;
                            var needShield = actionConfig.Param3.ToInt() == 1;
                            var disableNumber = actionConfig.Param4.ToInt() == 1;

                            var bHasShield = CreatureHelper.CheckHasShield(action.Entity, ShieldLookup);
                            if (needShield && !bHasShield)
                            {
                                continue;
                            }

                            Ecb.AppendToBuffer(sortKey, action.Entity, new CreatureDataProcess
                            {
                                Type = ECreatureDataProcess.Cure,
                                AddValue = value,
                                AddPercent = percent,
                                BoolValue = disableNumber,
                            });
                            Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, action.Entity, true);

                            break;
                        }

                        case ESkillAction.DashFollowLine:
                        {
                            var speed = actionConfig.Param1;
                            //var resourceId = (int)actionConfig.Param2;
                            //var shadowCount = (int)actionConfig.Param3;
                            var effectId = actionConfig.Param4.ToInt();

                            Ecb.SetComponent(sortKey, master.Value, new DashFollowLineTag
                            {
                                Speed = speed,
                                EffectId = effectId,
                            });
                            Ecb.SetComponentEnabled<DashFollowLineTag>(sortKey, master.Value, true);
                            break;
                        }

                        //dash
                        case ESkillAction.Dash:
                        {
                            //dash 的情况不允许Cast多次
                            var speed = actionConfig.Param1;
                            
                            var afterSkillId = (int)actionConfig.Param4;
                            var extraDist = actionConfig.Param5; //额外距离
                            var disableCollision = actionConfig.Param6.ToInt() == 1;
                            var effectId = actionConfig.Param7.ToInt();
                            var forceDist = actionConfig.Param8;

                            var dashEndPos = actionPos;
                            if (extraDist > 0)
                            {
                                if (TransformLookup.TryGetComponent(master.Value, out var masterTrans))
                                {
                                    var dir = math.normalizesafe(actionPos - masterTrans.Position);
                                    dashEndPos += dir * extraDist;
                                }
                            }

                            Ecb.SetComponent(sortKey, master.Value, new DashStartTag
                            {
                                Pos = dashEndPos,
                                Speed = speed,
                                EffectId = effectId,
                                AfterSkillId = afterSkillId,
                                RootSkillId = properties.ValueRO.RootSkillId,
                                SkillAtkValue = properties.ValueRO.AtkValue,
                                SkillRecursionCount = properties.ValueRO.RecursionCount,
                                SkillMaxRecursion = properties.ValueRO.MaxRecursion,
                                PrevTarget = action.Entity,
                                PrevTargetPos = action.Pos,
                                DisableCollision = disableCollision,
                                ForceDist = forceDist,
                            });
                            Ecb.SetComponentEnabled<DashStartTag>(sortKey, master.Value, true);
                            break;
                        }
                        case ESkillAction.DashBounce:
                        {
                            //dash 的情况不允许Cast多次
                            var speed = actionConfig.Param1;
                            var bounceCount = actionConfig.Param2.ToInt();
                            var forceDist = actionConfig.Param3;
                            var afterSkillId = (int)actionConfig.Param4;
                            var effectId = actionConfig.Param5.ToInt();

                            Ecb.SetComponent(sortKey, master.Value, new DashStartTag
                            {
                                Pos = actionPos,
                                Speed = speed,
                                EffectId = effectId,
                                AfterSkillId = afterSkillId,
                                RootSkillId = properties.ValueRO.RootSkillId,
                                SkillAtkValue = properties.ValueRO.AtkValue,
                                SkillRecursionCount = properties.ValueRO.RecursionCount,
                                SkillMaxRecursion = properties.ValueRO.MaxRecursion,
                                PrevTarget = action.Entity,
                                PrevTargetPos = action.Pos,
                                DisableCollision = false,
                                ForceDist = forceDist,
                                BounceCount = bounceCount,
                            });
                            Ecb.SetComponentEnabled<DashStartTag>(sortKey, master.Value, true);
                            break;
                        }
                        case ESkillAction.PlayMuzzleEffect:
                        {
                            var resourceId = (int)actionConfig.Param1;
                            var scale = actionConfig.Param2;

                            if (MuzzlePosLookup.TryGetComponent(master.Value, out var muzzlePos))
                            {
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                {
                                    ResourceId = resourceId,
                                    Pos = muzzlePos.Value,
                                    DelayDestroy = 2f,
                                    Scale = scale,
                                });
                            }
                          
                            break;
                        }
                        //播放特效
                        case ESkillAction.PlayEffect:
                        {
                            var resourceId = (int)actionConfig.Param1;
                            var delayDestroy = actionConfig.Param2;
                            var usePosition = (int)actionConfig.Param3 == 1;
                            var y = actionConfig.Param4;
                            var influenceByAtkRange = actionConfig.Param5.ToInt() == 1;
                            var scale = actionConfig.Param6;
                            var xOffset = actionConfig.Param7;
                            var yOffset = actionConfig.Param8;

                            if (scale <= 0f) scale = 1f;
                            if (resourceId > 0)
                            {
                                var scaleFactor = 1f;
                                if (influenceByAtkRange)
                                {
                                    scaleFactor += AttrHelper.GetDamageRangeFactor(master.Value, CreatureLookup, AttrLookup, AttrModifyLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup, false);
                                }

                                var parent = usePosition ? Entity.Null : action.Entity;
                                var pos = actionPos;
                                if (TransformLookup.TryGetComponent(parent, out var parentTransform) )
                                {
                                    pos = new float3(xOffset * parentTransform.Scale, 0, yOffset * parentTransform.Scale);
                                }

                                pos.y = y;
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                {
                                    ResourceId = resourceId,
                                    Pos = pos,
                                    Parent = parent,
                                    DelayDestroy = delayDestroy,
                                    Scale = scale * scaleFactor,
                                });
                            }

                            break;
                        }
                        case ESkillAction.PlayEffectMaster:
                        {
                            var resourceId = (int)actionConfig.Param1;
                            var delayDestroy = actionConfig.Param2;
                            var scale = actionConfig.Param3;

                            if (scale <= 0f)
                            {
                                scale = 1f;
                            }
                            
                            if (resourceId > 0)
                            {
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                {
                                    ResourceId = resourceId,
                                    Pos = float3.zero,
                                    Parent = master.Value,
                                    DelayDestroy = delayDestroy,
                                    Scale = scale,
                                });
                            }
                            break;
                        }
                        //移除特效特效必须在master身上才有效
                        case ESkillAction.RemoveEffect:
                        {
                            var resourceId = (int)actionConfig.Param1;
                            if (CreatureLookup.HasComponent(action.Entity))
                            {
                                if (ChildLookup.TryGetBuffer(action.Entity, out var xChildren))
                                {
                                    foreach (var child in xChildren)
                                    {
                                        if (EffectLookup.TryGetComponent(child.Value, out var effectProperties))
                                        {
                                            if (effectProperties.ResourceId == resourceId)
                                            {
                                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = child.Value });
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("RemoveEffect error, 选择目标为坐标点， 目标点必须是Creature才生效");
                            }

                            break;
                        }
                        case ESkillAction.SummonMonsterRandom:
                        {
                            var monsterId1 = (int)actionConfig.Param1;
                            var monsterId2 = (int)actionConfig.Param2;
                            var monsterId3 = (int)actionConfig.Param3;
                            var monsterId4 = (int)actionConfig.Param4;

                            var list = new NativeList<int>(Allocator.Temp);
                            if (monsterId1 > 0) list.Add(monsterId1);
                            if (monsterId2 > 0) list.Add(monsterId2);
                            if (monsterId3 > 0) list.Add(monsterId3);
                            if (monsterId4 > 0) list.Add(monsterId4);

                            var monsterId = list[random.ValueRW.Value.NextInt(0, list.Length)];
                            list.Dispose();

                            //召唤一个怪物
                            Ecb.AppendToBuffer(sortKey, GlobalEntity, new SummonMonsterCreateBuffer
                            {
                                MonsterId = monsterId,
                                Parent = master.Value,
                                BornPos = actionPos,
                            });
                            break;
                        }

                        case ESkillAction.CreateMonster:
                        {
                            var monsterId = (int)actionConfig.Param1;
                            var hpPercent = actionConfig.Param2;
                            var isBoss = (int)actionConfig.Param3 == 1;
                            var count = actionConfig.Param4.ToInt();

                            if (hpPercent <= 0)
                            {
                                hpPercent = 1f;
                            }

                            if (count <= 0)
                            {
                                count = 1;
                            }

                            if (CreatureLookup.TryGetComponent(master.Value, out var masterCreature))
                            {
                                for (var c = 0; c < count; c++)
                                {
                                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new MonsterCreateBuffer
                                    {
                                        MonsterId = monsterId,
                                        HpPercent = hpPercent,
                                        BornPos = actionPos,
                                        IsBoss = isBoss,
                                        TeamId = masterCreature.AtkValue.Team,
                                    });
                                }
                            }

                            break;
                        }

                        //召唤、孵化
                        case ESkillAction.SummonMonster:
                        case ESkillAction.Hatch:
                        {
                            var monsterId = (int)actionConfig.Param1;
                            var useParentAsSummoner = (int)actionConfig.Param3 == 1;

                            var parent = master.Value;
                            if (CreatureLookup.TryGetComponent(parent, out var masterCreature))
                            {
                                if (useParentAsSummoner && CreatureLookup.HasComponent(masterCreature.SummonParent))
                                {
                                    parent = masterCreature.SummonParent;
                                }
                            }


                            //孵化的情况需要自杀，并且把孵化者的角度作为召唤物环绕的初始角度
                            var bornAngle = 0f;
                            if (actionConfig.Action == ESkillAction.Hatch)
                            {
                                if (CreatureLookup.TryGetComponent(action.Entity, out var creature))
                                {
                                    if (creature.Type != ECreatureType.Player)
                                    {
                                        Ecb.SetComponent(sortKey, master.Value, new EnterDieTag { FromHatch = true });
                                        Ecb.SetComponentEnabled<EnterDieTag>(sortKey, master.Value, true);
                                    }
                                    else
                                    {
                                        Debug.LogError("Hatch Acton不支持Character");
                                    }
                                }

                                if (MonsterMoveLookup.TryGetComponent(action.Entity, out var monsterMove) && monsterMove.Mode == EMonsterMoveMode.Round)
                                {
                                    bornAngle = monsterMove.AroundAngle;
                                }
                            }

                            //召唤一个怪物
                            Ecb.AppendToBuffer(sortKey, GlobalEntity, new SummonMonsterCreateBuffer
                            {
                                MonsterId = monsterId,
                                Parent = parent,
                                BornPos = actionPos,
                                BornAngle = bornAngle,
                            });
                            break;
                        }
                        //禁止移动
                        case ESkillAction.StopMove:
                        {
                            var delay = actionConfig.Param1;
                            var turnToTarget = actionConfig.Param2.ToInt() == 1;
                            var toMaster = actionConfig.Param3.ToInt() == 1;
                            var stopEntity = toMaster ? master.Value : action.Entity;

                            if (CreatureLookup.HasComponent(stopEntity) && TransformLookup.TryGetComponent(stopEntity, out var stopTrans))
                            {
                                if (turnToTarget)
                                {
                                    var dirToTarget = math.normalizesafe(actionPos - stopTrans.Position);
                                    Ecb.AppendToBuffer(sortKey, stopEntity, new CreatureDataProcess { Type = ECreatureDataProcess.Turn, Float3Value = dirToTarget });
                                    Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, stopEntity, true);
                                }

                                Ecb.SetComponent(sortKey, stopEntity, new DisableMoveTag
                                {
                                    DestroyDelay = delay
                                });
                                Ecb.SetComponentEnabled<DisableMoveTag>(sortKey, stopEntity, true);
                            }

                            break;
                        }
                        case ESkillAction.ResumeMove:
                        {
                            var toMaster = actionConfig.Param1.ToInt() == 1;

                            var stopEntity = toMaster ? master.Value : action.Entity;
                            if (CreatureLookup.HasComponent(stopEntity))
                            {
                                Ecb.SetComponentEnabled<DisableMoveTag>(sortKey, stopEntity, false);
                            }

                            break;
                        }

                        case ESkillAction.DrawLine:
                        {
                            Debug.LogError("todo drawline");
                            /*var time = actionConfig.Param1;
                            var resourceId = (int)actionConfig.Param2;
                            var width = actionConfig.Param3;
                            var count = actionConfig.Param4.ToInt();
                            var minAngle = actionConfig.Param5;
                            var maxAngle = actionConfig.Param6;
                            var length = actionConfig.Param7;

                            var centerPos = actionPos;
                            if (MonsterLookup.TryGetComponent(master.Value, out var monster))
                            {
                                centerPos = monster.BornPos;
                            }

                            var startPos = centerPos + new float3(-length / 2f, -length / 2f, 0);

                            for (var c = 0; c < count; c++)
                            {
                                var randomAngle = random.ValueRW.Value.NextFloat(minAngle, maxAngle);

                                float3 lineForward;
                                if (c % 2 == 0)
                                {
                                    //右上
                                    lineForward = MathHelper.Angle2Forward(90 - randomAngle);
                                }
                                else
                                {
                                    //左上
                                    lineForward = MathHelper.Angle2Forward(270 + randomAngle);
                                }

                                var endPos = startPos + lineForward * length;
                                var delay = time * c;
                                var uid = FactoryHelper.GetRandomUid(random);
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                {
                                    Uid = uid,
                                    Delay = delay,
                                    CreateTime = CurrTime,
                                    ResourceId = resourceId,
                                    Pos = action.StartPos,
                                    DelayDestroy = 30,
                                    DestroyWithEntity = master.Value,
                                    Caster = master.Value,
                                    StretchData = new AnimationStretchData
                                    {
                                        Enable = true,
                                        TargetPos = endPos,
                                        StartPos = startPos,
                                        Width = width,
                                        AniTime = time,
                                        ContTime = time,
                                    }
                                });

                                if (LineDataLookup.HasBuffer(master.Value))
                                {
                                    Ecb.AppendToBuffer(sortKey, master.Value, new CreatureDrawLineData
                                    {
                                        EffectUid = uid,
                                        Start = startPos,
                                        End = endPos,
                                    });
                                    Ecb.SetComponentEnabled<CreatureDrawLineData>(sortKey, master.Value, true);
                                }

                                startPos = endPos;
                            }*/

                            break;
                        }
                        case ESkillAction.WarningLine:
                        {
                            Debug.LogError("todo warningLine");
                            /*var time = actionConfig.Param1;
                            var resourceId = (int)actionConfig.Param2;
                            var width = actionConfig.Param3;
                            var usePosition = (int)actionConfig.Param4 == 1;
                            var extraDist = actionConfig.Param5;
                            var contTime = actionConfig.Param6;

                            if (contTime <= 0)
                            {
                                contTime = time;
                            }

                            if (resourceId > 0)
                            {
                                if (TransformLookup.TryGetComponent(master.Value, out var localTrans))
                                {
                                    var actionEntity = usePosition ? Entity.Null : action.Entity;

                                    var forward = math.normalizesafe(actionPos - localTrans.Position);
                                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                    {
                                        ResourceId = resourceId,
                                        Pos = localTrans.Position,
                                        DelayDestroy = contTime,
                                        DestroyWithEntity = master.Value,
                                        Caster = master.Value,
                                        StretchData = new AnimationStretchData
                                        {
                                            Enable = true,
                                            TargetEntity = actionEntity,
                                            TargetPos = actionPos + extraDist * forward,
                                            StartPos = localTrans.Position,
                                            Width = width,
                                            AniTime = time,
                                            ContTime = contTime,
                                        }
                                    });
                                }
                                else
                                {
                                    Debug.LogError("ESkillAction.WarningLine Error, skill -> master 没有Transform?");
                                }
                            }*/

                            break;
                        }
                        case ESkillAction.WarningLineHorizSplit:
                        {
                            Debug.LogError("todo WarningLineHorizSplit");
                            /*var time = actionConfig.Param1;
                            var resourceId = (int)actionConfig.Param2;
                            var width = actionConfig.Param3;
                            var length = actionConfig.Param4;
                            var contTime = actionConfig.Param5;
                            var horizCount = actionConfig.Param6.ToInt();
                            var horizDist = actionConfig.Param7;

                            if (contTime <= 0)
                            {
                                contTime = time;
                            }

                            if (horizCount <= 0)
                            {
                                horizCount = 1;
                            }

                            if (resourceId > 0)
                            {
                                var forward = math.normalizesafe(actionPos - action.StartPos);
                                for (var c = 0; c < horizCount; c++)
                                {
                                    MathHelper.CalcHorizSplitDist(horizCount, c, horizDist);
                                    var horiz = MathHelper.CalcHorizSplitDist(horizCount, c, horizDist);
                                    var startPos = action.StartPos + math.mul(quaternion.RotateZ(math.radians(90)), forward) * horiz;
                                    var endPos = startPos + forward * length;
                                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                    {
                                        ResourceId = resourceId,
                                        Pos = startPos,
                                        DelayDestroy = contTime,
                                        DestroyWithEntity = master.Value,
                                        Caster = master.Value,
                                        StretchData = new AnimationStretchData
                                        {
                                            Enable = true,
                                            TargetEntity = Entity.Null,
                                            TargetPos = endPos,
                                            StartPos = startPos,
                                            Width = width,
                                            AniTime = time,
                                            ContTime = contTime,
                                        }
                                    });
                                }
                            }*/

                            break;
                        }
                        case ESkillAction.WarningLineSectorSplit:
                        {
                            Debug.LogError("todo WarningLineSectorSplit");
                            /*var time = actionConfig.Param1;
                            var resourceId = (int)actionConfig.Param2;
                            var width = actionConfig.Param3;
                            var length = actionConfig.Param4;
                            var contTime = actionConfig.Param5;
                            var splitCount = actionConfig.Param6.ToInt();
                            var splitAngle = actionConfig.Param7;

                            if (contTime <= 0)
                            {
                                contTime = time;
                            }

                            if (splitCount <= 0)
                            {
                                splitCount = 1;
                            }

                            if (resourceId > 0)
                            {
                                var forward = math.normalizesafe(actionPos - action.StartPos);
                                for (var c = 0; c < splitCount; c++)
                                {
                                    var splitForward = MathHelper.CalcSectorSplitForward(splitCount, c, splitAngle, forward);
                                    var endPos = action.StartPos + splitForward * length;

                                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                    {
                                        ResourceId = resourceId,
                                        Pos = action.StartPos,
                                        DelayDestroy = contTime,
                                        DestroyWithEntity = master.Value,
                                        Caster = master.Value,
                                        StretchData = new AnimationStretchData
                                        {
                                            Enable = true,
                                            TargetEntity = Entity.Null,
                                            TargetPos = endPos,
                                            StartPos = action.StartPos,
                                            Width = width,
                                            AniTime = time,
                                            ContTime = contTime,
                                        }
                                    });
                                }
                            }*/

                            break;
                        }
                        case ESkillAction.WarningCircle:
                        {
                            var time = actionConfig.Param1;
                            var resourceId = (int)actionConfig.Param2;
                            var radius = actionConfig.Param3;
                            var usePosition = (int)actionConfig.Param4 == 1;
                            var bulletId = actionConfig.Param5.ToInt();

                            if (resourceId > 0)
                            {
                                var parent = usePosition ? Entity.Null : action.Entity;
                                var parentScale = 1f;
                                float3 pos;
                                if (TransformLookup.TryGetComponent(parent, out var parentTransform))
                                {
                                    parentScale = parentTransform.Scale;
                                    pos = float3.zero;
                                }
                                else
                                {
                                    pos = actionPos;
                                }

                                //scale with bullet
                                if (bulletId > 0 && CacheHelper.GetBulletConfig(bulletId, CacheEntity, CacheLookup, out var bulletConfig))
                                {
                                    radius = BulletHelper.CalcBulletBombRadius(bulletConfig.BombRadius, bulletConfig, master.Value, false, Entity.Null, CreatureLookup, AttrLookup, AttrModifyLookup, TransformLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);
                                }

                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                {
                                    ResourceId = resourceId,
                                    Pos = pos,
                                    Parent = parent,
                                    Scale = radius * 2f / parentScale,
                                    DelayDestroy = time,
                                });
                            }

                            break;
                        }
                        case ESkillAction.AbsorbEnemy:
                        {
                            var radius = actionConfig.Param1;
                            var speed = actionConfig.Param2;
                            var contTime = actionConfig.Param3;

                            contTime = BuffHelper.CalcContTime(contTime, master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);

                            var enemies = PhysicsHelper.OverlapEnemies(master.Value, CollisionWorld, actionPos, radius, CreatureTag, DeadLookup, properties.ValueRO.AtkValue.Team);
                            for (var e = 0; e < enemies.Length; e++)
                            {
                                Ecb.SetComponentEnabled<CreatureAbsorbTag>(sortKey, enemies[e], true);
                                Ecb.SetComponent(sortKey, enemies[e], new CreatureAbsorbTag
                                {
                                    Speed = speed,
                                    Target = actionPos,
                                    DestroyDelay = contTime
                                });
                            }

                            enemies.Dispose();
                            break;
                        }
                        case ESkillAction.Taunt:
                        {
                            var radius = actionConfig.Param1;
                            var contTime = actionConfig.Param2;

                            contTime = BuffHelper.CalcContTime(contTime, master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);

                            var enemies = PhysicsHelper.OverlapEnemies(master.Value, CollisionWorld, actionPos, radius, CreatureTag, DeadLookup, properties.ValueRO.AtkValue.Team);
                            for (var e = 0; e < enemies.Length; e++)
                            {
                                Ecb.SetComponentEnabled<CreatureForceTargetTag>(sortKey, enemies[e], true);
                                Ecb.SetComponent(sortKey, enemies[e], new CreatureForceTargetTag
                                {
                                    Target = actionPos,
                                    DestroyDelay = contTime
                                });
                            }

                            enemies.Dispose();
                            break;
                        }
                        //增加最大血量
                        case ESkillAction.AddMaxHp:
                        {
                            if (CreatureLookup.HasComponent(action.Entity))
                            {
                                var value = (int)actionConfig.Param1;
                                var percent = actionConfig.Param2;

                                Ecb.AppendToBuffer(sortKey, action.Entity, new CreatureDataProcess { Type = ECreatureDataProcess.AddMaxHp, AddValue = value, AddPercent = percent });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, action.Entity, true);
                            }
                            else
                            {
                                Debug.LogError("技能AddMaxHp错误, 目标是个坐标，不是creature?");
                            }

                            break;
                        }
                        //增加护盾 
                        case ESkillAction.AddShield:
                        {
                            var percent = actionConfig.Param1;
                            var contTime = actionConfig.Param2;

                            if (CreatureLookup.HasComponent(action.Entity))
                            {
                                Ecb.AppendToBuffer(sortKey, action.Entity, new CreatureDataProcess
                                {
                                    Type = ECreatureDataProcess.AddShield,
                                    AddPercent = percent,
                                    FloatValue1 = contTime, //持续时间
                                });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, action.Entity, true);
                            }
                            else
                            {
                                Debug.LogError("技能AddShield错误, 目标是个坐标，不是creature?");
                            }

                            break;
                        }
                        //掉落
                        case ESkillAction.DropItem:
                        {
                            var dropItemId = actionConfig.Param1.ToInt();
                            var dropItemCount = actionConfig.Param2.ToInt();

                            if (dropItemCount <= 0)
                            {
                                dropItemCount = 1;
                            }

                            Ecb.AppendToBuffer(sortKey, GlobalEntity, new DropItemCreateBuffer
                            {
                                Pos = actionPos,
                                DropItemId = dropItemId,
                                Count = dropItemCount,
                                RandomRange = 0.5f,
                            });
                            break;
                        }

                        case ESkillAction.PlaySound:
                        {
                            var soundId = actionConfig.Param1.ToInt();
                            Ecb.AppendToBuffer(sortKey, GlobalEntity, new PlaySoundBuffer { SoundId = soundId });
                            break;
                        }
                        //播放动作
                        case ESkillAction.PlayAnimation:
                        {
                            var animationId = actionConfig.Param1.ToInt();
                            if (CreatureLookup.HasComponent(master.Value))
                            {
                                if (EventPlayAnimation.HasComponent(master.Value))
                                {
                                    Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Value = animationId });
                                    Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                }
                            }
                            else
                            {
                                Debug.LogError("Skill Action: PlayAnimation Error, 目标不是Creature");
                            }
                            break;
                        }
                        //播放动作
                        case ESkillAction.PlayAnimationIdle:
                        {
                            if (CreatureLookup.HasComponent(master.Value))
                            {
                                if (EventPlayAnimation.HasComponent(master.Value))
                                {
                                    Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = EAnimationType.Idle });
                                    Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                }
                            }
                            else
                            {
                                Debug.LogError("Skill Action: PlayAnimation Error, 目标不是Creature");
                            }

                            break;
                        }
                        case ESkillAction.PlayAtk:
                        {
                            if (CreatureLookup.HasComponent(master.Value))
                            {
                                if (EventPlayAnimation.HasComponent(master.Value))
                                {
                                    Ecb.SetComponent(sortKey, master.Value, new HybridEvent_PlayAnimation { Type = EAnimationType.Atk });
                                    Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, master.Value, true);
                                }
                            }
                            else
                            {
                                Debug.LogError("Skill Action: PlayAnimation Error, 目标不是Creature");
                            }

                            break;
                        }

                        //自杀
                        case ESkillAction.ImmediatelyDie:
                        {
                            var banDeadTrigger = actionConfig.Param1.ToInt() == 1;
                            if (CreatureLookup.TryGetComponent(action.Entity, out var creature))
                            {
                                //暂时不对玩家生效
                                if (creature.Type != ECreatureType.Player)
                                {
                                    Ecb.SetComponent(sortKey, action.Entity, new EnterDieTag { BanTrigger = banDeadTrigger });
                                    Ecb.SetComponentEnabled<EnterDieTag>(sortKey, action.Entity, true);
                                }
                                else
                                {
                                    Debug.LogError("ImmediatelyDie Acton不支持Character");
                                }
                            }

                            break;
                        }
                        //绑定技能
                        case ESkillAction.AddSkill:
                        {
                            var skillId = actionConfig.Param1.ToInt();
                            var forceMaster = actionConfig.Param2.ToInt() == 1;
                            var toEntity = forceMaster ? master.Value : action.Entity;

                            if (TransformLookup.TryGetComponent(toEntity, out var masterTrans))
                            {
                                SkillHelper.AddSkill(GlobalEntity, toEntity, skillId, properties.ValueRO.AtkValue, masterTrans.Position, Ecb, sortKey);
                            }

                            break;
                        }
                        case ESkillAction.AddSkillToSummon:
                        {
                            var skillId = actionConfig.Param1.ToInt();

                            if (SummonEntitiesLookup.TryGetBuffer(master.Value, out var summonEntities))
                            {
                                foreach (var summonEntity in summonEntities)
                                {
                                    if (TransformLookup.TryGetComponent(summonEntity.Value, out var summonTransform) && CreatureLookup.TryGetComponent(summonEntity.Value, out var summonCreature))
                                    {
                                        SkillHelper.AddSkill(GlobalEntity, summonEntity.Value, skillId, summonCreature.AtkValue, summonTransform.Position, Ecb, sortKey);
                                    }
                                }
                            }

                            break;
                        }
                        case ESkillAction.AddSkillToMaster:
                        {
                            var skillId = actionConfig.Param1.ToInt();
                            if (CreatureLookup.TryGetComponent(master.Value, out var masterCreature))
                            {
                                if (TransformLookup.TryGetComponent(masterCreature.SummonParent, out var parentTransform) && CreatureLookup.TryGetComponent(masterCreature.SummonParent, out var parentCreature))
                                {
                                    SkillHelper.AddSkill(GlobalEntity, masterCreature.SummonParent, skillId, parentCreature.AtkValue, parentTransform.Position, Ecb, sortKey);
                                }
                            }

                            break;
                        }

                        //删除绑定技能
                        case ESkillAction.UnbindSkill:
                        {
                            var skillId = actionConfig.Param1.ToInt();
                            var skillClass = actionConfig.Param2.ToInt();

                            if (skillId > 0)
                            {
                                SkillHelper.RemoveSkill(GlobalEntity, master.Value, skillId, Ecb, sortKey);
                            }

                            if (skillClass > 0)
                            {
                                if (CacheLookup.TryGetComponent(CacheEntity, out var cache))
                                {
                                    for (var j = 0; j < cache.SkillConfig.Value.Value.Length; j++)
                                    {
                                        var sConfig = cache.SkillConfig.Value.Value[j];
                                        if (sConfig.ClassId == skillClass)
                                        {
                                            SkillHelper.RemoveSkill(GlobalEntity, master.Value, sConfig.Id, Ecb, sortKey);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                        //改变缩放
                        case ESkillAction.ChangeScale:
                        {
                            var scale = actionConfig.Param1;
                            Ecb.AppendToBuffer(sortKey, master.Value, new CreatureDataProcess { Type = ECreatureDataProcess.AddScale, AddValue = scale });
                            Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, master.Value, true);
                            break;
                        }
                        case ESkillAction.TheWorld:
                        {
                            var contTime = actionConfig.Param1;
                            var effectId = actionConfig.Param2.ToInt();

                            contTime = BuffHelper.CalcContTime(contTime, master.Value, CreatureLookup, BuffEntitiesLookup, BuffTagLookup, BuffCommonLookup);

                            //进入时停
                            Ecb.SetComponent(sortKey, GlobalEntity, new GlobalMonsterPauseData
                            {
                                InMonsterPause = true,
                                PauseMonsterTag = true,
                                ContTime = contTime,
                                Timer = 0,
                            });

                            if (effectId > 0)
                            {
                                Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                                {
                                    ResourceId = effectId,
                                    Parent = master.Value,
                                    DelayDestroy = contTime,
                                    Scale = 1f,
                                });
                            }

                            break;
                        }
                        case ESkillAction.SetMasterActive:
                        {
                            var bActive = actionConfig.Param1.ToInt() == 1;
                            if (TransformLookup.HasComponent(master.Value))
                            {
                                Ecb.AppendToBuffer(sortKey, master.Value, new CreatureDataProcess { Type = ECreatureDataProcess.SetActive, IntValue1 = bActive ? 1 : 0 });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, master.Value, true);
                            }

                            break;
                        }
                        case ESkillAction.SetWeaponActive:
                        {
                            var bActive = actionConfig.Param1.ToInt() == 1;
                            if (EventSetWeaponActive.HasComponent(master.Value))
                            {
                                Ecb.SetComponent(sortKey, master.Value, new HybridEvent_SetWeaponActive
                                {
                                    Value = bActive
                                });
                                Ecb.SetComponentEnabled<HybridEvent_SetWeaponActive>(sortKey, master.Value, true);
                            }
                            break;
                        }
                        case ESkillAction.Teleport:
                        {
                            if (CreatureLookup.HasComponent(master.Value))
                            {
                                Ecb.AppendToBuffer(sortKey, master.Value, new CreatureDataProcess { Type = ECreatureDataProcess.Teleport, Float3Value = actionPos });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, master.Value, true);
                            }

                            break;
                        }
                        case ESkillAction.SetBuffEnable:
                        {
                            var bActive = actionConfig.Param1.ToInt() == 1;
                            if (CreatureLookup.HasComponent(action.Entity))
                            {
                                Ecb.SetComponentEnabled<DisableBuffTag>(sortKey, action.Entity, !bActive);
                            }

                            break;
                        }
                        case ESkillAction.ChangeSummonMoveType:
                        {
                            var monsterId = actionConfig.Param1.ToInt();
                            var moveType = actionConfig.Param2.ToInt();
                            var moveParam1 = actionConfig.Param3;
                            var moveParam2 = actionConfig.Param4;
                            var moveParam3 = actionConfig.Param5;
                            var moveParam4 = actionConfig.Param6;
                            var moveParam5 = actionConfig.Param7;
                            var moveParam6 = actionConfig.Param8;

                            if (SummonEntitiesLookup.TryGetBuffer(action.Entity, out var summonEntities))
                            {
                                foreach (var summonEntity in summonEntities)
                                {
                                    if (MonsterLookup.TryGetComponent(summonEntity.Value, out var monster))
                                    {
                                        if (monster.Id == monsterId)
                                        {
                                            Ecb.SetComponent(sortKey, summonEntity.Value, new MonsterMove
                                            {
                                                Mode = (EMonsterMoveMode)moveType,
                                                Param1 = moveParam1,
                                                Param2 = moveParam2,
                                                Param3 = moveParam3,
                                                Param4 = moveParam4,
                                                Param5 = moveParam5,
                                                Param6 = moveParam6
                                            });
                                        }
                                    }
                                }
                            }

                            break;
                        }
                        case ESkillAction.SmallMonsterToElite:
                        {
                            //只能给local player加的
                            if (LocalPlayerLookup.HasComponent(master.Value))
                            {
                                Ecb.AppendToBuffer(sortKey, master.Value, new CreatureDataProcess
                                {
                                    Type = ECreatureDataProcess.SmallMonsterToElite,
                                });
                                Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, master.Value, true);
                            }
                            break;
                        }
                        default:
                        {
                            Debug.LogError($"配置了不存在的SkillAction:{actionConfig.Action}");
                            break;
                        }
                    }
                }


                tempShareData.Dispose();
                actionBuffers.Clear();
                Ecb.SetComponentEnabled<SkillActionBuffer>(sortKey, entity, false);
            }
        }
    }
}