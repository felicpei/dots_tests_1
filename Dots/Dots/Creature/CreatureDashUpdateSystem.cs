using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Dots
{
    [BurstCompile]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureDashUpdateSystem : ISystem
    {
        // 缓存 Lookup 以提高性能
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<InFreezeTag> _inFreezeLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate(new EntityQueryBuilder(Allocator.Temp).WithAll<InDashingTag>() .Build(ref state));
            
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _inFreezeLookup = state.GetComponentLookup<InFreezeTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var globalEntity = SystemAPI.GetSingletonEntity<GlobalInitialized>();
            var globalAspect = SystemAPI.GetAspect<GlobalAspect>(globalEntity);
            if (globalAspect.InPause) return;

            // 每帧更新 Lookup 状态
            _deadLookup.Update(ref state);
            _inFreezeLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);

            // 使用系统级 ECB 避免主线程卡顿
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // 获取物理世界
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            // 调度并行 Job
            state.Dependency = new DashUpdateJob
            {
                Ecb = ecb,
                DeltaTime = SystemAPI.Time.DeltaTime,
                CollisionWorld = collisionWorld,
                GlobalEntity = globalEntity,
                // 传入 Lookup
                DeadLookup = _deadLookup,
                InFreezeLookup = _inFreezeLookup,
                SkillEntitiesLookup = _skillEntitiesLookup,
                SkillTagLookup = _skillTagLookup,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct DashUpdateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public Entity GlobalEntity;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public ComponentLookup<InFreezeTag> InFreezeLookup;
            [ReadOnly] public BufferLookup<SkillEntities> SkillEntitiesLookup;
            [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;

            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey,
                RefRW<CreatureProperties> creature,
                RefRW<LocalTransform> localTransform,
                RefRW<InDashingTag> data,
                RefRW<CreatureMove> creatureMove)
            {
                // 1. 基础状态过滤
                if (DeadLookup.IsComponentEnabled(entity))
                {
                    Ecb.SetComponentEnabled<InDashingTag>(sortKey, entity, false);
                    return;
                }

                if (InFreezeLookup.IsComponentEnabled(entity)) return;

                // 2. 位移计算
                var moveDist = DeltaTime * data.ValueRO.Speed;
                localTransform.ValueRW.Position += data.ValueRO.Forward * moveDist;

                // 统计移动 (Helper需适配)
                CreatureHelper.CountMoveDist(entity, creatureMove, moveDist, SkillEntitiesLookup, SkillTagLookup, Ecb, sortKey);

                data.ValueRW.CurTime += DeltaTime;

                // 3. 碰撞检测逻辑 (调用你的 PhysicsHelper)
                var canBounce = data.ValueRO.MaxBounceCount > 0 && data.ValueRO.CurBounceCount < data.ValueRO.MaxBounceCount;
                var bCheckFence = !data.ValueRO.DontCheckFence && creature.ValueRO.Type != ECreatureType.Servant;
                var bHitFence = false;
                var hitNormal = float3.zero;
                if (bCheckFence || canBounce)
                {
                    // 在 Job 中使用 Allocator.Temp 是安全的（Burst 支持）
                    var allHits = new NativeList<RaycastHit>(Allocator.Temp);
                    
                    // 调用你底层的 PhysicsHelper.RayCastAll
                    if (PhysicsHelper.RayCastAll(CollisionWorld, PhysicsLayers.DontMove, localTransform.ValueRO.Position, data.ValueRO.Forward, 0.2f, allHits))
                    {
                        foreach (var hit in allHits)
                        {
                            if (hit.Entity != entity)
                            {
                                bHitFence = true;
                                hitNormal = hit.SurfaceNormal;
                                break;
                            }
                        }
                    }
                    allHits.Dispose(); // 必须手动释放

                    // 处理反弹
                    if (canBounce && bHitFence)
                    {
                        data.ValueRW.Forward = MathHelper.ReflectSafe(data.ValueRO.Forward, hitNormal);
                        data.ValueRW.StartPos = localTransform.ValueRO.Position;
                        data.ValueRW.CurTime = 0;
                        data.ValueRW.CurBounceCount += 1;
                        return; // 反弹后结束本帧逻辑
                    }
                }

                // 4. 结束判定与后续逻辑
                if (data.ValueRO.CurTime >= data.ValueRO.TotalTime || bHitFence || creature.ValueRO.CurHp <= 0)
                {
                    if (data.ValueRO.AfterSkillId > 0)
                    {
                        var forceInfo = new SkillPrevTarget
                        {
                            Enable = true,
                            Target = data.ValueRO.PrevTarget,
                            Pos = data.ValueRO.PrevTargetPos
                        };
                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new CreateSkillBuffer(
                            entity, data.ValueRO.AfterSkillId, data.ValueRO.SkillAtkValue, entity, localTransform.ValueRO.Position,
                            forceInfo, data.ValueRO.RootSkillId, 0, data.ValueRO.SkillRecursionCount, data.ValueRO.SkillMaxRecursion));
                    }

                    // 移除冲刺 Tag
                    Ecb.SetComponentEnabled<InDashingTag>(sortKey, entity, false);

                    // 清理特效
                    if (data.ValueRO.HasEffect)
                    {
                        Ecb.AppendToBuffer(sortKey, GlobalEntity, new DestroyEffectByFrom 
                        { 
                            From = EEffectFrom.Dash, 
                            FromId = data.ValueRO.RootSkillId, 
                            Parent = entity 
                        });
                    }

                    // 撞墙触发技能 (Helper需适配)
                    if (bHitFence)
                    {
                        SkillHelper.DoSkillTrigger(entity, SkillEntitiesLookup, SkillTagLookup, new SkillTriggerData(ESkillTrigger.OnDashHitObstacle), Ecb, sortKey);
                    }
                }
            }
        }
    }
}