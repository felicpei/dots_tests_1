using Deploys;
using Dots;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

//怪物仇恨系统，决定怪物的当前目标用
namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    public partial struct MonsterHatredSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<CreatureForceTargetTag> _forceTargetLookup;
        [ReadOnly] private ComponentLookup<MonsterMove> _monsterMoveLookup;
        [ReadOnly] private ComponentLookup<DisableAutoTargetTag> _disableAutoTargetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _forceTargetLookup = state.GetComponentLookup<CreatureForceTargetTag>(true);
            _monsterMoveLookup = state.GetComponentLookup<MonsterMove>(true);
            _disableAutoTargetLookup = state.GetComponentLookup<DisableAutoTargetTag>(true);
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

            _creatureTag.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _forceTargetLookup.Update(ref state);
            _monsterMoveLookup.Update(ref state);
            _disableAutoTargetLookup.Update(ref state);

            var localPlayerEntity = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            var localPlayerPos = float3.zero;
            if (_transformLookup.TryGetComponent(localPlayerEntity, out var localPlayerTransform))
            {
                localPlayerPos = localPlayerTransform.Position;
            }

            //player list
            /*var playerEntities = new NativeList<Entity>(Allocator.TempJob);
            foreach (var (_, entity) in SystemAPI.Query<PlayerProperties>().WithEntityAccess())
            {
                playerEntities.Add(entity);
            }*/
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            new HatredSystem
            {
                //PlayerEntities = playerEntities.AsParallelReader(),
                InMonsterPause = global.InMonsterPause,
                LocalPlayerPos = localPlayerPos,
                PlayerEntity = localPlayerEntity,
                TransformLookup = _transformLookup,
                ForceTargetLookup = _forceTargetLookup,
                CreatureTagLookup = _creatureTag,
                MonsterMoveLookup = _monsterMoveLookup,
                DeadLookup = _deadLookup,
                DisableAutoTargetLookup = _disableAutoTargetLookup,
                World = collisionWorld,
            }.ScheduleParallel();
            state.Dependency.Complete();

            //playerEntities.Dispose();
        }

        [BurstCompile]
        private partial struct HatredSystem : IJobEntity
        {
            public bool InMonsterPause;
            [ReadOnly] public ComponentLookup<CreatureTag> CreatureTagLookup;
            [ReadOnly] public ComponentLookup<InDeadTag> DeadLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<CreatureForceTargetTag> ForceTargetLookup;
            [ReadOnly] public ComponentLookup<MonsterMove> MonsterMoveLookup;
            [ReadOnly] public ComponentLookup<DisableAutoTargetTag> DisableAutoTargetLookup;
            
            [ReadOnly] public float3 LocalPlayerPos;
            [ReadOnly] public Entity PlayerEntity;
            [ReadOnly] public CollisionWorld World;

            [BurstCompile]
            private void Execute(RefRW<MonsterTarget> monsterTarget, CreatureProperties creature, MonsterProperties monster, Entity entity)
            {
                if (InMonsterPause && creature.AtkValue.Team == ETeamId.Monster)
                {
                    return;
                }

                if (!TransformLookup.TryGetComponent(entity, out var localTransform))
                {
                    return;
                }

                switch (creature.AtkValue.Team)
                {
                    //找附近10M内最近的怪物
                    case ETeamId.Player:
                    {
                        if (MonsterMoveLookup.TryGetComponent(entity, out var monsterMove) && monsterMove.Mode == EMonsterMoveMode.Direct)
                        {
                            var bHasTarget = false;
                            if (PhysicsHelper.GetNearestEnemy(entity, World, localTransform.Position, 10, CreatureTagLookup, DeadLookup, DisableAutoTargetLookup, creature.AtkValue.Team, out var enemy))
                            {
                                if (TransformLookup.TryGetComponent(enemy, out var enemyTrans))
                                {
                                    monsterTarget.ValueRW.HasTarget = true;
                                    monsterTarget.ValueRW.Pos = enemyTrans.Position;
                                    monsterTarget.ValueRW.HatredEnemy = enemy;
                                    bHasTarget = true;
                                }
                            }

                            if (!bHasTarget)
                            {
                                monsterTarget.ValueRW.HasTarget = false;
                                monsterTarget.ValueRW.Pos = float3.zero;
                                monsterTarget.ValueRW.HatredEnemy = Entity.Null;
                            }
                        }

                        break;
                    }

                    //找最近的玩家
                    case ETeamId.Monster:
                    {
                        /*var target = Entity.Null;
                        var targetPos = float3.zero;
                        var minDist = float.MaxValue;

                        for (var i = 0; i < PlayerEntities.Length; i++)
                        {
                            var hit = PlayerEntities[i];
                            if (TransformLookup.TryGetComponent(hit, out var trans))
                            {
                                var distSq = math.distancesq(trans.Position, worldTransform.Position);
                                if (distSq < minDist)
                                {
                                    minDist = distSq;
                                    target = entity;
                                    targetPos = trans.Position;
                                }
                            }
                        }

                        monsterTarget.ValueRW.HasTarget = target != Entity.Null;
                        monsterTarget.ValueRW.Pos = targetPos;*/

                        //如果有嘲讽目标，则pos为嘲讽目标的坐标
                        if (ForceTargetLookup.IsComponentEnabled(entity) && ForceTargetLookup.TryGetComponent(entity, out var forceTarget))
                        {
                            monsterTarget.ValueRW.HasTarget = true;
                            monsterTarget.ValueRW.Pos = forceTarget.Target;
                            monsterTarget.ValueRW.HatredEnemy = entity;
                        }
                        else
                        {
                            //簡化，優化效率
                            monsterTarget.ValueRW.HasTarget = true;  
                            if (localTransform.Position.z > LocalPlayerPos.z + 1.5F)
                            {
                                var targetPos = monster.BornPos;
                                targetPos.z = LocalPlayerPos.z;
                                monsterTarget.ValueRW.Pos = targetPos;
                            }
                            else
                            {
                                monsterTarget.ValueRW.Pos = LocalPlayerPos;
                               //monsterTarget.ValueRW.HatredEnemy = PlayerEntity;
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}