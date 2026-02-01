using Unity.Entities;
using Unity.Mathematics;
using Deploys;

namespace Dots
{
    public struct PlayerInitTag : IComponentData
    {
    }
    
    //标记本地玩家
    public struct LocalPlayerTag : IComponentData
    {
        public int Id;
        public int Level;
        public int NeedExp;
    }

    public struct PlayerDpsBuffer : IBufferElementData
    {
        public int ServantId;
        public float DpsTotalDamage;
        public float DpsStartTime;
    }

    public struct PlayerAttrData : IComponentData
    {
        public float MaxHp;
        public float Recovery;
        public float MoveSpeed;
        public float Armor;
        public float Dodge;
        public float Crit;
        public float CritDamage;
        public float Damage;
        public float PhysicsAtk;
        public float FireAtk;
        public float WaterAtk;
        public float IceAtk;
        public float LightingAtk;
        public float StoneAtk;
        public float SkillSpeed;
        public float BulletSpeed;
        public float DamageInterval;
        public float ShootCount;
        public float DamageRange;
        public float ExpFactor;
        public float Lucky;
        public float Interest;
        public float BossDamage;
        public float EnemySpeed;
        public float EnemyCount;
        public float PickupRange;
    }
    
    public struct PlayerAttrModify : IBufferElementData
    {
        public EAttr Type;
        public float Value;
    }
    
    [InternalBufferCapacity(8)]
    public struct DpsAppendBuffer : IBufferElementData, IEnableableComponent
    {
        public float Damage;
        public int ServantId;  
    }
    
    public struct UIUpdateBuffer : IBufferElementData
    {
        public EventData Value;
    }
    
    //to global
    public struct MoveHistory : IBufferElementData
    {
        public float3 Value;
    }
    
    //to main servant
    [InternalBufferCapacity(8)]
    public struct PickupBuffer : IBufferElementData, IEnableableComponent
    {
        public EDropItemAction Action;
        public int DropItemId;
        public int Value;
        public double Param1;
        public double Param2;
        public double Param3;
        public double Param4;
    }

    public struct PickupMagnetTag : IComponentData, IEnableableComponent
    {
        public float Radius;
    }

    public struct PickupSkillTag : IComponentData, IEnableableComponent
    {
        public int SkillId;
        public float ContTime;
        public float Timer;
    }
}