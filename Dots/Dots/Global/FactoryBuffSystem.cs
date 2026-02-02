using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct FactoryBuffSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private ComponentLookup<InDeadState> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private ComponentLookup<StatusSummon> _summonLookup;
        [ReadOnly] private ComponentLookup<DisableBuffTag> _disableBuffTagLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _deadLookup = state.GetComponentLookup<InDeadState>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _summonLookup = state.GetComponentLookup<StatusSummon>(true);
            _disableBuffTagLookup = state.GetComponentLookup<DisableBuffTag>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _creatureTag.Update(ref state);
            _deadLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _summonLookup.Update(ref state);
            _disableBuffTagLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            var masters = new NativeList<Entity>(Allocator.Temp);
            for (var i = global.CreateBuffData.Length - 1; i >= 0; i--)
            {
                var buffer = global.CreateBuffData[i];

                //同一个帧每个entity只处理一个buff
                if (masters.Contains(buffer.Master))
                {
                    continue;
                }

                global.CreateBuffData.RemoveAt(i);
                var bAlive = _deadLookup.HasComponent(buffer.Master) && !_deadLookup.IsComponentEnabled(buffer.Master);
                if (bAlive && _summonLookup.HasComponent(buffer.Master))
                {
                    BuffHelper.AddBuff(global, cache, buffer, _summonLookup, _disableBuffTagLookup, _buffEntitiesLookup, _buffTagLookup, _transformLookup, _buffCommonLookup, ecb);
                    masters.Add(buffer.Master);
                }
            }

            masters.Dispose();

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}