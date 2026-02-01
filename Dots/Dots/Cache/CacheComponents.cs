using Deploys;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dots
{
    public struct IntRandomArr
    {
        public int Id0;
        public int Id1;
        public int Id2;
        public int Id3;
        public int Id4;
        public int Id5;
        public int Id6;
        public int Id7;
        public int Id8;
        public int Id9;
    }
    
    public struct CacheInitialed : IComponentData
    {
        
    }
    public struct CacheInitTag : IComponentData
    {
    }
    
    public struct CacheProperties : IComponentData
    {
        public BlobAssetReference<CacheLocalPlayerConfig> LocalPlayerConfig;
        public BlobAssetReference<CacheMonsterConfig> MonsterConfig;
        public BlobAssetReference<CacheServantConfig> ServantConfig;
        public BlobAssetReference<CacheBulletConfig> BulletConfig;
        public BlobAssetReference<CacheBulletBehaviourConfig> BulletBehaviourConfig;
        public BlobAssetReference<CacheDropItemConfig> DropItemConfig;
        public BlobAssetReference<CacheBuffConfig> BuffConfig;
        public BlobAssetReference<CacheSkillConfig> SkillConfig;
        public BlobAssetReference<CacheDamageNumberConfig> DamageNumberConfig;
        public BlobAssetReference<CacheResourceCfg> ResourceConfig;
        public BlobAssetReference<CacheElementConfig> CacheElementConfig;
    }
    
    public struct CacheLocalPlayerConfig
    {
        public BlobArray<LocalPlayerConfig> Value;
    }
    
    public struct CacheDamageNumberConfig
    {
        public BlobArray<DamageNumberConfig> Value;
    }

    public struct CacheElementConfig
    {
        public BlobArray<ElementConfig> Value;
    }
    
    public struct CacheResourceCfg
    {
        public BlobArray<ResourceCfg> Value;
    }
    
    public struct CacheMonsterConfig
    {
        public BlobArray<MonsterConfig> Value;
    }
    
    public struct CacheServantConfig
    {
        public BlobArray<ServantConfig> Value;
    }

    public struct CacheBulletConfig
    {
        public BlobArray<BulletConfig> Value;
    }

    public struct CacheBulletBehaviourConfig
    {
        public BlobArray<BulletBehaviourConfig> Value;
    }

    public struct CacheDropItemConfig
    {
        public BlobArray<DropItemConfig> Value;
    }

    public struct CacheBuffConfig
    {
        public BlobArray<BuffConfig> Value;
    }

    public struct CacheSkillConfig
    {
        public BlobArray<SkillConfig> Value;
    }
    
    public struct ResourceCfg
    {
        public int Id;
        public float Scale;
        public float CenterY;
    }
    
    public struct DamageNumberConfig
    {
        public int Id;
        public float4 Color;
        public IntRandomArr Prefix;
    }

    public struct ElementConfig
    {
        public EElement Id;
        public bool bCont;
        public int buffRes;
        public float contTime;
        public float None;
        public float Water;
        public float Fire;
        public float Ice;
        public float Lighting;
        public float Stone;
    }

    public struct LocalPlayerConfig
    {
        public int Id;
        public int ResId;
        public float ServantMoveSpeed;
    }
    
    public struct MonsterMoveConfig
    {
        public EMonsterMoveMode MoveMode;
        public float MoveParam1;
        public float MoveParam2;
        public float MoveParam3;
        public float MoveParam4;
        public float MoveParam5;
        public float MoveParam6;
    }

    public struct MonsterConfig
    {
        public int Id;
        public float Scale;
        public int EliteId;
        public int BindBulletId;
        public MonsterMoveConfig Move;
        public ECollisionType CollisionType;
        public bool BanTurn;
        public bool BanBuff;
        public bool BanAutoTarget;
        public bool BanBulletHit;
        public bool BanHurt;
        public float HpFactor;
        public float AtkFactor;
        public float DelayDestroySec;
        public EElement BelongElementId;
        public float BornTime;
        public int BornResId;
        public int ResId;
        public int DieResId;
        public int DieSound;
        public int Skill1;
        public int Skill2;
        public int Skill3;
        public int Skill4;
        public int Skill5;
    }

    public struct ServantConfig
    {
        public int Id;
        public EElement BelongElementId;
        public int ResId;
        public int DieResId;
        public float damage2;
        public float damage3;
        public float damage4;
        public float damage5;
    }

    public struct BulletConfig
    {
        public int Id;
        public EElement ElementId;
        public int ResourceId;
        public int ClassId;
        public float Scale;
        public float ContTime;
        public float Velocity;
        public float Acceleration;
        public float BombRadius;
        public float HitForce;
        public bool BanHit;
        public bool BanTurn;
        public int AttachBuffId;
        public int HorizCount;
        public float HorizDist;
        public int SplitCount;
        public float SplitAngle;
        public int SplitBulletId;
        public int DamageCount;
        public int BounceCount;
        public int BombEffectId;
        public int HitEffectId;
        public EDamageCalc DamageCalc;
        public float DamageFactor;
        public int HitSound;
        public int BombSound;
        public float BombShakeRadius;
        public float BombShakeTime;
        public EHitRule HitSameTeam;
        public ECollisionBulletType CollisionWithBullet;
        public float DamageInterval;
        public int Behaviour;
        public EDamageShape DamageShape;
        public float DamageShapeParam;
    }

    public struct BulletBehaviourConfig
    {
        public int Id;
        public int BindingGroup;
        public EBulletBehaviourTrigger TriggerAction;
        public float TriggerParam1;
        public float TriggerParam2;
        public float TriggerParam3;
        public float TriggerParam4;
        public float TriggerParam5;
        public float TriggerParam6;
        public EBulletBehaviourAction Action;
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public float Param5;
        public float Param6;
    }

    public struct DropItemConfig
    {
        public int Id;
        public int ResourceId;
        public float Speed;
        public float Acceleration;
        public int PickupSound;
        public float DestroyDelay;
        public bool AutoFly;
        public bool EndFly;
        public bool CanMagnet;
        public EDropItemAction Action;
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public float RotateSpeed;
    }

    public struct BuffConfig
    {
        public int Id;
        public float Duration;
        public int LayerLimit;
        public int Group;
        public int EffectId;
        public int RemoveEffectId;
        public EBuffType BuffType;
        public EInfluenceSummon InfluenceSummon;
        public int SummonId;
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public float Param5;
        public float Param6;
        public float Param7;
        public float Param8;
        public int BulletId;
        public Color Color;
    }

    public struct SkillConfig
    {
        public int Id;
        public int CastCount;
        public int MaxTriggerCount;
        public int Group;
        public int BindingGroup;
        public int ClassId;

        public SkillTriggerConfig Trigger;
        public SkillTargetConfig Target;
        public SkillCastConfig Cast;

        public SkillActionConfig End1;
        public SkillActionConfig End2;
        public SkillActionConfig End3;
        public SkillActionConfig End4;
        public SkillActionConfig End5;
        public SkillActionConfig End6;
    }

    public struct SkillTriggerConfig
    {
        public ESkillTrigger Method;
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public float Param5;
        public float Param6;
        public float Param7;
        public float Param8;
    }

    public struct SkillTargetConfig
    {
        public ESkillTarget Method;
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public float Param5;
        public float Param6;
        public float Param7;
        public float Param8;
    }

    public struct SkillCastConfig
    {
        public ESkillCast Method;
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public float Param5;
        public float Param6;
        public float Param7;
        public float Param8;
    }

    public struct SkillActionConfig
    {
        public ESkillAction Action;
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public float Param5;
        public float Param6;
        public float Param7;
        public float Param8;
    }
}