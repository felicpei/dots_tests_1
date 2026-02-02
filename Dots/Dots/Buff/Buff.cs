using Deploys;
using Unity.Entities;

namespace Dots
{
    public enum EBuffUpdate
    {
        None,
        Add,
        Remove,
        Overlap,
    }

    [InternalBufferCapacity(4)]
    public struct BuffUpdateBuffer : IBufferElementData, IEnableableComponent
    {
        public readonly EBuffUpdate Value;

        public BuffUpdateBuffer(EBuffUpdate value)
        {
            Value = value;
        }
    }
    
    public struct BuffTimeProperty : IComponentData, IEnableableComponent
    {
        public float StartTime;
        public float ContTime;
    }

    public struct BuffTag : IComponentData
    {
        public int BuffId;
        public EBuffType BuffType;
        public EBuffFrom From;
        public int FromId;      //哪个Skill加的
        public int Group;
        public Entity Attacker;
        public Entity Master;
        public int Layer;
        public bool HasEffect;
        public EInfluenceSummon InfluenceSummon;
        public int InfluenceSummonId;
    }

    public struct BuffFreeze : IComponentData
    {
        public bool Enable;
    }
   
    public struct BuffTransfusion  : IComponentData, IEnableableComponent
    {
        public float Factor;
        public Entity Attacker;
    }
  
    public struct BuffAppendRandomIds : IComponentData, IEnableableComponent
    {
        public int CheckInt1;
        public int CheckInt2;
        public int Append1;
        public int Append2;
        public int Append3;
        public int Append4;
    }
     
    public struct BuffCommonData : IComponentData, IEnableableComponent
    {
        public float MaxLimit;
        public float AddFactor;
        public float AddValue;
        public int CheckInt1;
        public int CheckInt2;
        public int CheckInt3;
        public bool CheckBool1;
        public float CheckFloatMin;
        public float CheckFloatMax;
        public int AttachInt;
        public float TempData;
    }
}