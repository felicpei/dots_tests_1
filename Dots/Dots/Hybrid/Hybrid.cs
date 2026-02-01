using Dots;
using Lobby;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dots
{
    public enum EHybridType
    {
        None,
        LocalPlayer,
        Effect,
        Monster,
        Servant,
        DamageNumber,
        DamageElement,
        ProgressBar,
        DropItem,
        Bullet,
        Map,
    }
    
    public struct HybridLinkTag : IComponentData, IEnableableComponent
    {
        public EHybridType Type;
        public int ResId;
        public int Id;
        public bool IsMainServant;
        public ERarity Rarity;
        public ECreatureType CreatureType; 
        public float3 InitPos;

        public int Tile;
        public float4 Color;
        
        public float Scale;
    }
    
    public class HybridTransform : ICleanupComponentData
    {
        public Transform Value;
        public Vector3 PrefabScale;
    }

    public class HybridPlayerController : ICleanupComponentData
    {
        public ControllerLocalPlayer Value;
    }
    
    public class HybridMonsterController : ICleanupComponentData
    {
        public ControllerMonster Value;
    }
    
    public class HybridServantController : ICleanupComponentData
    {
        public ControllerServant Value;
        public ServantHud Hud;
        public ServantBubble Bubble;
    }
    
    public class HybridProgressBarController : ICleanupComponentData
    {
        public ControllerProgressBar Value;
    }
    
    public class HybridDamageNumberController : ICleanupComponentData
    {
        public ControllerDamageNumber Value;
    }
    
    public class HybridDamageElementController : ICleanupComponentData
    {
        public ControllerDamageElement Value;
    }
    
    public class HybridDropItemController : ICleanupComponentData
    {
        public ControllerDropItem Value;
    }
    
    public struct ScaleXZData : IComponentData
    {
        public float X;
        public float Z;
    }

    public struct HybridEvent_SetActive : IComponentData, IEnableableComponent
    {
        public bool Value;
    }
    
    public struct HybridEvent_SetWeaponActive : IComponentData, IEnableableComponent
    {
        public bool Value;
    }

    public struct HybridEvent_ChangeServant : IComponentData
    {
        public int ServantId;
        public ERarity Rarity;
    }

    public enum EAnimationType
    {
        None,
        Idle,
        Atk,
        Dead,
        SpellStart,
        SpellEnd,
    }
    
    public struct HybridEvent_PlayAnimation : IComponentData, IEnableableComponent
    {
        public int Value;
        public EAnimationType Type;
    }
    
    
    public struct HybridEvent_PlayMuzzleEffect : IComponentData, IEnableableComponent
    {
        public int EffectId;
        public float Delay;
    }
    
    public struct HybridEvent_StopMuzzleEffect : IComponentData, IEnableableComponent
    {
        public float Delay;
    }
}