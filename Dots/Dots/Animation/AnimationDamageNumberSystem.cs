using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    public struct DamageNumberProperties : IComponentData
    {
        public float OriginScale;
        public float MaxMoveTime;
        public float MoveSpeed;
        public float ToScale;
        
        public float CurMoveTime;
        public bool MoveOver;
        public bool IsScaleOver;
        public bool IsScaleMax;
    }

    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial struct AnimationDamageNumberSystem : ISystem
    {
        [ReadOnly] private BufferLookup<Child> _childLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _childLookup = state.GetBufferLookup<Child>(true);
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

            _childLookup.Update(ref state);

            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            new DamageNumberMoveJob
            {
                GlobalEntity = global.Entity,
                ChildLookup = _childLookup,
                DeltaTime = deltaTime,
                Ecb = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct DamageNumberMoveJob : IJobEntity
        {
            public Entity GlobalEntity;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            [ReadOnly] public BufferLookup<Child> ChildLookup;

            [BurstCompile]
            private void Execute(RefRW<DamageNumberProperties> properties, RefRW<LocalTransform> localTransform, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                //飘动结束，destroy
                if (properties.ValueRO.MoveOver)
                {
                    //destroy Child
                    if (ChildLookup.TryGetBuffer(entity, out var children))
                    {
                        for (var i = 0; i < children.Length; i++)
                        {
                            Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = children[i].Value });
                        }
                    }

                    Ecb.RemoveComponent<Child>(sortKey, entity);
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EntityDestroyBuffer { Value = entity });
                    return;
                }

                //缩放结束后，向上飘动
                if (properties.ValueRO.IsScaleOver)
                {
                    if (properties.ValueRO.CurMoveTime >= properties.ValueRO.MaxMoveTime)
                    {
                        properties.ValueRW.MoveOver = true;
                    }
                    else
                    {
                        localTransform.ValueRW.Position = localTransform.ValueRO.Position + new float3(0, 1, 0) * properties.ValueRO.MoveSpeed * DeltaTime;
                    }

                    properties.ValueRW.CurMoveTime += DeltaTime;
                    return;
                }


                //先变大，再变小
                //简单的缩放动画
                if (!properties.ValueRO.IsScaleOver)
                {
                    if (!properties.ValueRO.IsScaleMax)
                    {
                        var toScale = localTransform.ValueRO.Scale + DeltaTime * 25f * (properties.ValueRO.ToScale - properties.ValueRO.OriginScale);
                        if (toScale > properties.ValueRO.ToScale)
                        {
                            toScale = properties.ValueRO.ToScale;
                            properties.ValueRW.IsScaleMax = true;
                        } 

                        localTransform.ValueRW.Scale = toScale;
                    }
                    else
                    {
                        var targetScale = localTransform.ValueRO.Scale - DeltaTime * 7f * (properties.ValueRO.ToScale - properties.ValueRO.OriginScale);
                        if (targetScale < properties.ValueRO.OriginScale)
                        {
                            targetScale = properties.ValueRO.OriginScale;
                            properties.ValueRW.IsScaleOver = true;
                        }

                        localTransform.ValueRW.Scale = targetScale;
                    }
                }
            }
        }
    }
}