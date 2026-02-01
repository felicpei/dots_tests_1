using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureDisableHurtSystem : ISystem
    {
        private EntityQuery _query;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<DisableHurtTag>();
            _query = state.GetEntityQuery(queryBuilder);
            queryBuilder.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_query.IsEmpty)
            {
                return;
            }
            
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            if (global.InPause)
            {
                return;
            }


            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            //禁止移动状态
            new DisableHurtTimerJob { Ecb = ecb.AsParallelWriter(), DeltaTime = deltaTime }.ScheduleParallel();
            state.Dependency.Complete();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct DisableHurtTimerJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            [BurstCompile]
            private void Execute(RefRW<DisableHurtTag> tag, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (tag.ValueRO.ContTime > 0)
                {
                    tag.ValueRW.Timer += DeltaTime;
                    if (tag.ValueRO.Timer > tag.ValueRO.ContTime)
                    {
                        Ecb.SetComponentEnabled<DisableHurtTag>(sortKey, entity, false);
                    }
                }
            }
        }
    }
}