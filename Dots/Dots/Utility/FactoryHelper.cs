using Deploys;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Dots
{
    public enum EBuffFrom
    {
        None,
        Skill,
        Buff,
        Bullet,
        Element,
    }

    public static class FactoryHelper
    {
        public static Entity InstantiateEntity(Entity prefab, EntityCommandBuffer ecb)
        {
            var entity = ecb.Instantiate(prefab);
            return entity;
        }

        public static void RemoveAllEffect(Entity entity, Entity globalEntity, BufferLookup<Child> childrenLookup, ComponentLookup<EffectProperties> effectLookup,
            EntityCommandBuffer.ParallelWriter ecb, int sortKey)
        {
            if (childrenLookup.TryGetBuffer(entity, out var children))
            {
                for (var i = children.Length - 1; i >= 0; i--)
                {
                    var child = children[i].Value;
                    if (effectLookup.HasComponent(child))
                    {
                        ecb.AppendToBuffer(sortKey, globalEntity, new EntityDestroyBuffer { Value = child });
                    }
                }
            }
        }

        public static void RemoveAllEffect(Entity entity, Entity globalEntity, BufferLookup<Child> childrenLookup, ComponentLookup<EffectProperties> effectLookup, EntityCommandBuffer ecb)
        {
            if (childrenLookup.TryGetBuffer(entity, out var children))
            {
                for (var i = children.Length - 1; i >= 0; i--)
                {
                    var child = children[i].Value;
                    if (effectLookup.HasComponent(child))
                    {
                        ecb.AppendToBuffer(globalEntity, new EntityDestroyBuffer { Value = child });
                    }
                }
            }
        }

        //创建伤害数字
        public static void CreateDamageNumber(CacheAspect cache, GlobalAspect global, DamageNumberCreateBuffer buffer, EntityCommandBuffer ecb)
        {
            var eDamageId = buffer.Id;
            if (buffer.Type == EDamageNumber.Cure)
            {
                eDamageId = (int)EDamageNumberId.Cure;
            }

            if (!cache.GetDamageNumberConfig(out var config, eDamageId))
            {
                return;
            }

            var width = 0.7f; //数字间隔
            var number = buffer.Value;
            var arr = new NativeList<long>(Allocator.Temp);
            NativeList<int> configPrefix;

            //处理数字
            if (buffer.Type == EDamageNumber.Miss)
            {
                if (cache.GetDamageNumberConfig(out var configMiss, (int)EDamageNumberId.Miss))
                {
                    configPrefix = CacheHelper.RandomToNativeList(configMiss.Prefix);
                }
                else
                {
                    Debug.LogError("get damage number config miss error!");
                    configPrefix = new NativeList<int>(Allocator.Temp);
                }
            }
            else if (buffer.Type == EDamageNumber.Shield)
            {
                if (cache.GetDamageNumberConfig(out var configShield, (int)EDamageNumberId.Shield))
                {
                    configPrefix = CacheHelper.RandomToNativeList(configShield.Prefix);
                }
                else
                {
                    Debug.LogError("get damage number config shield error!");
                    configPrefix = new NativeList<int>(Allocator.Temp);
                }
            }
            else
            {
                configPrefix = CacheHelper.RandomToNativeList(config.Prefix);
                var numberInfo = UIUtility.DoTransNumTo_K_M(number);
                switch (numberInfo.Suffix)
                {
                    case UIUtility.NumberSuffixType.K:
                        arr.Add(26);
                        break;
                    case UIUtility.NumberSuffixType.M:
                        arr.Add(28);
                        break;
                    case UIUtility.NumberSuffixType.B:
                        arr.Add(17);
                        break;
                    case UIUtility.NumberSuffixType.T:
                        arr.Add(35);
                        break;
                }

                var numIdxList = MathHelper.ConvertFloatToIndices(numberInfo.Value);
                for (var i = 0; i < numIdxList.Length; i++)
                {
                    arr.Add(numIdxList[i]);
                }

                numIdxList.Dispose();
            }

            //处理前缀
            if (configPrefix.Length > 0)
            {
                for (var i = configPrefix.Length - 1; i >= 0; i--)
                {
                    arr.Add(configPrefix[i]);
                }
            }

            configPrefix.Dispose();

            var parent = InstantiateEntity(global.Empty, ecb);
            const float scale = 0.25f;


            //元素反应图标
            var startX = -arr.Length / 2f * width;

            if (buffer.Reaction != EElementReaction.None)
            {
                var entity = ecb.Instantiate(global.Empty);
                startX -= width * 1.75f;
                var pos = new float3(startX * scale, 0, 0);

                ecb.AddComponent(entity, new Parent { Value = parent });
                ecb.AddComponent(entity, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale * 1.5f));
                ecb.AddComponent(entity, new HybridLinkTag
                {
                    Type = EHybridType.DamageElement,
                    Id = (int)buffer.Reaction,
                    Color = config.Color,
                });
                startX += width * 1.75f;
            }

            //整体缩放
            {
                for (var i = arr.Length - 1; i >= 0; i--)
                {
                    var tile = arr[i];
                    var entity = ecb.Instantiate(global.Empty);
                    var pos = new float3(startX * scale, 0, 0);

                    ecb.AddComponent(entity, new Parent { Value = parent });
                    ecb.AddComponent(entity, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale));
                    ecb.AddComponent(entity, new HybridLinkTag
                    {
                        Type = EHybridType.DamageNumber,
                        Tile = (int)tile,
                        Color = config.Color,
                    });
                    startX += width;
                }

                arr.Dispose();
            }

            //头顶飘出
            var toCameraDist = math.distance(global.CameraPos, buffer.Position);

            //todo 克制与被克制
            float toScale;
            float originScale = toCameraDist / 15f;
            var moveTime = 0.25f;
            var moveSpeed = 3f;

            switch (buffer.Type)
            {
                case EDamageNumber.Crit:
                    originScale *= 1.25f;
                    toScale = originScale * 1.5f;
                    break;
                case EDamageNumber.SuperCrit:
                    originScale *= 1.75f;
                    toScale = originScale * 1.5f;
                    break;
                case EDamageNumber.FatalHit:
                    originScale *= 2f;
                    toScale = originScale * 1.5f;
                    break;
                default:
                    toScale = originScale * 1.3f;
                    break;
            }

            ecb.AddComponent(parent, LocalTransform.FromPositionRotationScale(buffer.Position, global.CameraRotation, originScale));
            ecb.AddComponent(parent, new DamageNumberProperties
            {
                MaxMoveTime = moveTime,
                MoveSpeed = moveSpeed,
                ToScale = toScale,
                OriginScale = originScale,
            });
        }

        //创建角色
        public static void CreatePlayer(CollisionWorld world, int localPlayerId, GlobalAspect global, CacheAspect cache, EntityCommandBuffer ecb)
        {
            if (!cache.GetLocalPlayerConfig(localPlayerId, out var playerConfig))
            {
                Debug.LogError($"CreatePlayer GetLocalPlayerConfig, 找不到配置, localPlayerId:{localPlayerId}");
                return;
            }

            if (!cache.GetResourceConfig(playerConfig.ResId, out var resCfg))
            {
                Debug.LogError($"CreatePlayer GetResourceConfig, 找不到配置, localPlayerId:{localPlayerId} resId: {playerConfig.ResId}");
                return;
            }

            var entity = InstantiateEntity(global.Prefabs.Player, ecb);

            //初始化为Creature
            var playerProps = global.PlayerProps;
            playerProps.RepelCd = 0;

            CreatureHelper.InitAsCreature(ECreatureType.Player, resCfg.CenterY, EElement.None, global.CreateRandomSeed(), ETeamId.Player, playerProps, 0,
                ecb, entity, resCfg.Scale, Entity.Null, 0);

            var bornPos = PhysicsHelper.GetGroundPos(global.PlayerBornPos, world);

            //set transform
            ecb.SetComponent(entity, new LocalTransform
            {
                Position = bornPos,
                Rotation = quaternion.identity,
                Scale = resCfg.Scale,
            });

            //add properties
            ecb.AddComponent(entity, new LocalPlayerTag
            {
                Id = localPlayerId,
                Level = 0,
                NeedExp = 0,
            });

            //add attr
            ecb.AddComponent(entity, new PlayerAttrData
            {
                MaxHp = 0,
                Recovery = 0,
                MoveSpeed = 0,
                Armor = 0,
                Dodge = 0,
                Crit = 0,
                CritDamage = 0,
                Damage = 0,
                PhysicsAtk = 0,
                FireAtk = 0,
                WaterAtk = 0,
                IceAtk = 0,
                LightingAtk = 0,
                StoneAtk = 0,
                SkillSpeed = 0,
                BulletSpeed = 0,
                DamageInterval = 0,
                ShootCount = 0,
                DamageRange = 0,
                ExpFactor = 0,
                Lucky = 0,
                Interest = 0,
                BossDamage = 0,
                EnemySpeed = 0,
                EnemyCount = 0,
                PickupRange = 0,
            });
            ecb.AddBuffer<PlayerAttrModify>(entity);

            //ui update buffer
            ecb.AddBuffer<UIUpdateBuffer>(entity);

            //dps统计
            ecb.AddBuffer<DpsAppendBuffer>(entity);
            ecb.AddBuffer<ServantList>(entity);
            ecb.SetComponentEnabled<DpsAppendBuffer>(entity, false);

            ecb.AddBuffer<PlayerDpsBuffer>(entity);

            ecb.AddComponent<StatusHpRecovery>(entity);

            //init tag
            ecb.AddComponent<PlayerInitTag>(entity);

            //Hybrid GameObject
            ecb.AddComponent(entity, new HybridLinkTag
            {
                Type = EHybridType.LocalPlayer,
                ResId = resCfg.Id,
                InitPos = bornPos,
                Id = 1,
                CreatureType = ECreatureType.Player,
            });
            ecb.SetComponentEnabled<HybridLinkTag>(entity, true);
        }

        public static void CreateServant(Entity localPlayerEntity, CollisionWorld world, GlobalAspect global,
            CacheAspect cache, int servantId, int uniqueId, ERarity rarity, int idx, FightProps props, float3 bornPos, EntityCommandBuffer ecb)
        {
            //deploy
            if (!cache.GetServantConfig(servantId, out var config))
            {
                Debug.LogError($"CreateServant Error, 找不到配置, monsterId:{servantId}");
                return;
            }

            if (!cache.GetResourceConfig(config.ResId, out var resCfg))
            {
                Debug.LogError($"CreateServant GetResourceConfig, 找不到配置, servantId:{servantId}");
                return;
            }

            var isMain = idx == 0;

            //刷新怪物
            var scale = resCfg.Scale;
            var prefab = global.Prefabs.Servant;
            var servant = InstantiateEntity(prefab, ecb);

            //bornPos
            bornPos = PhysicsHelper.GetGroundPos(bornPos, world);

            var moveSpeed = 6f;
            if (cache.GetLocalPlayerConfig(global.LocalPlayerId, out var localPlayerConfig))
            {
                moveSpeed = localPlayerConfig.ServantMoveSpeed;
            }
            else
            {
                Debug.LogError($"get local player config error, id:{global.LocalPlayerId}");
            }

            //根据品质提升攻击力
            var atkFactor = 1f;
            switch (rarity)
            {
                case ERarity.R1:
                    atkFactor = 1f;
                    break;
                case ERarity.R2:
                    atkFactor = config.damage2;
                    break;
                case ERarity.R3:
                    atkFactor = config.damage3;
                    break;
                case ERarity.R4:
                    atkFactor = config.damage4;
                    break;
                case ERarity.R5:
                    atkFactor = config.damage5;
                    break;
                default:
                {
                    Debug.LogError($"calc servant rarity error, rarity:{rarity}");
                    break;
                }
            }

            //初始化为Creature
            var team = ETeamId.Player;
            CreatureHelper.InitAsCreature(ECreatureType.Servant, resCfg.CenterY, config.BelongElementId, global.CreateRandomSeed(), team, props, moveSpeed, ecb, servant, scale, localPlayerEntity, config.Id,
                atkFactor: atkFactor);

            //monster properties
            ecb.AddComponent(servant, new ServantProperties
            {
                Id = config.Id,
                Idx = idx,
                Rarity = rarity,
                UniqueId = uniqueId,
            });
            ecb.AddComponent<StatusHpRecovery>(servant);

            MathHelper.forward2Rotation(MathHelper.Down, out var rot);
            ecb.SetComponent(servant, new LocalTransform
            {
                Position = bornPos,
                Scale = scale,
                Rotation = rot,
            });

            if (isMain)
            {
                ecb.AddComponent(servant, new MainServantTag
                {
                    initTempPos = bornPos,
                });

                //拾取buffer
                ecb.AddBuffer<PickupBuffer>(servant);
                ecb.SetComponentEnabled<PickupBuffer>(servant, false);

                ecb.AddComponent<PickupMagnetTag>(servant);
                ecb.SetComponentEnabled<PickupMagnetTag>(servant, false);

                ecb.AddComponent<PickupSkillTag>(servant);
                ecb.SetComponentEnabled<PickupSkillTag>(servant, false);
            }

            //初始化标记
            ecb.AddComponent(servant, new ServantInitTag
            {
                Rarity = rarity,
            });
            ecb.AppendToBuffer(localPlayerEntity, new SummonEntities { Value = servant });

            ecb.AddComponent<ServantDestroyTag>(servant);
            ecb.SetComponentEnabled<ServantDestroyTag>(servant, false);

            ecb.AddComponent<ServantLockForward>(servant);
            ecb.SetComponentEnabled<ServantLockForward>(servant, false);

            ecb.AddBuffer<ServantWeaponPosList>(servant);

            //add to player list
            ecb.AppendToBuffer(localPlayerEntity, new ServantList
            {
                Value = servant
            });

            //Hybrid GameObject
            ecb.AddComponent(servant, new HybridLinkTag
            {
                Type = EHybridType.Servant,
                Rarity = rarity,
                ResId = config.ResId,
                InitPos = bornPos,
                Id = config.Id,
                CreatureType = ECreatureType.Servant,
            });
            ecb.SetComponentEnabled<HybridLinkTag>(servant, true);
        }

        //创建怪物
        public static Entity CreateMonster(CollisionWorld world, GlobalAspect global, CacheAspect cache, int monsterId, SpawnMonsterProperties spawn,
            Entity parent, FightProps sourceProps, ETeamId team, float3 bornPos, float3 centerPos, EntityCommandBuffer ecb,
            float autoDestroyDelay = 0, float bornAngle = 0, float formationMinDist = 0,
            float addScaleFactor = 0f, ECreatureType type = ECreatureType.None, float hpPercent = 1f)
        {
            //deploy
            if (!cache.GetMonsterConfig(monsterId, out var config))
            {
                Debug.LogError($"SpawnMonster Error, 找不到配置, monsterId:{monsterId}");
                return Entity.Null;
            }

            if (!cache.GetResourceConfig(config.ResId, out var resCfg))
            {
                Debug.LogError($"SpawnMonster GetResourceConfig, 找不到配置, monsterId:{monsterId}");
                return Entity.Null;
            }

            //刷新怪物
            var scale = resCfg.Scale * config.Scale;
            scale = BuffHelper.CalcFactor(scale, addScaleFactor);

            Entity prefab;
            switch (config.CollisionType)
            {
                case ECollisionType.Default:
                    prefab = global.Prefabs.Monster;
                    break;
                case ECollisionType.Fly:
                    prefab = global.Prefabs.MonsterFly;
                    break;
                default:
                    prefab = global.Prefabs.Monster;
                    break;
            }

            var monster = InstantiateEntity(prefab, ecb);
            centerPos = PhysicsHelper.GetGroundPos(centerPos, world);
            bornPos = PhysicsHelper.GetGroundPos(bornPos, world);

            //初始化为Creature
            var props = new FightProps
            {
                Hp = sourceProps.Hp * config.HpFactor,
                Atk = sourceProps.Atk * config.AtkFactor,
                Crit = sourceProps.Crit,
                Def = sourceProps.Def,
                CritDamage = sourceProps.CritDamage,
                RepelCd = 0, //todo hit cd
                SpeedFac = sourceProps.SpeedFac
            };

            var creatureType = type != ECreatureType.None ? type : spawn.MonsterType;
            CreatureHelper.InitAsCreature(creatureType, resCfg.CenterY, config.BelongElementId, global.CreateRandomSeed(), team, props,
                0f, ecb, monster, scale, parent, config.Id, hpPercent);

            //monster properties
            ecb.AddComponent(monster, new MonsterProperties
            {
                Id = config.Id,
                SpawnPointId = spawn.Id,
                BornPos = bornPos,
                CenterPos = centerPos,
            });

            //移动方式相关初始化
            var moveConfig = spawn.MoveConfig.MoveMode != EMonsterMoveMode.None ? spawn.MoveConfig : config.Move;
            if (moveConfig.MoveMode != EMonsterMoveMode.None)
            {
                var moveComponent = new MonsterMove
                {
                    Mode = moveConfig.MoveMode,
                    Param1 = moveConfig.MoveParam1,
                    Param2 = moveConfig.MoveParam2,
                    Param3 = moveConfig.MoveParam3,
                    Param4 = moveConfig.MoveParam4,
                    Param5 = moveConfig.MoveParam5,
                    Param6 = moveConfig.MoveParam6,
                };

                switch (moveComponent.Mode)
                {
                    case EMonsterMoveMode.Direct:
                    case EMonsterMoveMode.KeepDist:
                    case EMonsterMoveMode.RandomEnemy:
                    {
                        break;
                    }

                    case EMonsterMoveMode.Round:
                    {
                        moveComponent.AroundAngle = bornAngle;
                        break;
                    }
                    case EMonsterMoveMode.ToFormationCenter:
                    {
                        moveComponent.BornPos = bornPos;
                        moveComponent.CenterPos = centerPos;
                        moveComponent.ToCenter = true;
                        moveComponent.FormationMinDist = formationMinDist;
                        if (formationMinDist <= 0)
                        {
                            Debug.LogError($"怪物不是从Formation方式刷出的，但使用了Formation的专用移动方式ToFormationCenter, monsterId:{config.Id}  spawnId:{spawn.Id}");
                        }

                        break;
                    }
                    case EMonsterMoveMode.PatrolRandomPoint:
                    case EMonsterMoveMode.PatrolEnemyDirection:
                    {
                        moveComponent.BornPos = bornPos;
                        moveComponent.CenterPos = centerPos;
                        break;
                    }
                    case EMonsterMoveMode.PatrolLR:
                    {
                        moveComponent.BornPos = bornPos;
                        moveComponent.CenterPos = centerPos;
                        autoDestroyDelay = moveConfig.MoveParam5;
                        break;
                    }
                    case EMonsterMoveMode.MoveLR:
                    {
                        moveComponent.BornPos = bornPos;
                        break;
                    }
                }

                ecb.AddComponent(monster, moveComponent);
            }

            //刷新boss同时清理全场小怪
            if (spawn.MonsterType == ECreatureType.Boss)
            {
                ecb.SetComponent(global.Entity, new GlobalBossInfo
                {
                    MonsterId = monsterId,
                    HpPercent = 1f,
                });
                //ecb.AddComponent(global.Entity, new ClearMonsterTag { ContainBoss = false });
            }

            //延迟销毁功能
            if (autoDestroyDelay <= 0)
            {
                autoDestroyDelay = config.DelayDestroySec;
            }

            ecb.AddComponent<MonsterDelayDestroy>(monster);
            ecb.SetComponentEnabled<MonsterDelayDestroy>(monster, false);

            if (autoDestroyDelay > 0)
            {
                ecb.SetComponent(monster, new MonsterDelayDestroy
                {
                    DelayTime = autoDestroyDelay,
                    Timer = 0,
                });
                ecb.SetComponentEnabled<MonsterDelayDestroy>(monster, true);
            }

            //禁止被设置为自动瞄准目标
            ecb.SetComponentEnabled<DisableAutoTargetTag>(monster, config.BanAutoTarget);

            //禁止子弹命中
            ecb.SetComponentEnabled<DisableBulletHitTag>(monster, config.BanBulletHit);

            //禁止被伤害
            ecb.SetComponentEnabled<DisableHurtTag>(monster, config.BanHurt);

            //禁止buff
            ecb.SetComponentEnabled<DisableBuffTag>(monster, config.BanBuff);

            ecb.AddComponent<MonsterDestroyTag>(monster);
            ecb.SetComponentEnabled<MonsterDestroyTag>(monster, false);

            ecb.AddComponent<ProgressBarComponent>(monster);
            ecb.SetComponentEnabled<ProgressBarComponent>(monster, false);

            //traonsform
            MathHelper.forward2Rotation(MathHelper.Down, out var rot);
            ecb.SetComponent(monster, new LocalTransform
            {
                Position = bornPos,
                Scale = scale,
                Rotation = rot,
            });

            ecb.AddComponent<MonsterTarget>(monster);
            ecb.AddBuffer<MonsterDropInfo>(monster);


            //wave材料掉落
            if (team == ETeamId.Monster)
            {
                //drop material
                var dropProb = 0.7f + global.NoDropMaterialCount * 0.2f;
                var rand = global.Random.ValueRW.Value.NextFloat(0f, 1f);

                if (rand < dropProb)
                {
                    var dropCount = global.WavePerMaterial * (1 + global.NoDropMaterialCount) + global.WaveSaveMaterial;
                    var intDrop = (int)dropCount;
                    if (intDrop > 0)
                    {
                        ecb.AddComponent(monster, new MonsterDropMaterial
                        {
                            Count = intDrop,
                        });
                        global.WaveSaveMaterial = dropCount - intDrop;
                        global.NoDropMaterialCount = 0;
                    }
                }
                else
                {
                    global.NoDropMaterialCount += 1;
                }

                //drop gold 
                if (global.WavePerGold > 0)
                {
                    if (global.WavePerGoldProb <= 0 || global.Random.ValueRW.Value.NextFloat(0f, 1f) < global.WavePerGoldProb)
                    {
                        ecb.AddComponent(monster, new MonsterDropGold
                        {
                            Count = global.WavePerGold,
                        });
                    }
                }
            }

            //初始化标记
            ecb.AddComponent(monster, new MonsterInitTag());

            //Hybrid GameObject
            ecb.AddComponent(monster, new HybridLinkTag
            {
                Type = EHybridType.Monster,
                ResId = config.ResId,
                InitPos = bornPos,
                Id = config.Id,
                CreatureType = creatureType,
            });
            ecb.SetComponentEnabled<HybridLinkTag>(monster, true);

            return monster;
        }

        //添加skill
        public static void CreateSkill(Entity entityCreature, float currTime, float deltaTime, int skillId, GlobalAspect global, CacheAspect cache, CreateSkillBuffer buffer,
            bool isBindSkill, ComponentLookup<LocalToWorld> localToWorldLookup, EntityCommandBuffer ecb)
        {
            if (!localToWorldLookup.HasComponent(entityCreature))
            {
                Debug.LogError("add skill error no parent");
                return;
            }

            if (!cache.GetSkillConfig(skillId, out var skillConfig))
            {
                Debug.LogError($"Skill.tab 找不到SkillId:{skillId}");
                return;
            }

            var skillEntity = ecb.Instantiate(global.Empty);
            ecb.AddComponent(skillEntity, new MasterCreature { Value = entityCreature });
            ecb.AppendToBuffer(entityCreature, new SkillEntities { Value = skillEntity });
            ecb.SetComponentEnabled<SkillEntities>(entityCreature, true);

            //skill tag
            ecb.AddComponent(skillEntity, new SkillTag
            {
                Id = skillConfig.Id,
            });

            ecb.AddComponent(skillEntity, new SkillProperties
            {
                Id = skillConfig.Id,
                IsOver = false,
                AtkValue = buffer.AtkValue,
                CreateTime = currTime,
                CurrLayer = 1,
                AddedLayer = 0,
                RecursionCount = buffer.Recursion,
                MaxRecursion = buffer.MaxRecursion,
                PrevTarget = buffer.PrevTarget,
                RootSkillId = buffer.RootSkillId,
                StartInfo = new SkillStartInfo
                {
                    IsBindSkill = isBindSkill,
                    CastDelayTime = buffer.CastDelay,
                    StartEntity = buffer.StartEntity,
                    StartPos = buffer.StartPos,
                }
            });

            ecb.AddComponent(skillEntity, new RandomSeed
            {
                Value = Random.CreateFromIndex((uint)(skillConfig.Id + deltaTime * 1000000))
            });

            ecb.AddBuffer<SkillTargetTags>(skillEntity);
            ecb.SetComponentEnabled<SkillTargetTags>(skillEntity, false);

            ecb.AddBuffer<SkillLayerAddBuffer>(skillEntity);
            ecb.SetComponentEnabled<SkillLayerAddBuffer>(skillEntity, false);

            ecb.AddBuffer<SkillActionBuffer>(skillEntity);
            ecb.SetComponentEnabled<SkillActionBuffer>(skillEntity, false);

            ecb.AddBuffer<SkillTargetBuffer>(skillEntity);
            ecb.SetComponentEnabled<SkillTargetBuffer>(skillEntity, false);

            ecb.AddBuffer<SkillEndBuffer>(skillEntity);
            ecb.SetComponentEnabled<SkillEndBuffer>(skillEntity, false);

            //triggers
            ecb.AddBuffer<SkillTriggerData>(skillEntity);

            //找到BindingGroup 相同的技能, 一起加上
            if (!isBindSkill && buffer.RootSkillId == 0 && skillConfig.BindingGroup > 0)
            {
                for (var i = 0; i < cache.CacheProperties.ValueRO.SkillConfig.Value.Value.Length; i++)
                {
                    var config = cache.CacheProperties.ValueRO.SkillConfig.Value.Value[i];
                    if (config.BindingGroup == skillConfig.BindingGroup && config.Id != skillConfig.Id)
                    {
                        CreateSkill(entityCreature, currTime, deltaTime, config.Id, global, cache, buffer, true, localToWorldLookup, ecb);
                    }
                }
            }
        }

        //添加buff
        public static void CreateBuff(Entity entityCreature, CreateBuffData data, GlobalAspect global, CacheAspect cache,
            ComponentLookup<StatusSummon> summonLookup, 
            ComponentLookup<LocalTransform> transformLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, 
            ComponentLookup<BuffTag> buffTagLookup, 
            ComponentLookup<BuffCommonData> buffCommonLookup, 
            EntityCommandBuffer ecb)
        {
            if (!transformLookup.TryGetComponent(entityCreature, out var masterTrans))
            {
                Debug.LogError("CreateBuff Error, No Parent");
                return;
            }

            if (!cache.GetBuffConfig(data.BuffId, out var buffConfig))
            {
                Debug.LogError($"CreateBuff Error, No BuffId:{data.BuffId}");
                return;
            }

            //不是Overlap的话新建特效
            var hasEffect = false;
            if (buffConfig.EffectId > 0)
            {
                hasEffect = true;
                ecb.AppendToBuffer(global.Entity, new EffectCreateBuffer
                {
                    Loop = true,
                    From = EEffectFrom.Buff,
                    FromId = buffConfig.Id,
                    ResourceId = buffConfig.EffectId,
                    Parent = entityCreature,
                });
            }

            var contTime = data.ContTime == 0f ? buffConfig.Duration : data.ContTime;
            contTime = BuffHelper.CalcContTime(contTime, data.Attacker, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);

            //看是否增加、减少debuff时间
            {
                var addFactor = BuffHelper.GetBuffAddFactor(entityCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.DebuffTime, (int)buffConfig.BuffType);
                contTime = BuffHelper.CalcFactor(contTime, addFactor);
            }

            //时间增加的buff
            {
                var addFactor = BuffHelper.GetBuffAddFactor(data.Attacker, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.AttackerBuffTime, (int)buffConfig.BuffType);
                contTime = BuffHelper.CalcFactor(contTime, addFactor);
            }

            var buffEntity = ecb.Instantiate(global.Empty);

            //Bind Parent
            ecb.AddComponent(buffEntity, new MasterCreature { Value = entityCreature });
            ecb.AppendToBuffer(entityCreature, new BuffEntities { Value = buffEntity });
            ecb.SetComponentEnabled<BuffEntities>(entityCreature, true);

            //buff Tag 
            var buffTag = new BuffTag
            {
                BuffId = data.BuffId,
                BuffType = buffConfig.BuffType,
                From = data.From,
                FromId = data.FromId,
                InfluenceSummon = buffConfig.InfluenceSummon,
                InfluenceSummonId = buffConfig.SummonId,
                Group = buffConfig.Group,
                Attacker = data.Attacker,
                Master = entityCreature,
                Layer = 1,
                HasEffect = hasEffect,
            };
            ecb.AddComponent(buffEntity, buffTag);

            //buff components
            ecb.AddComponent<BuffTimeProperty>(buffEntity);
            ecb.SetComponentEnabled<BuffTimeProperty>(buffEntity, false);

            ecb.AddComponent<BuffFreeze>(buffEntity);

            ecb.AddComponent<BuffTransfusion>(buffEntity);
            ecb.SetComponentEnabled<BuffTransfusion>(buffEntity, false);

            ecb.AddComponent<BuffAppendRandomIds>(buffEntity);
            ecb.SetComponentEnabled<BuffAppendRandomIds>(buffEntity, false);

            ecb.AddComponent<BuffCommonData>(buffEntity);
            ecb.SetComponentEnabled<BuffCommonData>(buffEntity, false);

            if (contTime > 0)
            {
                ecb.SetComponent(buffEntity, new BuffTimeProperty
                {
                    StartTime = global.Time,
                    ContTime = contTime + 0.1f, //统计加个0.1秒，防止dot最后一下出不来
                });
                ecb.SetComponentEnabled<BuffTimeProperty>(buffEntity, true);
            }

            //通知buff system更新
            ecb.AddBuffer<BuffUpdateBuffer>(buffEntity);
            ecb.AppendToBuffer(buffEntity, new BuffUpdateBuffer(EBuffUpdate.Add));
            ecb.SetComponentEnabled<BuffUpdateBuffer>(buffEntity, true);
        }


        //创建子弹
        public static void CreateBullet(GlobalAspect global, CacheAspect cache, BulletCreateBuffer buffer,
            EntityCommandBuffer ecb, ComponentLookup<LocalTransform> transformLookup, ComponentLookup<StatusSummon> summonLookup,
            ComponentLookup<PlayerAttrData> attrLookup, BufferLookup<PlayerAttrModify> attrModifyLookup,
            BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, ComponentLookup<BuffCommonData> buffCommonLookup,
            BufferLookup<SkillEntities> skillEntitiesLookup, ComponentLookup<SkillTag> skillTagLookup)
        {
            var bulletId = buffer.BulletId;
            if (!cache.GetBulletConfig(bulletId, out var bulletConfig))
            {
                Debug.LogError($"Create Bullet Error, 找不到配置, BulletId:{bulletId}");
                return;
            }

            if (!transformLookup.HasComponent(buffer.ParentCreature))
            {
                Debug.LogError($"Create Bullet Error, no ParentCreature");
                return;
            }

            var resScale = 1f;
            var hasRes = false;
            var resId = 0;
            if (bulletConfig.ResourceId > 0 && cache.GetResourceConfig(bulletConfig.ResourceId, out var resConfig))
            {
                hasRes = true;
                resScale = resConfig.Scale;
                resId = resConfig.Id;
            }

            //查看buff是否替换子弹ID
            {
                var attachList = BuffHelper.GetBuffAttachInt(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.ReplaceBullet, bulletConfig.Id, bulletConfig.ClassId);
                if (attachList.Length > 0)
                {
                    var newBulletId = attachList[global.Random.ValueRW.Value.NextInt(0, attachList.Length)];

                    //拿到新的子弹ID后，需要重新更新子弹配置
                    if (cache.GetBulletConfig(newBulletId, out var newBulletConfig))
                    {
                        bulletId = newBulletId;
                        bulletConfig = newBulletConfig;
                    }
                    else
                    {
                        Debug.LogError($"Create Bullet Error, ReplaceBullet Buff替换过来的的子弹ID配置表力没有, BulletId:{bulletId}");
                    }
                }

                attachList.Dispose();
            }


            //计算缩放
            var bulletScale = bulletConfig.Scale * resScale;
            if (buffer.ScaleFactor > 0)
            {
                bulletScale *= buffer.ScaleFactor;
            }

            var bNeedDisplay = bulletScale > 0 && buffer.ForceHitCreature == Entity.Null && !buffer.ImmediatelyBomb;
            var collisionType = bulletConfig.CollisionWithBullet;

            //检查是否同故宫buff替换CollisionType
            {
                var newCollisionTypes = BuffHelper.GetBuffAttachInt(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletChangeCollisionType, bulletConfig.Id, bulletConfig.ClassId);
                if (newCollisionTypes.Length > 0)
                {
                    collisionType = (ECollisionBulletType)newCollisionTypes[0];
                }

                newCollisionTypes.Dispose();
            }

            var prefab = collisionType != ECollisionBulletType.None ? global.Prefabs.BulletPhysics : global.Empty;
            var bulletEntity = InstantiateEntity(prefab, ecb);

            //物理效果
            if (collisionType != ECollisionBulletType.None)
            {
                ecb.AddComponent(bulletEntity, new BulletCollisionTag
                {
                    Id = bulletConfig.Id,
                    CollisionWithBullet = collisionType,
                    Team = buffer.AtkValue.Team,
                });

                /*var motion = collisionType == ECollisionBulletType.PhysicCollision ? BodyMotionType.Dynamic : BodyMotionType.Static;
                PhysicsHelper.CreatePhysicsBody(bulletEntity, motion, ecb, 5, 5);*/
            }

            //获得速度
            var velocity = BulletHelper.CalcBulletVelocity(buffer.ParentCreature, bulletConfig, summonLookup, attrLookup, attrModifyLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, out var acceleration);
            if (buffer.SpeedFactor > 0)
            {
                velocity *= buffer.SpeedFactor;
            }

            //计算持续时间
            float contTime;
            if (buffer.From is EBulletFrom.Buff or EBulletFrom.Collision)
            {
                contTime = 0;
                ecb.AppendToBuffer(buffer.TransformParent, new BindingBullet
                {
                    Value = bulletEntity,
                    From = buffer.From,
                    FromId = buffer.FromId,
                });
            }
            else
            {
                contTime = bulletConfig.ContTime;
                contTime = BuffHelper.CalcContTime(contTime, buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);

                //buff bulletDist 影响
                {
                    var addFactor = BuffHelper.GetBuffAddFactor(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletContTime, bulletConfig.Id);
                    contTime = BuffHelper.CalcFactor(contTime, addFactor);
                }

                if (transformLookup.HasComponent(buffer.ForceHitCreature))
                {
                    contTime = 1;
                }
            }

            //running data
            var maxBounceCount = bulletConfig.BounceCount;
            if (maxBounceCount != -1 && !buffer.DisableBounce)
            {
                var addValue = BuffHelper.GetBuffAddValue(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletBounce, bulletConfig.Id, bulletConfig.ClassId);
                maxBounceCount += (int)addValue;
            }

            //获取穿透次数
            var maxDamageCount = bulletConfig.DamageCount;
            if (maxDamageCount > 0)
            {
                var addValue = BuffHelper.GetBuffAddValue(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletDamageCount, bulletConfig.Id, bulletConfig.ClassId);
                maxDamageCount += (int)addValue;
            }

            //角度分裂的计算
            if (!buffer.DisableSplit)
            {
                //计算分裂, 只有直线子弹允许分裂（环绕不允许）
                var horizCount = bulletConfig.HorizCount <= 0 ? 1 : bulletConfig.HorizCount;
                var horizDist = bulletConfig.HorizDist;

                BulletHelper.CalcSplitInfo(bulletConfig, buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, out var splitCount, out var splitAngle);

                //横向分裂buff
                var horizResult = BuffHelper.GetBuffFactorAndValue(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.BulletSplitHoriz, bulletConfig.Id, bulletConfig.ClassId);
                if (horizResult.AddValue > 0 || horizResult.AddFactor > 0)
                {
                    horizCount += (int)horizResult.AddValue;
                    horizCount = (int)BuffHelper.CalcFactor(horizCount, horizResult.AddFactor);
                    horizDist = horizResult.TempData;
                }

                if (splitCount > 1 || horizCount > 1)
                {
                    ecb.AddComponent(bulletEntity, new BulletSplit
                    {
                        HorizCount = horizCount,
                        HorizDist = horizDist,
                        SplitCount = splitCount,
                        SplitAngle = splitAngle,
                        SplitBulletId = bulletConfig.SplitBulletId > 0 ? bulletConfig.SplitBulletId : bulletConfig.Id,
                        NextCanSplit = bulletConfig.SplitBulletId > 0 //用自己的子弹ID只给分裂一次，否则无限递归了
                    });
                }
            }

            //random seed
            ecb.AddComponent(bulletEntity, new RandomSeed { Value = global.CreateRandomSeed(), });

            //计算爆炸范围
            var sourceRadius = buffer.BombRadius > 0 ? buffer.BombRadius : bulletConfig.BombRadius;
            var bombRadius = BulletHelper.CalcBulletBombRadius(sourceRadius, bulletConfig, buffer.ParentCreature, buffer.ImmediatelyBomb, buffer.ForceHitCreature,
                summonLookup, attrLookup, attrModifyLookup, transformLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup);

            //技能配置：向前偏移量
            var bornPos = buffer.ShootPos;

            //环绕半径
            var damageRangeFactor = AttrHelper.GetDamageRangeFactor(buffer.ParentCreature, summonLookup, attrLookup, attrModifyLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, true);
            var radiusX = BuffHelper.CalcFactor(buffer.AroundRadiusX, damageRangeFactor);
            var radiusY = BuffHelper.CalcFactor(buffer.AroundRadiusY, damageRangeFactor);
            var aroundEnable = transformLookup.HasComponent(buffer.AroundEntity) || MathHelper.IsValid(buffer.AroundPos);
            var bTrace = summonLookup.HasComponent(buffer.TraceCreature);

            ecb.AddComponent(bulletEntity, new BulletAtkValue
            {
                Atk = buffer.AtkValue.Atk,
                Crit = buffer.AtkValue.Crit,
                CritDamage = buffer.AtkValue.CritDamage,
            });

            ecb.AddComponent(bulletEntity, new BulletProperties
            {
                ElementId = bulletConfig.ElementId,
                From = buffer.From,
                FromId = buffer.FromId,
                BulletId = bulletId,
                CreateTime = global.Time,
                ContTime = contTime,
                BombRadius = bombRadius,
                Team = buffer.AtkValue.Team,
                MasterCreature = buffer.ParentCreature,
                HitForce = buffer.DisableHitForce ? 0 : bulletConfig.HitForce,
                ForceHitCreature = buffer.ForceHitCreature,
                SkillTraceTarget = buffer.TraceCreature,
                SkillEntity = buffer.SkillEntity,
                AllowSkillEndAction = buffer.AllowSkillEndAction,
                SkillEndActionIndex = buffer.SkillEndActionIndex,
                ShootPos = bornPos,
                MaxBounceCount = maxBounceCount,
                MaxDamageCount = maxDamageCount,
                RunningDirection = buffer.Direction,
                ForceDistance = buffer.ForceDistance,
                TransformParent = buffer.TransformParent,
                ResourceScale = resScale,

                //伤害区域
                DamageShape = bulletConfig.DamageShape,
                DamageShapeParam = bulletConfig.DamageShapeParam,

                //初始速度信息
                D1 = buffer.Direction,
                V1 = aroundEnable ? 0 : velocity,
                A1 = aroundEnable ? 0 : acceleration,
                VTurn = bTrace ? 30 : aroundEnable ? velocity : 0,
                ATurn = aroundEnable ? acceleration : 0,

                //环绕信息
                AroundEntity = buffer.AroundEntity,
                AroundPos = buffer.AroundPos,
                AroundRadiusX = radiusX,
                AroundRadiusY = radiusY,
                AroundAngle = buffer.AroundDefaultAngle,
                ReverseAroundDirection = buffer.ReverseAround,

                SourceScale = bulletScale,
                Offset = buffer.Offset,
            });

            var rotation = quaternion.identity;
            if (!bulletConfig.BanTurn && MathHelper.forward2Rotation(buffer.Direction, out var rotResult))
            {
                rotation = rotResult;
            }

            //bullet behaviour 相关数据
            {
                var addBehaviours = BuffHelper.GetBuffAttachInt(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.AddBulletBehaviour, bulletConfig.Id, bulletConfig.ClassId);

                //加配置表的id
                if (bulletConfig.Behaviour > 0)
                {
                    var behaviourId = bulletConfig.Behaviour;
                    var changeBehaviours = BuffHelper.GetBuffAttachInt(buffer.ParentCreature, summonLookup, buffEntitiesLookup, buffTagLookup, buffCommonLookup, EBuffType.ChangeBulletBehaviour, bulletConfig.Id, bulletConfig.ClassId, behaviourId);

                    for (var i = 0; i < changeBehaviours.Length; i++)
                    {
                        behaviourId = changeBehaviours[i];
                        break;
                    }

                    changeBehaviours.Dispose();
                    addBehaviours.Add(behaviourId);
                }

                ecb.AddComponent(bulletEntity, new BulletTriggerData { SourceAtk = buffer.AtkValue.Atk });

                ecb.AddBuffer<BulletTriggerBuffer>(bulletEntity);
                ecb.SetComponentEnabled<BulletTriggerBuffer>(bulletEntity, false);

                ecb.AddBuffer<BulletBehaviourBuffer>(bulletEntity);
                ecb.SetComponentEnabled<BulletBehaviourBuffer>(bulletEntity, false);

                ecb.AddBuffer<BulletActionBuffer>(bulletEntity);
                ecb.SetComponentEnabled<BulletActionBuffer>(bulletEntity, false);

                if (addBehaviours.Length > 0)
                {
                    for (var j = 0; j < addBehaviours.Length; j++)
                    {
                        var id = addBehaviours[j];
                        BulletHelper.AddBehaviour(global, cache, id, bulletEntity, ecb);

                        ecb.SetComponentEnabled<BulletTriggerBuffer>(bulletEntity, true);
                        ecb.SetComponentEnabled<BulletBehaviourBuffer>(bulletEntity, true);
                    }
                }

                addBehaviours.Dispose();
            }

            //init resource
            ecb.AddBuffer<BulletHitCreature>(bulletEntity);

            ecb.AddComponent<BulletAddScale>(bulletEntity);
            ecb.SetComponentEnabled<BulletAddScale>(bulletEntity, false);

            ecb.AddComponent<BulletRepel>(bulletEntity);
            ecb.SetComponentEnabled<BulletRepel>(bulletEntity, false);

            //爆炸tag
            if (buffer.ImmediatelyBomb)
            {
                ecb.AddComponent(bulletEntity, new BulletBombTag
                {
                    Frame = 2,
                });
                ecb.SetComponentEnabled<BulletBombTag>(bulletEntity, true); //是否立即爆炸
            }
            else
            {
                ecb.AddComponent<BulletBombTag>(bulletEntity);
                ecb.SetComponentEnabled<BulletBombTag>(bulletEntity, false);
            }


            //销毁tag   
            ecb.AddComponent<BulletDestroyTag>(bulletEntity);
            ecb.SetComponentEnabled<BulletDestroyTag>(bulletEntity, false);

            //local transform 
            ecb.SetComponent(bulletEntity, new LocalTransform
            {
                Position = bornPos,
                Rotation = rotation,
                Scale = bulletScale,
            });

            //技能Trigger
            var bulletFromSkillId = 0;
            if (skillTagLookup.TryGetComponent(buffer.SkillEntity, out var skillTag))
            {
                bulletFromSkillId = skillTag.Id;
            }

            SkillHelper.DoSkillTrigger(buffer.ParentCreature, skillEntitiesLookup, skillTagLookup, new SkillTriggerData(ESkillTrigger.OnBulletCreate)
            {
                IntValue1 = bulletConfig.Id,
                IntValue2 = bulletConfig.ClassId,
                FloatValue1 = bulletFromSkillId,
            }, ecb);

            if (bNeedDisplay && hasRes)
            {
                ecb.AddComponent(bulletEntity, new HybridLinkTag
                {
                    Type = EHybridType.Bullet,
                    ResId = resId,
                    InitPos = bornPos
                });
                ecb.SetComponentEnabled<HybridLinkTag>(bulletEntity, true);
            }
        }

        public static void CreateDropItem(GlobalAspect global, CacheAspect cache, DropItemCreateBuffer buffer, EntityCommandBuffer ecb, CollisionWorld collisionWorld)
        {
            if (!cache.GetDropItemConfig(buffer.DropItemId, out var config))
            {
                Debug.LogError($"CreateDropItem Error, 找不到配置, DropItem:{buffer.DropItemId}");
                return;
            }

            if (!cache.GetResourceConfig(config.ResourceId, out var resConfig))
            {
                return;
            }

            var scale = resConfig.Scale;
            var entity = InstantiateEntity(global.Empty, ecb);

            //random seed
            ecb.AddComponent(entity, new RandomSeed
            {
                Value = global.CreateRandomSeed(),
            });

            ecb.AddComponent(entity, new DropItemProperties
            {
                Id = config.Id,
                Value = buffer.Value,
            });

            //init
            ecb.AddComponent(entity, new DropItemInitTag
            {
                Id = config.Id,
            });
            ecb.SetComponentEnabled<DropItemInitTag>(entity, true);

            //idle状态
            ecb.AddComponent(entity, new DropItemIdleTag
            {
                Id = config.Id,
                EndFly = config.EndFly,
            });
            ecb.SetComponentEnabled<DropItemIdleTag>(entity, true);

            //fly
            ecb.AddComponent<DropItemFlyTag>(entity);
            ecb.SetComponentEnabled<DropItemFlyTag>(entity, false);

            ecb.AddComponent<DropItemForceTag>(entity);
            ecb.SetComponentEnabled<DropItemForceTag>(entity, false);

            var bornPos = buffer.Pos;
            if (buffer.RandomRange > 0)
            {
                bornPos = MathHelper.RandomRangePos(global.Random, bornPos, 0, buffer.RandomRange);
            }

            bornPos = PhysicsHelper.GetGroundPos(bornPos, collisionWorld);

            ecb.SetComponent(entity, new LocalTransform
            {
                Position = bornPos,
                Rotation = quaternion.identity,
                Scale = scale,
            });

            //hybrid
            ecb.AddComponent(entity, new HybridLinkTag { Type = EHybridType.DropItem, ResId = resConfig.Id, InitPos = bornPos, Id = config.DestroyDelay > 0 ? 1 : 0 });
            ecb.SetComponentEnabled<HybridLinkTag>(entity, true);
        }


        //创建特效 
        public static void CreateEffects(GlobalAspect global, CacheAspect cache, 
            EffectCreateBuffer buffer, EntityCommandBuffer ecb,
            ComponentLookup<LocalTransform> transformLookup, ComponentLookup<StatusCenter> centerLookup)
        {
            if (!cache.GetResourceConfig(buffer.ResourceId, out var resConfig))
            {
                return;
            }

            if (buffer.Parent != Entity.Null && !transformLookup.HasComponent(buffer.Parent))
            {
                return;
            }

            var effectEntity = InstantiateEntity(global.Empty, ecb);
            ecb.AddComponent(effectEntity, new EffectProperties
            {
                ResourceId = buffer.ResourceId,
                From = buffer.From,
                FromId = buffer.FromId,
                Parent = buffer.Parent,
            });

            ecb.AddComponent<EffectDelayDestroy>(effectEntity);
            ecb.SetComponentEnabled<EffectDelayDestroy>(effectEntity, false);

            if (!buffer.Loop)
            {
                var delayDestroy = buffer.DelayDestroy > 0 ? buffer.DelayDestroy : 1f;
                ecb.SetComponent(effectEntity, new EffectDelayDestroy { DelayDestroy = delayDestroy });
                ecb.SetComponentEnabled<EffectDelayDestroy>(effectEntity, true);
            }

            var localScale = buffer.Scale > 0 ? buffer.Scale : 1f;
            localScale *= resConfig.Scale;

            var pos = buffer.Pos;
            var rotation = buffer.Rotation;
            if (transformLookup.TryGetComponent(buffer.Parent, out var parentTransform))
            {
                //bind parent
                ecb.AddComponent(effectEntity, new Parent { Value = buffer.Parent });
                if (!MathHelper.IsZero(buffer.LocalPos))
                {
                    pos = buffer.LocalPos;
                    rotation = math.inverse(rotation);
                }
                else
                {
                    if (centerLookup.TryGetComponent(buffer.Parent, out var creature))
                    {
                        //使用中心坐标点
                        pos = float3.zero;
                        pos.y = creature.CenterY;
                    }
                    else
                    {
                        pos = float3.zero;
                    }

                    rotation = math.inverse(parentTransform.Rotation);
                }
            }

            ecb.AddComponent(effectEntity, LocalTransform.FromPositionRotationScale(pos, rotation, localScale));

            //自动变大动画
            if (buffer.ScaleAni.Enable)
            {
                ecb.AddComponent(effectEntity, new AnimationScaleComponent
                {
                    ScaleMultiple = buffer.ScaleAni.ScaleMultiple,
                    MaxTime = buffer.ScaleAni.UseTime,
                    Timer = 0,
                    OriginScale = localScale
                });
            }

            if (buffer.MoveData.Enable)
            {
                ecb.AddComponent(effectEntity, new AnimationMoveComponent
                {
                    Speed = buffer.MoveData.Speed,
                    Forward = buffer.MoveData.Forward,
                    TotalTime = buffer.MoveData.TotalTime,
                    CurTime = 0
                });
            }

            if (buffer.RotateData.Enable)
            {
                ecb.AddComponent(effectEntity, new AnimationRotateComponent
                {
                    Speed = buffer.RotateData.Speed,
                    CenterPos = buffer.RotateData.CenterPos,
                    Radius = buffer.RotateData.Radius,
                    UseAtkRange = buffer.RotateData.UseAtkRange,
                    SourceScale = buffer.RotateData.SourceScale,
                    MasterCreature = buffer.RotateData.MasterCreature,
                });
            }

            //伸展的动画
            if (buffer.StretchData.Enable)
            {
                ecb.AddComponent(effectEntity, new EntityStretchAnimation
                {
                    Master = buffer.StretchData.Master,
                    TargetPos = buffer.StretchData.TargetPos,
                    StartPos = buffer.StretchData.StartPos,
                    NeedTime = buffer.StretchData.Time,
                    Width = buffer.StretchData.Width,
                });

                ecb.AddComponent(effectEntity, new ScaleXZData
                {
                    X = 1f,
                    Z = 1f,
                });
            }

            //hybrid
            ecb.AddComponent(effectEntity, new HybridLinkTag
            {
                Type = EHybridType.Effect,
                ResId = resConfig.Id,
                InitPos = pos,
            });
            ecb.SetComponentEnabled<HybridLinkTag>(effectEntity, true);
        }

        public static void CreateProgressBar(CacheAspect cache, GlobalAspect global, ProgressBarCreateBuffer buffer, EntityCommandBuffer ecb,
            ComponentLookup<LocalTransform> transformLookup, ComponentLookup<InDeadState> deadLookup,
            ComponentLookup<StatusCenter> centerLookup)
        {
            if (!transformLookup.TryGetComponent(buffer.Parent, out var parentTransform))
            {
                return;
            }

            if (deadLookup.HasComponent(buffer.Parent) && deadLookup.IsComponentEnabled(buffer.Parent))
            {
                return;
            }

            var resId = buffer.IsGreen ? (int)EResourceId.ProgressBarGreen : (int)EResourceId.ProgressBarRed;
            if (!cache.GetResourceConfig(resId, out var resConfig))
            {
                return;
            }

            var entity = InstantiateEntity(global.Empty, ecb);
            var pos = buffer.StartPos;
            if (centerLookup.TryGetComponent(buffer.Parent, out var creature))
            {
                pos = CreatureHelper.GetHeadPos(buffer.StartPos, creature, parentTransform.Scale);
            }

            ecb.SetComponent(entity, new LocalTransform
            {
                Scale = parentTransform.Scale,
                Rotation = quaternion.identity,
                Position = pos,
            });

            ecb.AddComponent(entity, new MaterialFillRate
            {
                Value = buffer.Value - 0.5f,
            });

            ecb.SetComponent(buffer.Parent, new ProgressBarComponent { Value = entity, });
            ecb.SetComponentEnabled<ProgressBarComponent>(buffer.Parent, true);

            //hybrid
            ecb.AddComponent(entity, new HybridLinkTag { Type = EHybridType.ProgressBar, ResId = resConfig.Id });
            ecb.SetComponentEnabled<HybridLinkTag>(entity, true);
        }
    }
}