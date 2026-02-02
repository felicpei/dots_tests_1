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
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct FactoryMonsterSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<StatusCenter> _centerLookup;
        [ReadOnly] private ComponentLookup<CreatureProps> _propsLookup;
        [ReadOnly] private ComponentLookup<StatusHp> _hpLookup;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            
            _centerLookup = state.GetComponentLookup<StatusCenter>(true);
            _propsLookup = state.GetComponentLookup<CreatureProps>(true);
            _hpLookup = state.GetComponentLookup<StatusHp>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _centerLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _propsLookup.Update(ref state);
            _hpLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            //让所有怪物死亡
            if (SystemAPI.HasComponent<ClearMonsterTag>(global.Entity))
            {
                var tag = SystemAPI.GetComponentRW<ClearMonsterTag>(global.Entity);
                if (tag.ValueRO.Timer < tag.ValueRO.Delay)
                {
                    tag.ValueRW.Timer += SystemAPI.Time.DeltaTime;
                }
                else
                {
                    ecb.RemoveComponent<ClearMonsterTag>(global.Entity);

                    foreach (var (creature, entity) in SystemAPI.Query<CreatureTag>().WithAll<MonsterProperties>().WithEntityAccess())
                    {
                        //排除boss
                        if (creature.Type == ECreatureType.Boss && !tag.ValueRO.ContainBoss)
                        {
                            continue;
                        }

                        //排除玩家的召唤物
                        if (creature.TeamId != ETeamId.Monster)
                        {
                            continue;
                        }

                        ecb.SetComponent(entity, new EnterDieTag { BanDrop = tag.ValueRO.BanDrop, BanTrigger = true });
                        ecb.SetComponentEnabled<EnterDieTag>(entity, true);
                    }
                }
            }

            //直接刷新怪物
            for (var i = global.MonsterCreateBuffer.Length - 1; i >= 0; i--)
            {
                var buffer = global.MonsterCreateBuffer[i];
                global.MonsterCreateBuffer.RemoveAt(i);

                var props = global.MonsterProps;
                var team = buffer.TeamId;
                var type =  ECreatureType.Small;
                var hpPercent = buffer.HpPercent <= 0f ? 1f : buffer.HpPercent;

                if (buffer.IsBoss)
                {
                    type = ECreatureType.Boss;
                }
                else if (buffer.IsElite)
                {
                    type = ECreatureType.Elite;
                }
                FactoryHelper.CreateMonster(collisionWorld, global, cache, buffer.MonsterId, default, Entity.Null, props, team, buffer.BornPos, buffer.BornPos, ecb, type: type, hpPercent: hpPercent);
            }

            //召唤怪物
            for (var i = global.SummonCreateBuffer.Length - 1; i >= 0; i--)
            {
                var buffer = global.SummonCreateBuffer[i];
                global.SummonCreateBuffer.RemoveAt(i);

                //召唤时，必须有Parent
                if (_propsLookup.TryGetComponent(buffer.Parent, out var creature) &&
                    _hpLookup.TryGetComponent(buffer.Parent, out var hpInfo))
                {
                    var team = creature.AtkValue.Team;
                    var props = new FightProps
                    {
                        Atk = creature.AtkValue.Atk,
                        Crit = creature.AtkValue.Crit,
                        CritDamage = creature.AtkValue.CritDamage,
                        Def = creature.Def,
                        Hp = hpInfo.FullHp,
                        RepelCd = creature.RepelCd,
                    };

                    var monster = FactoryHelper.CreateMonster(collisionWorld, global, cache, buffer.MonsterId, 
                        default, buffer.Parent, props, team, buffer.BornPos, buffer.BornPos, ecb, bornAngle: buffer.BornAngle);

                    ecb.AppendToBuffer(buffer.Parent, new SummonEntities { Value = monster });
                    if (buffer.BornAngle <= 0)
                    {
                        ecb.AppendToBuffer(buffer.Parent, new CreatureDataProcess { Type = ECreatureDataProcess.ResetSummonsAroundAngle, EntityValue = monster });
                        ecb.SetComponentEnabled<CreatureDataProcess>(buffer.Parent, true);
                    }

                    SkillHelper.DoSkillTrigger(buffer.Parent, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnSummonMonster)
                    {
                        IntValue1 = buffer.MonsterId,
                        Entity = monster
                    }, ecb);
                }
            }

            //血条
            for (var i = global.ProgressBarCreateBuffer.Length - 1; i >= 0; i--)
            {
                FactoryHelper.CreateProgressBar(cache, global, global.ProgressBarCreateBuffer[i], ecb, _transformLookup, _deadLookup, _centerLookup);
                global.ProgressBarCreateBuffer.RemoveAt(i);
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}