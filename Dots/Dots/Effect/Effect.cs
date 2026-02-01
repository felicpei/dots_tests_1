using Unity.Entities;

namespace Dots
{
    public struct EffectProperties : IComponentData
    {
        public int ResourceId;
        public EEffectFrom From;
        public int FromId;
        public Entity Parent;
    }
    
    public struct EffectDelayDestroy : IComponentData, IEnableableComponent
    {
        public float DelayDestroy;
        public float AliveTime;
    }
}