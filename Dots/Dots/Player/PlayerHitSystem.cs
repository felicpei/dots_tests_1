using Dots;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    public partial struct PlayerHitSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CacheProperties> _cacheLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<CacheProperties>();
            
            _cacheLookup  = state.GetComponentLookup<CacheProperties>(true);
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
            _cacheLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var (tag, localTransform, entity) in SystemAPI.Query<EnterHitTag, LocalTransform>().WithAll<LocalPlayerTag>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<EnterHitTag>(entity, false);
                     
                /*//播放音效
                if (config.HitSound > 0)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new PlaySoundBuffer
                    {
                        SoundId = config.HitSound
                    });
                }

                //播放特效
                if (config.HitEffect > 0)
                {
                    Ecb.AppendToBuffer(sortKey, GlobalEntity, new EffectCreateBuffer
                    {
                        ResourceId = config.HitEffect,
                        Parent = entity,
                        //Caster = entity,
                    });
                }

                //手机震动
                Ecb.AppendToBuffer(sortKey, GlobalEntity, new ControllerShakeBuffer
                {
                    ShakeType = HapticTypes.HeavyImpact,
                });*/
            }
    
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}