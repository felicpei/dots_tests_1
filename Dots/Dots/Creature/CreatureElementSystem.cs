using System;
using Deploys;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureElementSystem : ISystem
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
            new CreatureElementJob
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
        private partial struct CreatureElementJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public Entity Factory;

            [BurstCompile]
            private void Execute(RefRW<BindingElement> element, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (element.ValueRO.Value != EElement.None)
                {
                    element.ValueRW.Timer += DeltaTime;
                    if (element.ValueRO.Timer >= element.ValueRO.ContTime)
                    {
                        //remove effect
                        Ecb.AppendToBuffer(sortKey, Factory, new DestroyEffectByFrom
                        {
                            Parent = entity,
                            From = EEffectFrom.Element,
                            FromId = (int)element.ValueRO.Value
                        });
                        
                        element.ValueRW.Value = EElement.None;
                        element.ValueRW.Timer = 0;
                    }
                }
            }
        }
    }
}