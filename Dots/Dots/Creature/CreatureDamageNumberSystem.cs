using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureDamageNumberSystem : ISystem
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

            //创建伤害数字
            new DamageNumberCreateJob
            {
                DeltaTime = deltaTime,
                Factory = global.Entity,
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            state.Dependency.Complete();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct DamageNumberCreateJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity Factory;

            [BurstCompile]
            private void Execute(DynamicBuffer<DamageNumberBuffer> damageNumberBuffers,  RefRW<RandomSeed> random,
                CreatureProperties creature, LocalTransform localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (damageNumberBuffers.Length <= 0)
                {
                    return;
                }
                
                var buffer = damageNumberBuffers[0];
                damageNumberBuffers.RemoveAt(0);

                var rand = random.ValueRW.Value.NextFloat2(-0.5f, 0.5f);
                var pos = CreatureHelper.getHeadPos(localTransform.Position, creature, localTransform.Scale) + new float3(rand.x, rand.y, 0);

                var createBuffer = new DamageNumberCreateBuffer
                {
                    Id = (int)buffer.Element,
                    Value = (int)buffer.Value,
                    Type = buffer.Type,
                    Position = pos,
                    Reaction = buffer.Reaction,
                };
                Ecb.AppendToBuffer(sortKey, Factory, createBuffer);

                //cd.ValueRW.Timer = 0.03f;
                if (damageNumberBuffers.Length <= 0)
                {
                    Ecb.SetComponentEnabled<DamageNumberBuffer>(sortKey, entity, false);
                }
            }
        }
    }
}