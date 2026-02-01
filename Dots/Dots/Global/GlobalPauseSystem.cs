using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct GlobalPauseSystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillTag> _skillTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();

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
            _skillEntitiesLookup.Update(ref state);
            _skillTagLookup.Update(ref state);

            //记录一个自动用的ElapsedTime, 暂停的时候不加
            var globalAspect = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var deltaTime = SystemAPI.Time.DeltaTime;
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();

            //暂停和解除暂停时机的相关处理
            if (!globalAspect.InPause)
            {
                globalAspect.Time += deltaTime;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            //暂停操作 
            if (globalAspect.PauseTag)
            {
                globalAspect.PauseTag = false;


                //monster
                foreach (var creature in SystemAPI.Query<RefRW<CreatureFps>>())
                {
                    creature.ValueRW.FpsFactorZero = true;
                }

                //停止玩家动作
                foreach (var creatureMove in SystemAPI.Query<RefRW<CreatureMove>>())
                {
                    creatureMove.ValueRW.InMove = false;
                }

                /*//地图变黑
                foreach (var (blendColor, blendOpacity) in SystemAPI.Query<RefRW<MaterialBlendColor>, RefRW<MaterialBlendOpacity>>())
                {
                    blendColor.ValueRW.Value = new float4(0, 0, 0, 0);
                    blendOpacity.ValueRW.Value = 0.8f;
                }*/
            }

            //解除暂停
            if (globalAspect.UnPauseTag)
            {
                globalAspect.UnPauseTag = false;

                /*//地图恢复
                foreach (var (blendColor, blendOpacity) in SystemAPI.Query<RefRW<MaterialBlendColor>, RefRW<MaterialBlendOpacity>>())
                {
                    blendColor.ValueRW.Value = new float4(1, 1, 1, 1);
                    blendOpacity.ValueRW.Value = 0f;
                }*/

                //如果在时停，则不恢复怪物
                if (!globalAspect.InMonsterPause)
                {
                    foreach (var (creatureTag, creatureFps) in SystemAPI.Query<CreatureTag,  RefRW<CreatureFps>>())
                    {
                        if (creatureTag.TeamId == ETeamId.Monster)
                        {
                            creatureFps.ValueRW.FpsFactorZero = false;
                        }
                    }
                }
            }

            //时停
            if (globalAspect.PauseMonsterTag)
            {
                globalAspect.PauseMonsterTag = false;
                foreach (var (creatureTag,  creatureFps) in SystemAPI.Query<CreatureTag, RefRW<CreatureFps>>().WithAll<MonsterProperties>())
                {
                    if (creatureTag.TeamId == ETeamId.Monster)
                    {
                        creatureFps.ValueRW.FpsFactorZero = true;
                    }
                }
            }

            //解除时停
            if (globalAspect.UnPauseMonsterTag)
            {
                globalAspect.UnPauseMonsterTag = false;
                foreach (var (creatureTag,  creatureFps) in SystemAPI.Query<CreatureTag,  RefRW<CreatureFps>>().WithAll<MonsterProperties>())
                {
                    if (creatureTag.TeamId == ETeamId.Monster)
                    {
                        creatureFps.ValueRW.FpsFactorZero = false;
                    }
                }
            }

            var monsterPauseData = SystemAPI.GetSingletonRW<GlobalMonsterPauseData>();
            if (monsterPauseData.ValueRO.InMonsterPause)
            {
                if (!globalAspect.InPause)
                {
                    monsterPauseData.ValueRW.Timer += deltaTime;
                    if (monsterPauseData.ValueRO.Timer > monsterPauseData.ValueRO.ContTime)
                    {
                        globalAspect.UnPauseMonsterTag = true;
                        globalAspect.InMonsterPause = false;

                        SkillHelper.DoSkillTrigger(localPlayer, _skillEntitiesLookup, _skillTagLookup, new SkillTriggerData(ESkillTrigger.OnTheWorldEnd), ecb);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}