using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    [UpdateAfter(typeof(ServantPickupSystem))]
    public partial struct PlayerPickupSkillSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
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

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            _deadLookup.Update(ref state);

            //拾取技能
            new PlayerPickupSkillJob
            {
                GlobalEntity = global.Entity,
                Ecb = ecb.AsParallelWriter(),
                DeltaTime = deltaTime,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct PlayerPickupSkillJob : IJobEntity
        {
            public Entity GlobalEntity;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;

            [BurstCompile]
            private void Execute(LocalPlayerTag _, RefRW<PickupSkillTag> tag, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                tag.ValueRW.Timer = tag.ValueRO.Timer + DeltaTime;

                if (tag.ValueRO.Timer > tag.ValueRO.ContTime)
                {
                    //移除技能s
                    SkillHelper.RemoveSkill(GlobalEntity, entity, tag.ValueRO.SkillId, Ecb, sortKey);
                    Ecb.SetComponentEnabled<PickupSkillTag>(sortKey, entity, false);
                }
            }
        }
    }
}