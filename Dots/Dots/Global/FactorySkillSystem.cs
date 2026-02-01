using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct FactorySkillSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<InDeadTag> _deadLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillProperties> _skillPropertiesLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _deadLookup = state.GetComponentLookup<InDeadTag>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillPropertiesLookup = state.GetComponentLookup<SkillProperties>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _creatureLookup.Update(ref state);
            _deadLookup.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillPropertiesLookup.Update(ref state);
            _localToWorldLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            var deltaTime = SystemAPI.Time.DeltaTime;

            //加技能
            if (global.CreateSkillBuffer.Length > 0)
            {
                var buffer = global.CreateSkillBuffer[0];
                global.CreateSkillBuffer.RemoveAt(0);

                var bAlive = _deadLookup.HasComponent(buffer.Master) && !_deadLookup.IsComponentEnabled(buffer.Master);
                if (bAlive && _creatureLookup.HasComponent(buffer.Master))
                {
                    SkillHelper.CreateSkill(deltaTime, global, cache, buffer, ecb, _creatureLookup, _skillEntitiesLookup, _skillPropertiesLookup, _localToWorldLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup);
                }
            }
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}