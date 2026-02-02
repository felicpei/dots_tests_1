using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MonsterSystemGroup))]
    public partial struct MonsterBornSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<StatusForward> _creatureForwardLookup;
        [ReadOnly] private ComponentLookup<HybridEvent_SetActive> _eventSetActive;
        [ReadOnly] private ComponentLookup<LocalTransform> _transformLookup;
        [ReadOnly] private BufferLookup<BindingBullet> _bindBulletLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<CacheProperties>();
            
            _eventSetActive = state.GetComponentLookup<HybridEvent_SetActive>(true);
            _creatureForwardLookup = state.GetComponentLookup<StatusForward>(true);
            _bindBulletLookup = state.GetBufferLookup<BindingBullet>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
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
            
            _creatureForwardLookup.Update(ref state);
            _eventSetActive.Update(ref state);
            _bindBulletLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var cache = SystemAPI.GetAspect<CacheAspect>(SystemAPI.GetSingletonEntity<CacheProperties>());
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            
            foreach (var (tag, monster, creature, creatureProps, entity) in 
                     SystemAPI.Query<RefRW<InBornTag>, MonsterProperties, RefRW<CreatureTag>, CreatureProps>().WithEntityAccess())
            {
                if (!cache.GetMonsterConfig(monster.Id, out var monsterConfig))
                {
                    ecb.SetComponentEnabled<InBornTag>(entity, false);
                    continue;
                }
                
                tag.ValueRW.Timer = tag.ValueRO.Timer - deltaTime;
                if (tag.ValueRO.Timer <= 0)
                {
                    CreatureHelper.ProcessMasterActive(true, entity, _eventSetActive, ecb);
                    
                    ecb.SetComponentEnabled<DisableMoveTag>(entity, false);
                    ecb.SetComponentEnabled<DisableHurtTag>(entity, monsterConfig.BanHurt);
                    ecb.SetComponentEnabled<DisableAutoTargetTag>(entity, monsterConfig.BanAutoTarget);
                    ecb.SetComponentEnabled<InBornTag>(entity, false);
                   
                    //绑定子弹
                    if (monsterConfig.BindBulletId > 0)
                    {
                        BulletHelper.BindCollisionBullet(global, entity, creatureProps, _bindBulletLookup, _transformLookup, monsterConfig.BindBulletId);
                    }
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}