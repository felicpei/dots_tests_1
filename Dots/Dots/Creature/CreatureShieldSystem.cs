using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureShieldSystem : ISystem
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
            
            //吸引状态
            new ShieldUpdateJob
            {
                Ecb = ecb.AsParallelWriter(),
                DeltaTime = deltaTime,
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct ShieldUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            
            [BurstCompile]
            private void Execute(RefRW<ShieldProperties> shield, RefRW<LocalTransform> local, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (shield.ValueRO.ContTime > 0)
                {
                    if (shield.ValueRO.Timer > shield.ValueRO.ContTime)
                    {
                        //remove shield
                        Ecb.AppendToBuffer(sortKey, entity, new CreatureDataProcess
                        {
                            Type = ECreatureDataProcess.RemoveShield,
                            BoolValue = true,
                        });
                        Ecb.SetComponentEnabled<CreatureDataProcess>(sortKey, entity, true);
                    }
                    else
                    {
                        shield.ValueRW.Timer += DeltaTime;
                    }
                }
            }
        }
    }
}