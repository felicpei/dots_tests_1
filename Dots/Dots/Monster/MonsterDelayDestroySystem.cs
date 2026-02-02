using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    [UpdateAfter(typeof(MonsterMoveSystem))]
    public partial struct MonsterDelayDestroySystem : ISystem
    {
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<EnterDieTag> _enterDieLookup;

        [BurstCompile] 
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _enterDieLookup = state.GetComponentLookup<EnterDieTag>(true);
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

            _deadLookup.Update(ref state);
            _enterDieLookup.Update(ref state);


            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //按时间销毁的
            new DelayDieJob
            {
                Ecb = ecb.AsParallelWriter(),
                DeltaTime = deltaTime,
                DeadLookup = _deadLookup,
                EnterDieLookup = _enterDieLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct DelayDieJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<InDeadState> DeadLookup;
            [ReadOnly] public ComponentLookup<EnterDieTag> EnterDieLookup;

            [BurstCompile]
            private void Execute(RefRW<MonsterDelayDestroy> delayDestroy, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (DeadLookup.IsComponentEnabled(entity) || EnterDieLookup.IsComponentEnabled(entity))
                {
                    return; 
                }

                delayDestroy.ValueRW.Timer = delayDestroy.ValueRO.Timer + DeltaTime;
                if (delayDestroy.ValueRO.Timer >= delayDestroy.ValueRO.DelayTime)
                {
                    Ecb.SetComponentEnabled<MonsterDelayDestroy>(sortKey, entity, false);
                    
                    Ecb.SetComponent(sortKey, entity, new EnterDieTag {  BanDrop = true  });
                    Ecb.SetComponentEnabled<EnterDieTag>(sortKey, entity, true);
                }
            }
        }
    }
}