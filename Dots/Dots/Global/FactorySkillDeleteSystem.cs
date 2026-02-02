using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct FactorySkillDeleteSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureTag> _creatureTag;
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillProperties> _skillPropertiesLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            
            _creatureTag = state.GetComponentLookup<CreatureTag>(true);
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillPropertiesLookup = state.GetComponentLookup<SkillProperties>(true);
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
            _creatureTag.Update(ref state);
            _skillEntitiesLookup.Update(ref state);
            _skillPropertiesLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());

            //删技能
            for (var i = global.RemoveSkillBuffer.Length - 1; i >= 0; i--)
            {
                var buffer = global.RemoveSkillBuffer[i];
                global.RemoveSkillBuffer.RemoveAt(i);

                if (_creatureTag.HasComponent(buffer.Master))
                {
                    SkillHelper.DeleteSkill(buffer.Master, buffer.SkillId, ecb, global, cache, _skillEntitiesLookup, _skillPropertiesLookup, _buffEntitiesLookup, _buffTagLookup);
                }
            }
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}