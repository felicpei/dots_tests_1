using Unity.Entities;
using Unity.Mathematics;

namespace Dots
{
    public struct ServantList : IBufferElementData
    {
        public Entity Value;
    }
    
    public struct ServantProperties : IComponentData
    {
        public int Id;
        public int Idx;
        public int UniqueId;
        public ERarity Rarity;
    }
    
    public struct ServantInitTag : IComponentData
    {
        public ERarity Rarity;
    }

    public struct MainServantTag : IComponentData
    {
        public float recordTimer;
        public float3 initTempPos;
    }

    public struct ServantDestroyTag : IComponentData, IEnableableComponent
    {
        public float DestroyDelay;
        public float Timer;
    }

    public struct ServantLockForward : IComponentData, IEnableableComponent
    {
        public float ContTime;
    }
        
    public struct ServantWeaponPosList : IBufferElementData
    {
        public float3 Value;
    }
}