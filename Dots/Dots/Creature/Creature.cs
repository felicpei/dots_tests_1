using Deploys;
using Unity.Entities;
using Unity.Mathematics;

namespace Dots
{

    public struct CreatureTag : IComponentData
    {
        public ECreatureType Type;
        public ETeamId TeamId;
    }

    public struct CreatureProperties : IComponentData
    {
        public ECreatureType Type;
        public EElement BelongElementId;
        
        public AtkValue AtkValue;
        public float FullHp;
        public float CurHp;
        public float FullHpFactor;
        
        public float Def;
        public float RepelCd;
       
        public int SelfConfigId;
        public Entity SummonParent;
        public float OriginScale;
        
        public bool InMoveCopy;
        public float MoveSpeedCopy;
        public float SpeedFac;
        public float CenterY;
    }

    public struct StatusHpRecovery : IComponentData
    {
        public float Timer;
    }
    
    public struct StatusColor : IComponentData
    {
        public float4 Color;
        public float Alpha;
        public bool InBuffColor;
    }

    public struct BindingElement : IComponentData
    {
        public EElement Value;
        public float ContTime;
        public float Timer;
    }

    public struct CreatureFps : IComponentData
    {
        public bool FpsFactorZero;
        public float FpsFactor;
    }
    
    public struct CreatureMuzzlePos : IComponentData
    {
        public float3 Value;
    }
    
    public struct CreatureMove : IComponentData
    {
        public float MoveSpeedSource;
        public float MoveSpeedResult;
        public bool InMove;
        public float MoveDistTemp;
    }

    public struct CreatureForward : IComponentData
    {
        public float3 FaceForward;
        public float3 MoveForward;
        public quaternion Result;
    }

    public struct ShieldProperties : IComponentData, IEnableableComponent
    {
        public float Value;
        public float MaxValue;
        public float ContTime;
        public float Timer;
    }

    //待处理事物
    [InternalBufferCapacity(1)]
    public struct CreatureDataProcess : IBufferElementData, IEnableableComponent
    {
        public ECreatureDataProcess Type;
        public Entity EntityValue;
        public float AddValue;
        public float AddPercent;
        public int IntValue1;
        public int IntValue2;
        public float FloatValue1;
        public bool BoolValue;
        public float3 Float3Value;
    }

    [InternalBufferCapacity(2)]
    public struct SkillEntities : IBufferElementData, IEnableableComponent
    {
        public Entity Value;
    }

    [InternalBufferCapacity(4)]
    public struct BuffEntities : IBufferElementData, IEnableableComponent
    {
        public Entity Value;
    }

    [InternalBufferCapacity(2)]
    public struct SummonEntities : IBufferElementData
    {
        public Entity Value;
    }

    public struct MasterCreature : IComponentData
    {
        public Entity Value;
    }

    public enum ECreatureDataProcess
    {
        Cure,
        AddCurHp,
        ResetHp,
        SetActive,
        AddScale,
        AddMaxHp,
        KillEnemy,
        AddShield,
        RemoveShield,
        SummonEntitiesDie,
        ResetSummonsAroundAngle,
        Teleport,
        CastBulletAction,
        Turn,
        UnbindBullet,
        SmallMonsterToElite,
        AddAttr,
    }

    public struct CollisionDamageCdTag : IComponentData, IEnableableComponent
    {
        public float Value;
    }

    public struct EnterHitTag : IComponentData, IEnableableComponent
    {
        public Entity Attacker;
    }

    public struct EnterDieTag : IComponentData, IEnableableComponent
    {
        public bool FromHatch;
        public bool BanDrop;
        public bool BanTrigger;
        public float3 HitForward;
    }

    public struct EnterReviveTag : IComponentData, IEnableableComponent
    {
        public float Delay;
        public float Timer;
    }

    public struct DisableAutoTargetTag : IComponentData, IEnableableComponent
    {
    }

    public struct DisableBuffTag : IComponentData, IEnableableComponent
    {
    }

    public struct CreatureForceTargetTag : IComponentData, IEnableableComponent
    {
        public float3 Target;
        public float Timer;
        public float DestroyDelay;
    }

    public struct CreatureAbsorbTag : IComponentData, IEnableableComponent
    {
        public float3 Target;
        public float Speed;
        public float Timer;
        public float DestroyDelay;
    }

    //击退开始标记
    public struct CreatureRepelStart : IComponentData, IEnableableComponent
    {
        public float3 Forward;
        public float Force;
        public bool IsPhysics;
        public float RepelMaxScale;
        public float RepelTime;
        public float RepelCd;
    }

    //击退CD
    public struct CreatureRepelCd : IComponentData, IEnableableComponent
    {
        public float Timer;
    }
    
    //传统坐标方式击退
    public struct CreatureRepelPosition : IComponentData, IEnableableComponent
    {
        public float3 Forward;
        public float Distance;
        public float MaxScale;
        public float ContTime;
        
        public float Timer;
        public float3 TargetPos;
        public float ExtraScale;
    }
    
    public struct DisableBulletHitTag : IComponentData, IEnableableComponent
    {
    }

    public struct DisableHurtTag : IComponentData, IEnableableComponent
    {
        public float Timer;
        public float ContTime;

        public DisableHurtTag(float contTime, float timer)
        {
            ContTime = contTime;
            Timer = timer;
        }
    }

    public struct DisableMoveTag : IComponentData, IEnableableComponent
    {
        public float Timer;
        public float DestroyDelay;
    }

    public struct EnterFreezeTag : IComponentData, IEnableableComponent
    {
    }

    public struct RemoveFreezeTag : IComponentData, IEnableableComponent
    {
    }
    
    public struct InDashingTag : IComponentData, IEnableableComponent
    {
        public float TotalTime;
        public float CurTime;
        public float Speed;
        public float3 Forward;
        public float3 StartPos;
        public bool DisableCollision;
        public bool DontCheckFence;

        public int MaxBounceCount;
        public int CurBounceCount;
        
        //skillInfo
        public int AfterSkillId;
        public int RootSkillId;
        public int SkillRecursionCount;
        public int SkillMaxRecursion;
        
        public AtkValue SkillAtkValue;
        public Entity PrevTarget;
        public float3 PrevTargetPos;

        public bool HasEffect;
    }

    public struct InRepelTag : IComponentData, IEnableableComponent
    {
        
    }

    public struct InDeadTag : IComponentData, IEnableableComponent
    {
    }

    public struct InFreezeTag : IComponentData, IEnableableComponent
    {
    }

    public struct InAttackTag : IComponentData, IEnableableComponent
    {
    }

    public struct DashStartTag : IComponentData, IEnableableComponent
    {
        public float3 Pos;
        public float Speed;
        public int EffectId;
        
        public int AfterSkillId;
        public int RootSkillId;
        public AtkValue SkillAtkValue;
        public int SkillRecursionCount;
        public int SkillMaxRecursion;
        
        public Entity PrevTarget;
        public float3 PrevTargetPos;
        public bool DisableCollision;
        public float ForceDist;
        public int BounceCount;
    }
    
    public struct DashFollowLineTag : IComponentData, IEnableableComponent
    {
        public float Speed;
        public int EffectId;
    }
    
    [InternalBufferCapacity(2)]
    public struct DamageBuffer : IBufferElementData, IEnableableComponent
    {
        public Entity Bullet;
        public float DamageFactor;
        public bool FromExplosion;
        
        public float3 RepelForward;
        public float RepelForce;
        public float RepelTime;
        public float RepelMaxScale;
        public float RepelCd;
    }
    
    [InternalBufferCapacity(4)]
    public struct DamageNumberBuffer : IBufferElementData, IEnableableComponent
    {
        public float Value;
        public EElement Element;
        public EDamageNumber Type;
        public EAgainstType Against;
        public EElementReaction Reaction;
    }

    [InternalBufferCapacity(1)]
    public struct ShootBulletBuffer : IBufferElementData, IEnableableComponent
    {
        public int BulletId;
        public float3 Direction;
        public Entity SkillEntity;
        public float MaxContTime; //持续时间
        public float ShootInterval;
        public int IntervalShootCount; //每次Interval射出几发子弹
        public float RotateAngle; //每射击一次偏转多少度
        public float RotateSpeed;

        public float3 ForceShootPos; //强制起点，设置了则不从玩家身上发出
        public bool DisableSplit;

        public float3 MuzzlePos;
        public int MuzzleEffectId;
        public bool MuzzleLaser;
        public float MuzzleScale;
        public float MuzzleAliveTime; //0表示跟随生命周期
        public bool HasMuzzle;

        public float ContTimer;
        public float ShootTimer;
        public int AlreadyShootCount;
        public float Offset;

        public bool PlaySpellAni;
        public bool bUseWeaponSlots;
    }

    [InternalBufferCapacity(2)]
    public struct BindingBullet : IBufferElementData
    {
        public Entity Value;
        public EBulletFrom From;
        public int FromId;
    }
    
    public enum EBulletFrom
    {
        Default = 0,
        Collision = 1,
        Buff = 2,
        ElementReaction = 3,
        None = 100,
    }
}