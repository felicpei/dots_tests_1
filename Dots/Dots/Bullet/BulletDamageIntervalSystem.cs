using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BulletSystemGroup))]
    public partial struct BulletDamageIntervalSystem : ISystem
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
            
            new DamageIntervalJob
            {
               DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
            state.Dependency.Complete();
        }


        [BurstCompile]
        private partial struct DamageIntervalJob : IJobEntity
        {
            public float DeltaTime;
            
            [BurstCompile]
            private void Execute(DynamicBuffer<BulletHitCreature> hitCreatures, [EntityIndexInQuery] int sortKey)
            {
                var list = new NativeArray<BulletHitCreature>(hitCreatures.Length, Allocator.Temp);
                for (var i = 0; i < hitCreatures.Length; i++)
                {
                    list[i] = hitCreatures[i];
                }

                for (var i = list.Length - 1; i >= 0; i--)
                {
                    var info = list[i];
                    info.Timer -= DeltaTime;
                    list[i] = info;
                }
                
                hitCreatures.Clear();
                hitCreatures.CopyFrom(list);
                list.Dispose();
                
                for (var i = hitCreatures.Length - 1; i >= 0; i--)
                {
                    if (hitCreatures[i].Timer <= 0)
                    {
                        hitCreatures.RemoveAt(i);
                    }
                }
            }
        }
    }
}