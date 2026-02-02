using Deploys;
using Unity.Entities;
using Unity.Mathematics;

namespace Dots
{
    public struct SkillStartInfo 
    {
        public bool IsBindSkill;
        public float CastDelayTime;    //来自技能触发时的延迟时间
        public Entity StartEntity;
        public float3 StartPos;
    } 
    
    public struct SkillPrevTarget  
    {
        public bool Enable;
        public Entity Target;
        public float3 Pos;
    }
    
    public struct MasterCreature : IComponentData
    {
        public Entity Value;
    }

    public struct SkillTag : IComponentData
    {
        public int Id;
    }
    public struct SkillProperties : IComponentData
    {
        public int Id;
        public float CreateTime;
        public float DelayTimer;
        public bool IsOver;
        public int CurrLayer;
        public int AddedLayer;
        public int TriggeredCount;
        public int RecursionCount;
        public int MaxRecursion;
        public int RootSkillId; 

        public float CdTimer;
        public bool InSubCdStatus;
        public float SubCdTimer;
        public bool PlayedAtk;

        public float CountFlag;
        public AtkValue AtkValue;
        public SkillStartInfo StartInfo;
        public SkillPrevTarget PrevTarget;

        public bool AniFlag;
        
        //临时变量
        public int SavedBulletId;
    }
    
    [InternalBufferCapacity(4)]
    public struct SkillLayerAddBuffer : IBufferElementData, IEnableableComponent
    {
        public int Value;
    }
    

    [InternalBufferCapacity(4)]
    public struct SkillTargetBuffer : IBufferElementData, IEnableableComponent
    {
        public readonly float3 StartPos;
        public readonly float3 Pos;
        public readonly Entity Entity;
        public readonly float CreateTime;
        public float3 Forward;
        public float Param1;

        public SkillTargetBuffer(float3 startPos, float3 pos, Entity entity, float createTime)
        {
            StartPos = startPos;
            Pos = pos;
            Entity = entity;
            CreateTime = createTime;
            Forward = float3.zero;
            Param1 = 0;
        }
    }

    [InternalBufferCapacity(4)]
    public struct SkillActionBuffer : IBufferElementData, IEnableableComponent
    {
        public int Idx;
        public float3 StartPos;
        public float3 Pos;
        public Entity Entity;
        public AtkValue AtkValue;
        public float CastIndex;
        public float Param1;
    }

    //选择目标的Tag
    [InternalBufferCapacity(4)]
    public struct SkillTargetTags : IBufferElementData, IEnableableComponent
    {
        public ESkillTrigger TriggerType;
        public Entity EntityValue;
        public float3 PosValue;
        public float3 Forward;
        public float CastDelay;
        public float CreateTime;
        public int ForceCastCount;
        public int Counter;
    }

    [InternalBufferCapacity(4)]
    public struct SkillEndBuffer : IBufferElementData, IEnableableComponent
    {
        public readonly float3 StartPos;
        public readonly float3 Pos;
        public readonly Entity Entity;
        public readonly AtkValue AtkValue;
        public readonly int CastIndex;
        public float Param1;

        public SkillEndBuffer(float3 startPos, float3 pos, Entity entity, AtkValue atkValue, int castIndex)
        {
            StartPos = startPos;
            Pos = pos;
            Entity = entity;
            AtkValue = atkValue;
            CastIndex = castIndex;
            Param1 = 0;
        }
    }
    
    [InternalBufferCapacity(4)]
    public struct SkillTriggerData : IBufferElementData
    {
        public readonly ESkillTrigger Type;
        public EBulletFrom From;
        public int FromParam;
        public Entity Entity;
        public float3 Pos;
        public float3 Forward;
        public int IntValue1;
        public int IntValue2;
        public float FloatValue1;
        public float FloatValue2;
        public bool BoolValue1;
        public float HpBefore;
        public float HpAfter;

        public SkillTriggerData(ESkillTrigger type)
        {
            Type = type;
            From = EBulletFrom.Default;
            FromParam = 0;
            Entity = Entity.Null;
            Pos = float3.zero;
            Forward = float3.zero;
            IntValue1 = 0;
            IntValue2 = 0;
            FloatValue1 = 0;
            FloatValue2 = 0;
            HpBefore = 0;
            HpAfter = 0;
            BoolValue1 = false;
        }
    }

}