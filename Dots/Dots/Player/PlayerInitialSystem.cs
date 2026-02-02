using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureInitSystemGroup))]
    public partial struct PlayerInitialSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (initData, creature, transform, entity) 
                     in SystemAPI.Query<PlayerInitTag, CreatureProps, LocalTransform>().WithEntityAccess())
            {
                ecb.RemoveComponent<PlayerInitTag>(entity);

                //来自外部的技能
                for (var i = 0; i < FightData.PlayerSkills.Count; i++)
                {
                    var skillId = FightData.PlayerSkills[i];
                    SkillHelper.AddSkill(global.Entity, entity, skillId, creature.AtkValue, transform.Position, ecb);
                }

                //dps统计(主角）
                ecb.AppendToBuffer(entity, new PlayerDpsBuffer { ServantId = 0, DpsStartTime = global.Time });
                
                //tell ts init finished
                ecb.AppendToBuffer(entity, new UIUpdateBuffer
                {
                    Value = new EventData
                    {
                        Command = EEventCommand.Init,
                    }
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}