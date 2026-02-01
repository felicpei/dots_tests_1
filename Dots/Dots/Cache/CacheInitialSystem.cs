using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dots
{
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CacheInitialSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CacheInitTag>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<CacheInitTag>())
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entity = SystemAPI.GetSingletonEntity<CacheInitTag>();

            //remove tag
            ecb.RemoveComponent<CacheInitTag>(entity);
            var globalCache = new CacheProperties();

            //resource deploy
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheResourceCfg>();
                
                var tab = Table.GetResourceList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new ResourceCfg
                    {
                        Id = deploy.Id,
                        Scale = deploy.Scale <= 0 ? 1f : deploy.Scale,
                        CenterY = deploy.CenterY <= 0 ? 1.5f : deploy.CenterY,
                    };
                    i++;
                }

                globalCache.ResourceConfig = builder.CreateBlobAssetReference<CacheResourceCfg>(Allocator.Persistent);
                builder.Dispose();
            }
            
            //element deploy
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheElementConfig>();
                var tab = Table.GetElementList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new ElementConfig
                    {
                        Id = deploy.Id,
                        bCont = deploy.IsCont,
                        buffRes = deploy.BuffRes,
                        contTime = deploy.ContTime,
                        None = deploy.None,
                        Water = deploy.Water,
                        Fire = deploy.Fire,
                        Ice = deploy.Ice,
                        Lighting = deploy.Lighting,
                        Stone = deploy.Stone,
                    };
                    i++;
                }

                globalCache.CacheElementConfig = builder.CreateBlobAssetReference<CacheElementConfig>(Allocator.Persistent);
                builder.Dispose();
            }
            
            //cache local player deploy
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var cacheRoot = ref builder.ConstructRoot<CacheLocalPlayerConfig>();
                var tab = Table.GetLocalPlayerList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new LocalPlayerConfig
                    {
                        Id = deploy.Id,
                        ResId = deploy.ResId,
                        ServantMoveSpeed = deploy.ServantMoveSpeed
                    };
                    i++;
                }

                globalCache.LocalPlayerConfig = builder.CreateBlobAssetReference<CacheLocalPlayerConfig>(Allocator.Persistent);
            }
            
            //cache damage number deploy
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var cacheRoot = ref builder.ConstructRoot<CacheDamageNumberConfig>();
                var tab = Table.GetDamageNumberList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new DamageNumberConfig
                    {
                        Id = deploy.Id,
                        Color = new float4(deploy.R, deploy.G, deploy.B, 1),
                        Prefix = CacheHelper.CreateRandomArray(DamageNumberLetter.GetIndexByString(deploy.Prefix).ToArray())
                    };
                    i++;
                }

                globalCache.DamageNumberConfig = builder.CreateBlobAssetReference<CacheDamageNumberConfig>(Allocator.Persistent);
            }
            
            //bullet deploy
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheBulletConfig>();
                var tab = Table.GetBulletList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new BulletConfig
                    {
                        Id = deploy.Id,
                        ElementId = deploy.ElementId,
                        ResourceId = deploy.ResourceId,
                        ClassId = deploy.ClassId,
                        Scale = deploy.Scale,
                        ContTime = deploy.ContTime,
                        Velocity = deploy.Velocity,
                        Acceleration = deploy.Acceleration,
                        BombRadius = deploy.BombRadius,
                        BanHit = deploy.BanHit,
                        BanTurn = deploy.BanTurn,
                        AttachBuffId = deploy.AttachBuffId,
                        HitForce = deploy.HitForce,
                        HorizCount = deploy.HorizCount,
                        HorizDist = deploy.HorizDist,
                        SplitCount = deploy.SplitCount,
                        SplitAngle = deploy.SplitAngle,
                        SplitBulletId = deploy.SplitBulletId,
                        DamageCount = deploy.DamageCount,
                        BounceCount = deploy.BounceCount,
                        BombEffectId = deploy.BombEffectId,
                        HitEffectId = deploy.HitEffectId,
                        DamageCalc = deploy.DamageCalc,
                        DamageFactor = deploy.DamageFactor,
                        BombSound = deploy.BombSound,
                        HitSound = deploy.HitSound,
                        BombShakeRadius = deploy.BombShakeRadius,
                        BombShakeTime = deploy.BombShakeTime,
                        HitSameTeam = deploy.HitSameTeam,
                        CollisionWithBullet = deploy.CollisionWithBullet,
                        DamageInterval = deploy.DamageInterval,
                        Behaviour = deploy.Behaviour,
                        DamageShape = deploy.DamageShape,
                        DamageShapeParam = deploy.DamageShapeParam,
                    };
                    i++;
                }

                globalCache.BulletConfig = builder.CreateBlobAssetReference<CacheBulletConfig>(Allocator.Persistent);
                builder.Dispose();
            }

            //cache BulletBehaviour
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheBulletBehaviourConfig>();
                var tab = Table.GetBulletBehaviourList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new BulletBehaviourConfig
                    {
                        Id = deploy.Id,
                        BindingGroup = deploy.BindingGroup,
                        TriggerAction = deploy.TriggerAction,
                        TriggerParam1 = deploy.TriggerParam1,
                        TriggerParam2 = deploy.TriggerParam2,
                        TriggerParam3 = deploy.TriggerParam3,
                        TriggerParam4 = deploy.TriggerParam4,
                        TriggerParam5 = deploy.TriggerParam5,
                        TriggerParam6 = deploy.TriggerParam6,
                        Action = deploy.Action,
                        Param1 = deploy.Param1,
                        Param2 = deploy.Param2,
                        Param3 = deploy.Param3,
                        Param4 = deploy.Param4,
                        Param5 = deploy.Param5,
                        Param6 = deploy.Param6,
                    };
                    i++;
                }

                globalCache.BulletBehaviourConfig = builder.CreateBlobAssetReference<CacheBulletBehaviourConfig>(Allocator.Persistent);
                builder.Dispose();
            }

            //cache DropItemTable 
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheDropItemConfig>();
                var tab = Table.GetDropItemList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new DropItemConfig
                    {
                        Id = deploy.Id,
                        ResourceId = deploy.ResourceId,
                        Speed = deploy.Speed,
                        Acceleration = deploy.Acceleration,
                        CanMagnet = deploy.CanMagnet,
                        Action = deploy.Action,
                        Param1 = deploy.Param1,
                        Param2 = deploy.Param2,
                        Param3 = deploy.Param3,
                        Param4 = deploy.Param4,
                        AutoFly = deploy.AutoFly,
                        EndFly = deploy.EndFly,
                        DestroyDelay = deploy.DestroyDelay,
                        PickupSound = deploy.PickSound,
                        RotateSpeed = deploy.RotateSpeed,
                    };
                    i++;
                }

                globalCache.DropItemConfig = builder.CreateBlobAssetReference<CacheDropItemConfig>(Allocator.Persistent);
                builder.Dispose();
            }

            //monster deploy cache
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheMonsterConfig>();
                var tab = Table.GetMonsterList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new MonsterConfig
                    {
                        Id = deploy.Id,
                        Scale = deploy.Scale,
                        EliteId = deploy.EliteId,
                        BindBulletId = deploy.BindBulletId,
                        Move = new MonsterMoveConfig
                        {
                            MoveMode = deploy.MoveMode,
                            MoveParam1 = deploy.MoveParam1,
                            MoveParam2 = deploy.MoveParam2,
                            MoveParam3 = deploy.MoveParam3,
                            MoveParam4 = deploy.MoveParam4,
                            MoveParam5 = deploy.MoveParam5,
                            MoveParam6 = deploy.MoveParam6,
                        },

                        CollisionType = deploy.CollisionType,
                        BanTurn = deploy.BanTurn,
                        BanBuff = deploy.BanBuff,
                        BanAutoTarget = deploy.BanAutoTarget,
                        BanBulletHit = deploy.BanBulletHit,
                        BanHurt = deploy.BanHurt,
                        HpFactor = deploy.HpFactor,
                        AtkFactor = deploy.AtkFactor,
                        DelayDestroySec = deploy.DelayDestroySec,
                        BornTime = deploy.BornTime,
                        BornResId = deploy.BornResId,
                        ResId = deploy.ResId,
                        BelongElementId = deploy.BelongElementId,
                        DieResId = deploy.DieResId,
                        DieSound = deploy.DieSound,
                        Skill1 = deploy.Skill1,
                        Skill2 = deploy.Skill2,
                        Skill3 = deploy.Skill3,
                        Skill4 = deploy.Skill4,
                        Skill5 = deploy.Skill5,
                    };
                    i++;
                }

                globalCache.MonsterConfig = builder.CreateBlobAssetReference<CacheMonsterConfig>(Allocator.Persistent);
                builder.Dispose();
            }
            
            //servant deploy cache
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheServantConfig>();
                var tab = Table.GetServantList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new ServantConfig
                    {
                        Id = deploy.Id,
                        BelongElementId = deploy.BelongElementId,
                        ResId = deploy.ResId,
                        DieResId = deploy.DieResId,
                        damage2 = deploy.Damage2,
                        damage3 = deploy.Damage3,
                        damage4 = deploy.Damage4,
                        damage5 = deploy.Damage5
                    };
                    i++;
                }

                globalCache.ServantConfig = builder.CreateBlobAssetReference<CacheServantConfig>(Allocator.Persistent);
                builder.Dispose();
            }

            //cache buff deploy
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheBuffConfig>();
                var tab = Table.GetBuffList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new BuffConfig
                    {
                        Id = deploy.Id,
                        Duration = deploy.Duration,
                        Group = deploy.Group,
                        LayerLimit = deploy.LayerLimit,
                        EffectId = deploy.EffectId,
                        RemoveEffectId = deploy.RemoveEffectId,
                        InfluenceSummon = deploy.InfluenceSummon,
                        SummonId = deploy.SummonId,
                        BulletId = deploy.BulletId,
                        BuffType = deploy.BuffType,
                        Param1 = deploy.Param1,
                        Param2 = deploy.Param2,
                        Param3 = deploy.Param3,
                        Param4 = deploy.Param4,
                        Param5 = deploy.Param5,
                        Param6 = deploy.Param6,
                        Param7 = deploy.Param7,
                        Param8 = deploy.Param8,
                        Color = new Color(deploy.ColorG, deploy.ColorG, deploy.ColorB, deploy.ColorA),
                    };
                    i++;
                }

                globalCache.BuffConfig = builder.CreateBlobAssetReference<CacheBuffConfig>(Allocator.Persistent);
                builder.Dispose();
            }

            //cache skill deploy
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var cacheRoot = ref builder.ConstructRoot<CacheSkillConfig>();
                var tab = Table.GetSkillList();
                var arr = builder.Allocate(ref cacheRoot.Value, tab.Count);
                var i = 0;
                foreach (var deploy in tab)
                {
                    arr[i] = new SkillConfig
                    {
                        Id = deploy.Id,
                        CastCount = deploy.CastCount,
                        MaxTriggerCount = deploy.MaxTriggerCount,
                        Group = deploy.Group,
                        ClassId = deploy.ClassId,
                        BindingGroup = deploy.BindingGroup,

                        Trigger = new SkillTriggerConfig
                        {
                            Method = deploy.TriggerMethod,
                            Param1 = deploy.TriggerParam1,
                            Param2 = deploy.TriggerParam2,
                            Param3 = deploy.TriggerParam3,
                            Param4 = deploy.TriggerParam4,
                            Param5 = deploy.TriggerParam5,
                            Param6 = deploy.TriggerParam6,
                            Param7 = deploy.TriggerParam7,
                            Param8 = deploy.TriggerParam8,
                        },

                        Target = new SkillTargetConfig
                        {
                            Method = deploy.TargetMethod,
                            Param1 = deploy.TargetParam1,
                            Param2 = deploy.TargetParam2,
                            Param3 = deploy.TargetParam3,
                            Param4 = deploy.TargetParam4,
                            Param5 = deploy.TargetParam5,
                            Param6 = deploy.TargetParam6,
                            Param7 = deploy.TargetParam7,
                            Param8 = deploy.TargetParam8,
                        },

                        Cast = new SkillCastConfig
                        {
                            Method = deploy.CastMethod,
                            Param1 = deploy.CastParam1,
                            Param2 = deploy.CastParam2,
                            Param3 = deploy.CastParam3,
                            Param4 = deploy.CastParam4,
                            Param5 = deploy.CastParam5,
                            Param6 = deploy.CastParam6,
                            Param7 = deploy.CastParam7,
                            Param8 = deploy.CastParam8,
                        },

                        End1 = new SkillActionConfig
                        {
                            Action = deploy.End1,
                            Param1 = deploy.End1Param1,
                            Param2 = deploy.End1Param2,
                            Param3 = deploy.End1Param3,
                            Param4 = deploy.End1Param4,
                            Param5 = deploy.End1Param5,
                            Param6 = deploy.End1Param6,
                            Param7 = deploy.End1Param7,
                            Param8 = deploy.End1Param8
                        },
                        End2 = new SkillActionConfig
                        {
                            Action = deploy.End2,
                            Param1 = deploy.End2Param1,
                            Param2 = deploy.End2Param2,
                            Param3 = deploy.End2Param3,
                            Param4 = deploy.End2Param4,
                            Param5 = deploy.End2Param5,
                            Param6 = deploy.End2Param6,
                            Param7 = deploy.End2Param7,
                            Param8 = deploy.End2Param8
                        },
                        End3 = new SkillActionConfig
                        {
                            Action = deploy.End3,
                            Param1 = deploy.End3Param1,
                            Param2 = deploy.End3Param2,
                            Param3 = deploy.End3Param3,
                            Param4 = deploy.End3Param4,
                            Param5 = deploy.End3Param5,
                            Param6 = deploy.End3Param6,
                            Param7 = deploy.End3Param7,
                            Param8 = deploy.End3Param8
                        },
                        End4 = new SkillActionConfig
                        {
                            Action = deploy.End4,
                            Param1 = deploy.End4Param1,
                            Param2 = deploy.End4Param2,
                            Param3 = deploy.End4Param3,
                            Param4 = deploy.End4Param4,
                            Param5 = deploy.End4Param5,
                            Param6 = deploy.End4Param6,
                            Param7 = deploy.End4Param7,
                            Param8 = deploy.End4Param8
                        },
                        End5 = new SkillActionConfig
                        {
                            Action = deploy.End5,
                            Param1 = deploy.End5Param1,
                            Param2 = deploy.End5Param2,
                            Param3 = deploy.End5Param3,
                            Param4 = deploy.End5Param4,
                            Param5 = deploy.End5Param5,
                            Param6 = deploy.End5Param6,
                            Param7 = deploy.End5Param7,
                            Param8 = deploy.End5Param8
                        },
                        End6 = new SkillActionConfig
                        {
                            Action = deploy.End6,
                            Param1 = deploy.End6Param1,
                            Param2 = deploy.End6Param2,
                            Param3 = deploy.End6Param3,
                            Param4 = deploy.End6Param4,
                            Param5 = deploy.End6Param5,
                            Param6 = deploy.End6Param6,
                            Param7 = deploy.End6Param7,
                            Param8 = deploy.End6Param8
                        },
                    };
                    i++;
                }

                globalCache.SkillConfig = builder.CreateBlobAssetReference<CacheSkillConfig>(Allocator.Persistent);
                builder.Dispose();
            }

            ecb.AddComponent(entity, globalCache);
            ecb.AddComponent<CacheInitialed>(entity);
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            GameInit.IsCacheReady = true;
        }
    }
}