using Dots;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureShootBulletSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_PlayAnimation> _eventPlayAnimationLookup;
        [ReadOnly] private ComponentLookup<ServantLockForward> _servantLockForwardLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();

            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _eventPlayAnimationLookup= state.GetComponentLookup<HybridEvent_PlayAnimation>(true); 
            _servantLockForwardLookup = state.GetComponentLookup<ServantLockForward>(true);
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
            
            _creatureLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _eventPlayAnimationLookup.Update(ref state);
            _servantLockForwardLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //射击技能子弹job
            new ShootSkillBulletJob
            {
                DeltaTime = deltaTime,
                Factory = global.Entity,
                Ecb = ecb.AsParallelWriter(),
                CreatureLookup = _creatureLookup,
                BuffCommonLookup = _buffCommonLookup,
                BuffTagLookup = _buffTagLookup,
                BuffEntitiesLookup = _buffEntitiesLookup,
                EventPlayAnimationLookup = _eventPlayAnimationLookup,
                ServantLockForwardLookup = _servantLockForwardLookup,
            }.ScheduleParallel();

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct ShootSkillBulletJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity Factory;
            
            [ReadOnly] public ComponentLookup<CreatureProperties> CreatureLookup;
            [ReadOnly] public BufferLookup<BuffEntities> BuffEntitiesLookup;
            [ReadOnly] public ComponentLookup<BuffTag> BuffTagLookup;
            [ReadOnly] public ComponentLookup<BuffCommonData> BuffCommonLookup;
            [ReadOnly] public  ComponentLookup<HybridEvent_PlayAnimation> EventPlayAnimationLookup;
            [ReadOnly] public ComponentLookup<ServantLockForward> ServantLockForwardLookup;
            
            [BurstCompile]
            private void Execute(DynamicBuffer<ShootBulletBuffer> skillShootBuffer, LocalTransform localTransform, RefRW<RandomSeed> random, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (!CreatureLookup.TryGetComponent(entity, out var creature))
                {
                    return;
                }
                
                //判断结束时间
                for (var i = skillShootBuffer.Length - 1; i >= 0; i--)
                {
                    var buffer = skillShootBuffer[i];
                    //时间到了移除
                    if (buffer.MaxContTime > 0 &&buffer.ContTimer >= buffer.MaxContTime)
                    {
                        if (buffer.MuzzleAliveTime <= 0 && buffer.HasMuzzle)
                        {
                            Ecb.AppendToBuffer(sortKey, Factory, new DestroyEffectByFrom { From = EEffectFrom.Muzzle, FromId = buffer.BulletId, Parent = entity});
                        }
                        
                        //animation
                        if (buffer.PlaySpellAni)
                        {
                            if (EventPlayAnimationLookup.HasComponent(entity))
                            {
                                Ecb.SetComponent(sortKey, entity, new HybridEvent_PlayAnimation { Type = EAnimationType.SpellEnd });
                                Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, entity, true);
                            }
                            if (ServantLockForwardLookup.HasComponent(entity))
                            {
                                Ecb.SetComponent(sortKey, entity, new ServantLockForward { ContTime = 0.1f});
                                Ecb.SetComponentEnabled<ServantLockForward>(sortKey, entity, true);
                            }
                        }
                        skillShootBuffer.RemoveAt(i);
                    }
                }
                
                var list = new NativeArray<ShootBulletBuffer>(skillShootBuffer.Length, Allocator.Temp);
                for (var i = 0; i < skillShootBuffer.Length; i++)
                {
                    list[i] = skillShootBuffer[i];
                }

                for (var i = list.Length - 1; i >= 0; i--)
                {
                    var buffer = list[i];

                    buffer.ContTimer += DeltaTime;
                    buffer.ShootTimer -= DeltaTime;

                    if (buffer.IntervalShootCount <= 0)
                    {
                        buffer.IntervalShootCount = 1;
                        Debug.LogError("creature shoot bullet error, intervalShootCount <= 0?");
                    }
                    
                    if (buffer.ShootTimer <= 0)
                    {
                        for (var j = 0; j < buffer.IntervalShootCount; j++)
                        {
                            var dir = MathHelper.RotateForward(buffer.Direction, buffer.RotateAngle * buffer.AlreadyShootCount);
                            if (buffer.RotateSpeed != 0)
                            {
                                var rotateAngle = buffer.ContTimer * buffer.RotateSpeed;
                                dir = MathHelper.RotateForward(buffer.Direction, rotateAngle);
                            }

                            //发射起点设置，如果传入了ForceShootPos，则使用该坐标
                            float3 shootPos;
                            float3 centerPos;
                            if (MathHelper.IsValid(buffer.ForceShootPos))
                            {
                                shootPos = buffer.ForceShootPos + buffer.MuzzlePos;
                                centerPos = buffer.ForceShootPos;
                            }
                            else
                            {
                                shootPos = localTransform.Position + buffer.MuzzlePos;
                                centerPos = localTransform.Position;
                            }

                            //animation
                            if (buffer.PlaySpellAni && buffer.AlreadyShootCount <= 0)
                            {
                                if (EventPlayAnimationLookup.HasComponent(entity))
                                {
                                    Ecb.SetComponent(sortKey, entity, new HybridEvent_PlayAnimation { Type = EAnimationType.SpellStart });
                                    Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, entity, true);
                                }
                                
                                if (ServantLockForwardLookup.HasComponent(entity))
                                {
                                    Ecb.SetComponent(sortKey, entity, new ServantLockForward { ContTime = 10 });
                                    Ecb.SetComponentEnabled<ServantLockForward>(sortKey, entity, true);
                                }
                            }
                            
                            //开始射击，播放枪火特效
                            if (buffer.MuzzleEffectId > 0)
                            {
                                if (buffer.MuzzleLaser)
                                {
                                    if (buffer.AlreadyShootCount <= 0)
                                    {
                                        var muzzleScale = buffer.MuzzleScale <= 0f ? 1f : buffer.MuzzleScale;
                                        var laserEffectBuffer = new EffectCreateBuffer
                                        {
                                            From = EEffectFrom.Muzzle,
                                            FromId = buffer.BulletId,
                                    
                                            Loop = true,
                                            ResourceId = buffer.MuzzleEffectId,
                                            Parent = entity,
                                            LocalPos = buffer.MuzzlePos,
                                            Scale = muzzleScale,
                                            Rotation = MathHelper.forward2RotationSafe(dir),
                                            DelayDestroy = buffer.MuzzleAliveTime,
                                            RotateData = new AnimationRotateData
                                            {
                                                Enable = buffer.RotateSpeed != 0,
                                                Speed = buffer.RotateSpeed,
                                                Radius = 0,
                                                CenterPos = CreatureHelper.getCenterPos(centerPos, creature, localTransform.Scale),
                                                UseAtkRange = true,
                                                SourceScale = muzzleScale,
                                                MasterCreature = entity,
                                            }
                                        };
                                        Ecb.AppendToBuffer(sortKey, Factory, laserEffectBuffer);

                                        if (buffer.MuzzleAliveTime <= 0)
                                        {
                                            buffer.HasMuzzle = true;
                                        }
                                    }
                                }
                                else
                                {
                                    //single muzzle
                                    var muzzleScale = buffer.MuzzleScale <= 0f ? 1f : buffer.MuzzleScale;
                                    var localPos = shootPos;
                                    var laserEffectBuffer = new EffectCreateBuffer
                                    {
                                        From = EEffectFrom.Muzzle,
                                        FromId = buffer.BulletId,
                                        Loop = false,
                                        ResourceId = buffer.MuzzleEffectId,
                                        Pos = localPos,
                                        Scale = muzzleScale,
                                        Rotation = MathHelper.forward2RotationSafe(dir),
                                        DelayDestroy = 1f,
                                    };
                                    Ecb.AppendToBuffer(sortKey, Factory, laserEffectBuffer);
                                }
                            }
                            
                            var bulletBuffer = new BulletCreateBuffer
                            {
                                BulletId = buffer.BulletId,
                                ParentCreature = entity,
                                AtkValue = new AtkValue(creature.AtkValue.Atk, creature.AtkValue.Crit, creature.AtkValue.CritDamage, creature.AtkValue.Team),
                                Direction = dir,
                                ShootPos = shootPos,
                                SkillEntity = buffer.SkillEntity,
                                DisableSplit = buffer.DisableSplit,
                                Offset = buffer.Offset,
                            };
                            Ecb.AppendToBuffer(sortKey, Factory, bulletBuffer);

                            buffer.ShootTimer = buffer.ShootInterval;
                            buffer.AlreadyShootCount += 1;
                        }
                    }

                    list[i] = buffer;
                }

                skillShootBuffer.Clear();
                skillShootBuffer.CopyFrom(list);
                list.Dispose();

                //判断结束时间
                for (var i = skillShootBuffer.Length - 1; i >= 0; i--)
                {
                    var buffer = skillShootBuffer[i];
                    
                    //时间到了移除
                    if (buffer.ContTimer >= buffer.MaxContTime)
                    {
                        if (buffer.MuzzleAliveTime <= 0 && buffer.HasMuzzle)
                        {
                            Ecb.AppendToBuffer(sortKey, Factory, new DestroyEffectByFrom { From = EEffectFrom.Muzzle, FromId = buffer.BulletId, Parent = entity});
                        }
                        
                        //animation
                        if (buffer.PlaySpellAni)
                        {
                            if (EventPlayAnimationLookup.HasComponent(entity))
                            {
                                Ecb.SetComponent(sortKey, entity, new HybridEvent_PlayAnimation { Type = EAnimationType.SpellEnd });
                                Ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(sortKey, entity, true);
                            }

                            if (ServantLockForwardLookup.HasComponent(entity))
                            {
                                Ecb.SetComponent(sortKey, entity, new ServantLockForward { ContTime = 0.1f});
                                Ecb.SetComponentEnabled<ServantLockForward>(sortKey, entity, true);
                            }
                        }
                        skillShootBuffer.RemoveAt(i);
                    }
                }

                if (skillShootBuffer.Length <= 0)
                {
                    Ecb.SetComponentEnabled<ShootBulletBuffer>(sortKey, entity, false);
                }
            }
        }
    }
}