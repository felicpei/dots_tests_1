using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    [UpdateAfter(typeof(MonsterMoveSystem))]
    public partial struct ServantDestroySystem : ISystem
    {
        [ReadOnly] private BufferLookup<SkillEntities> _skillEntitiesLookup;
        [ReadOnly] private ComponentLookup<SkillProperties> _skillLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            
            _skillEntitiesLookup = state.GetBufferLookup<SkillEntities>(true);
            _skillLookup = state.GetComponentLookup<SkillProperties>(true);
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
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());

            _skillEntitiesLookup.Update(ref state);
            _skillLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (tag, entity) in SystemAPI.Query<RefRW<ServantDestroyTag>>().WithEntityAccess())
            {
                tag.ValueRW.Timer = tag.ValueRO.Timer + deltaTime;  

                if (tag.ValueRO.Timer >= tag.ValueRO.DestroyDelay)
                {
                    //remove all skill
                    SkillHelper.RemoveAllSkill(global.Entity, entity, _skillEntitiesLookup, _skillLookup, ecb);

                    //remove all buffs
                    BuffHelper.RemoveAllBuff(entity, _buffEntitiesLookup, _buffTagLookup, ecb);
                    
                    ecb.SetComponentEnabled<ServantDestroyTag>(entity, false);
                    ecb.AppendToBuffer(global.Entity, new EntityDestroyBuffer { Value = entity });
                }
            }
            
            state.Dependency.Complete(); 

            ecb.Playback(state.EntityManager);  
            ecb.Dispose(); 
        } 
    }
}