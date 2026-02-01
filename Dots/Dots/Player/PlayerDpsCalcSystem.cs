using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    public partial struct PlayerDpsCalcSystem : ISystem
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
 
            //被击处理 
            new PlayerDpsCalcJob
            {
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct PlayerDpsCalcJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float CurTime;

            [BurstCompile]
            private void Execute(DynamicBuffer<PlayerDpsBuffer> dpsList, DynamicBuffer<DpsAppendBuffer> dmgBuffers, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                var list = new NativeArray<PlayerDpsBuffer>(dpsList.Length, Allocator.Temp);
                for (var i = 0; i < dpsList.Length; i++)
                {
                    list[i] = dpsList[i];
                }

                for (var i = 0; i < list.Length; i++)
                {
                    var dps = list[i];
                    for (var j = 0; j < dmgBuffers.Length; j++)
                    {
                        if (dmgBuffers[j].ServantId == dps.ServantId)
                        {
                            dps.DpsTotalDamage += dmgBuffers[j].Damage;
                        }
                    }
                    
                    list[i] = dps;
                }
               
                dpsList.Clear();
                dpsList.CopyFrom(list);
                list.Dispose();
                
                dmgBuffers.Clear();
                Ecb.SetComponentEnabled<DpsAppendBuffer>(sortKey, entity, false);
            }
        }
    }
}