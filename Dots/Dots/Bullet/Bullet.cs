using Unity.Entities;
using Unity.Mathematics;
using Deploys;

namespace Dots
{
    public enum EHeadingMethod
    {
        None,
        NearEnemy,
        RandomEnemy,
        SummonMonster,
        Master,
        AngleEnemy,
    }
    
    public struct BulletAtkValue : IComponentData
    {
        public float Atk;
        public float Crit;
        public float CritDamage;
    }
    
    public struct BulletProperties : IComponentData
    {
        public EBulletFrom From;
        public int FromId;
        public int BulletId;
        public ETeamId Team;
        public EElement ElementId;
        public float CreateTime;
        public float BombRadius;
        public Entity MasterCreature;
        public float HitForce;
        public Entity ForceHitCreature;
        public float3 ShootPos;                 //起点坐标
        public Entity SkillEntity;
        public bool AllowSkillEndAction;
        public int SkillEndActionIndex;
        public Entity TransformParent;
        public float ResourceScale;
        
        //running data
        public int MaxBounceCount;
        public int MaxDamageCount;
        public int CurrBounceCount;
        public int CurrDamageCount;
        public float ContTime;
        public float RunningTime;
        public float3 RunningDirection;
        
        //追踪的目标
        public Entity SkillTraceTarget;
        public Entity HeadingToTarget;
        public float3 HeadingOffset;
        public EHeadingMethod HeadingMethod;
        public int HeadBehaviourId;
        public bool HeadingAiming;
        
        //伤害判定形状
        public EDamageShape DamageShape;
        public float DamageShapeParam;

        //强制距离
        public float CurDist;
        public float ForceDistance;
        
        //第一速度
        public float3 D1;
        public float V1;
        public float A1;
        
        //第二速度
        public float3 D2;
        public float V2; 
        public float A2;
        
        //转向速度
        public float VTurn;
        public float ATurn;
        
        //环绕信息
        public Entity AroundEntity;
        public float3 AroundPos;
        public float AroundRadiusX;
        public float AroundRadiusY;
        public bool ReverseAroundDirection;
        public float AroundAngle;
        public float AroundTimer;
        public float3 AroundCenterOffset;       //环绕子弹的中心点偏移坐标
        public float AroundRadiusAddSpeed;

        //运行标记
        public bool Reflected;  //是否反弹过

        //原始缩放
        public float SourceScale;
        
        //others
        public float Offset;
        public float3 SelfRotate;
    }

    public struct BulletCollisionTag : IComponentData
    {
        public int Id;
        public ECollisionBulletType CollisionWithBullet;
        public ETeamId Team;
    }

    public struct BulletSplit : IComponentData
    {
        public int SplitCount;
        public float SplitAngle;
        public int SplitBulletId;
        public bool NextCanSplit;
        public int HorizCount;
        public float HorizDist;
    }
  
    public struct BulletBombTag : IComponentData, IEnableableComponent
    {
        public int Frame;
    }

    public struct BulletDestroyTag : IComponentData, IEnableableComponent
    {
        public int FrameCounter;
    }

    public struct BulletAddScale : IComponentData, IEnableableComponent
    {
        public float Curr;
        public float Speed;
        public float Max;
    }

    public struct BulletRepel : IComponentData, IEnableableComponent
    {
        public float Distance;
        public float MaxScale;
        public float Time;
        public float Cd;
    }
    
    [InternalBufferCapacity(8)] 
    public struct BulletHitCreature : IBufferElementData
    {
        public Entity Value;
        public float Timer;
    }

    public struct BulletTriggerData : IComponentData
    {
        public float SourceAtk;
        public float AtkFactor;
        public float ScaleFactor;
    }
    
    [InternalBufferCapacity(2)] 
    public struct BulletTriggerBuffer : IBufferElementData, IEnableableComponent
    {
        public EBulletBehaviourTrigger Type;
        public Entity Enemy;
        public float FlyDist;
    }
    
    [InternalBufferCapacity(2)] 
    public struct BulletBehaviourBuffer :  IBufferElementData, IEnableableComponent
    {
        public int Id;
        public bool TriggerFinished;
        public float CountFlag;
        public int TriggerCount;

        public bool InDelay;
        public bool InCd;
        public float WaitSec;
        public float Timer;
    }
    
    [InternalBufferCapacity(2)] 
    public struct BulletActionBuffer : IBufferElementData, IEnableableComponent
    {
        public int BehaviourId;
        public float3 HitNormal;
    }
}