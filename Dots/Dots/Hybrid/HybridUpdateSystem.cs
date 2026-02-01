using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    [UpdateInGroup(typeof(HybridSystemGroup))]
    [UpdateAfter(typeof(HybridUpdateTransformSystem))]
    public partial struct HybridUpdateSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<HybridEvent_SetActive> _eventSetActive;
        [ReadOnly] private ComponentLookup<HybridEvent_ChangeServant> _eventChangeServant;
        [ReadOnly] private ComponentLookup<HybridEvent_SetWeaponActive> _eventSetWeaponActive;
        [ReadOnly] private ComponentLookup<HybridEvent_PlayAnimation> _eventPlayAnimation;
        [ReadOnly] private ComponentLookup<HybridEvent_PlayMuzzleEffect> _eventPlayMuzzle;
        [ReadOnly] private ComponentLookup<HybridEvent_StopMuzzleEffect> _eventStopMuzzle;
        
        [ReadOnly] private ComponentLookup<PlayerAttrData> _attrLookup;
        [ReadOnly] private BufferLookup<PlayerAttrModify> _attrModifyLookup;
        [ReadOnly] private ComponentLookup<LocalToWorld> _transformLookup;
        [ReadOnly] private ComponentLookup<CreatureProperties> _creatureLookup;
        [ReadOnly] private ComponentLookup<ShieldProperties> _shieldLookup;
        [ReadOnly] private BufferLookup<BuffEntities> _buffEntitiesLookup;
        [ReadOnly] private ComponentLookup<BuffTag> _buffTagLookup;
        [ReadOnly] private ComponentLookup<BuffCommonData> _buffCommonLookup;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();

            _eventSetActive = state.GetComponentLookup<HybridEvent_SetActive>(true);
            _eventSetWeaponActive = state.GetComponentLookup<HybridEvent_SetWeaponActive>(true);
            _eventPlayAnimation = state.GetComponentLookup<HybridEvent_PlayAnimation>(true);
            _eventPlayMuzzle = state.GetComponentLookup<HybridEvent_PlayMuzzleEffect>(true);
            _eventStopMuzzle = state.GetComponentLookup<HybridEvent_StopMuzzleEffect>(true);
            _shieldLookup = state.GetComponentLookup<ShieldProperties>(true);
            _attrLookup = state.GetComponentLookup<PlayerAttrData>(true);
            _attrModifyLookup = state.GetBufferLookup<PlayerAttrModify>(true);
            _transformLookup = state.GetComponentLookup<LocalToWorld>(true);
            _creatureLookup = state.GetComponentLookup<CreatureProperties>(true);
            _buffEntitiesLookup = state.GetBufferLookup<BuffEntities>(true);
            _buffTagLookup = state.GetComponentLookup<BuffTag>(true);
            _buffCommonLookup = state.GetComponentLookup<BuffCommonData>(true);
            _eventChangeServant = state.GetComponentLookup<HybridEvent_ChangeServant>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _eventSetActive.Update(ref state);
            _eventSetWeaponActive.Update(ref state);
            _eventPlayAnimation.Update(ref state);
            _eventPlayMuzzle.Update(ref state);
            _eventStopMuzzle.Update(ref state);
            _transformLookup.Update(ref state);
            _creatureLookup.Update(ref state);
            _shieldLookup.Update(ref state);
            _attrLookup.Update(ref state);
            _attrModifyLookup.Update(ref state);
            _buffEntitiesLookup.Update(ref state);
            _buffTagLookup.Update(ref state);
            _buffCommonLookup.Update(ref state);
            _eventChangeServant.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());

            foreach (var (controller, blend, localToWorld, entity)
                     in SystemAPI.Query<HybridMonsterController, MaterialBlend, LocalToWorld>().WithEntityAccess())
            {
                UpdateBase(global, localToWorld, controller.Value, blend, entity, ecb);
            }

            //player
            foreach (var (controller, blend, localToWorld, entity) in
                     SystemAPI.Query<HybridPlayerController, MaterialBlend, LocalToWorld>().WithEntityAccess())
            {
                UpdateBase(global, localToWorld, controller.Value, blend, entity, ecb);
            }


            //servant
            foreach (var (controller, forward, weaponPosLists, creature, blend, localToWorld, entity)
                     in SystemAPI.Query<HybridServantController, CreatureForward, DynamicBuffer<ServantWeaponPosList> ,CreatureProperties, MaterialBlend, LocalToWorld>().WithEntityAccess())
            {
                UpdateBase(global, localToWorld, controller.Value, blend, entity, ecb);

                if (controller.Value.Weapons != null && controller.Value.Weapons.Count > 0)
                {
                    weaponPosLists.Clear();
                    foreach (var weaponPos in controller.Value.Weapons)
                    {
                        weaponPosLists.Add(new ServantWeaponPosList { Value = weaponPos.transform.position - controller.Value.transform.position });
                    }    
                }
                
                //hud
                controller.Hud.UpdateAim(forward.FaceForward);

                //hp
                controller.Hud.SetHp(creature.CurHp, AttrHelper.GetMaxHp(entity, _attrLookup, _attrModifyLookup, _creatureLookup, _buffEntitiesLookup, _buffTagLookup, _buffCommonLookup));

                var shieldValue = 0f;
                var shieldMaxValue = 0f;
                if (_shieldLookup.TryGetComponent(entity, out var shield) && _shieldLookup.IsComponentEnabled(entity))
                {
                    shieldValue = shield.Value;
                    shieldMaxValue = shield.MaxValue;
                }
                
                controller.Hud.SetShield(shieldValue, shieldMaxValue);
                controller.Value.UpdateMove(creature.InMoveCopy);
                
                if (_eventChangeServant.TryGetComponent(entity, out var eventChange))
                {
                    ecb.RemoveComponent<HybridEvent_ChangeServant>(entity);
                    controller.Value.InitShader(eventChange.Rarity);
                }

                controller.Value.UpdateMoveForward(forward.MoveForward, forward.FaceForward);
            }

            //progress bar
            foreach (var (controller, fillRate) in SystemAPI.Query<HybridProgressBarController, MaterialFillRate>())
            {
                controller.Value.UpdateProgress(fillRate.Value);
            }
            
            //dropitem
            foreach (var (controller, tag) in SystemAPI.Query<HybridDropItemController, DropItemIdleTag>())
            {
                controller.Value.UpdateFlash(tag.StartFlash);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void UpdateBase(GlobalAspect global, LocalToWorld localToWorld, ControllerBase controller, MaterialBlend blend, Entity entity, EntityCommandBuffer ecb)
        {
            //color
            controller.UpdateBlend(blend.Value, new Color(blend.Color.x, blend.Color.y, blend.Color.z, blend.Color.w));

            //更新枪火位置
            if (controller.hasMuzzle)
            {
                var muzzlePos = controller.getMuzzlePos();
                ecb.SetComponent(entity, new CreatureMuzzlePos
                {
                    Value = muzzlePos
                });
            }

            //active
            {
                if (_eventSetActive.TryGetComponent(entity, out var eventInfo) && _eventSetActive.IsComponentEnabled(entity))
                {
                    ecb.SetComponentEnabled<HybridEvent_SetActive>(entity, false);
                    controller.SetActive(eventInfo.Value);
                }
            }

            //weapon active
            {
                if (_eventSetWeaponActive.TryGetComponent(entity, out var eventInfo) && _eventSetWeaponActive.IsComponentEnabled(entity))
                {
                    ecb.SetComponentEnabled<HybridEvent_SetWeaponActive>(entity, false);
                    controller.SetWeaponActive(eventInfo.Value);
                }
            }

            //play animation
            {
                if (_eventPlayAnimation.TryGetComponent(entity, out var eventInfo) && _eventPlayAnimation.IsComponentEnabled(entity))
                {
                    ecb.SetComponentEnabled<HybridEvent_PlayAnimation>(entity, false);

                    switch (eventInfo.Type)
                    {
                        case EAnimationType.Idle:
                            //controller.PlayIdle();
                            break;
                        case EAnimationType.Atk:
                            controller.PlayAtk();
                            break;
                        case EAnimationType.Dead:
                        {
                            controller.PlayDead();
                            break;
                        }
                        case EAnimationType.SpellStart:
                        {
                            controller.SetAttacking(true);
                            break;
                        }
                        case EAnimationType.SpellEnd:
                        {
                            controller.SetAttacking(false);
                            break;
                        }
                        default:
                        {
                            controller.PlayAnimation(eventInfo.Value.ToString());
                            break;
                        }
                    }
                }
            }

            
            //play muzzle effect
            {
                if (_eventPlayMuzzle.TryGetComponent(entity, out var eventInfo) && _eventPlayMuzzle.IsComponentEnabled(entity))
                {
                    ecb.SetComponentEnabled<HybridEvent_PlayMuzzleEffect>(entity, false);
                    controller.PlayMuzzleEffect(eventInfo.EffectId, eventInfo.Delay);
                }
                
                if (_eventStopMuzzle.HasComponent(entity) && _eventStopMuzzle.IsComponentEnabled(entity))
                {
                    ecb.SetComponentEnabled<HybridEvent_StopMuzzleEffect>(entity, false);
                    controller.StopMuzzleEffect(eventInfo.Delay);
                }
            }
        }
    }
}