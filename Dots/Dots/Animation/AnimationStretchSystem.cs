using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    public struct EntityStretchAnimation : IComponentData
    {
        public Entity Master; //目标Entity(如果Entity = Entity.Null 则使用Pos)
        public float3 TargetPos;
        public float3 StartPos;
        public float NeedTime; //到终点时间
        public float Width;
        public float CurTime;
        public float AddedDist;
    }

    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial struct SYSTEM_AniEntityStretch : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
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

            _transformLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (info, transform, scaleXZ, entity) in
                     SystemAPI.Query<RefRW<EntityStretchAnimation>, RefRW<LocalTransform>, RefRW<ScaleXZData>>().WithEntityAccess())
            {
                if (info.ValueRO.CurTime > info.ValueRO.NeedTime)
                {
                    continue;
                }

                if (!_transformLookup.HasComponent(info.ValueRO.Master))
                {
                    ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                    continue;
                }
                
                var targetPos = info.ValueRO.TargetPos;
                var startPos = info.ValueRO.StartPos;
                targetPos.y = startPos.y;
                
                var direction = math.normalizesafe(targetPos - startPos);
                var dist = math.distance(startPos, targetPos);

                //当前剩余距离 / 当前剩余时间 = speed
                var speed = dist / info.ValueRO.NeedTime;
                info.ValueRW.AddedDist = info.ValueRO.AddedDist + deltaTime * speed;

                transform.ValueRW.Position = startPos + (info.ValueRO.AddedDist / 2f) * direction;
                info.ValueRW.CurTime = info.ValueRO.CurTime + deltaTime;

                transform.ValueRW.Rotation = MathHelper.forward2RotationSafe(direction);
                
                //scale xz
                scaleXZ.ValueRW.X = info.ValueRO.Width;
                scaleXZ.ValueRW.Z = 1 + info.ValueRO.AddedDist;
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
    }
}