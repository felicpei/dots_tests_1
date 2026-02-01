using Unity.Entities;
using Unity.Mathematics;

namespace Dots
{
    public struct DropItemProperties : IComponentData
    {
        public int Id;
        public int Value;
    }
    
    public struct DropItemInitTag : IComponentData, IEnableableComponent
    {
        public int Id;
    }

    public struct DropItemIdleTag : IComponentData, IEnableableComponent
    {
        public int Id;
        public float Timer;
        public bool EndFly;
        public bool StartFlash;
    }

    public struct DropItemFlyTag : IComponentData, IEnableableComponent
    {
        public float Speed;
        public bool BackAniFlag;
        public float TimeSpent;
    }
    
    public struct DropItemForceTag : IComponentData , IEnableableComponent
    {
        public float3 Forward;
        public float Speed;
        public float VerticalVelocity;
    }
}