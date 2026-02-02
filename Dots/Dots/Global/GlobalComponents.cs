using Deploys;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;


namespace Dots
{
    public enum ETeamId
    {
        Player = 1,
        Monster = 2,
    }
    
    public enum EResourceId
    {
        Empty = 0,
        DamageNumber = 1,
        ProgressBarRed = 2, 
        ProgressBarGreen = 3,
        DamageElement = 4,
    }

    public struct AtkValue
    {
        public readonly float Atk;
        public readonly float Crit;
        public readonly float CritDamage;
        public readonly ETeamId Team;

        public AtkValue(float atk, float crit, float critDamage, ETeamId team)
        {
            Atk = atk;
            Crit = crit;
            CritDamage = critDamage;
            Team = team;
        }
    }

    //读取摇杆相关参数，存储到InputProperties中
    public struct InputProperties : IComponentData
    {
        public EMoveJoyMode MoveMode;
        public float2 MoveDirection;
        public float MovePercent;
    }

    public struct GlobalPrefabs : IComponentData
    {
        public Entity Empty;
        public Entity Monster;
        public Entity MonsterNone;
        public Entity MonsterFly;
        public Entity Player;
        public Entity Servant;
        public Entity BulletPhysics;
        public Entity MapGround;
    }

    public struct MissionStartTag : IComponentData
    {
        public float Timer;
    }
    
    public struct GlobalData : IComponentData
    {
        public int MissionId;
        public int LocalPlayerId;
        public int TotalMonsterCount;

        //pause, missionTime
        public bool InPause;
        public float Time;

        //props
        public FightProps PlayerProps;
        public FightProps MonsterProps;

        //tags
        public float4 ScreenBound;  
        public bool PauseTag;
        public bool UnpauseTag;
        
        //wave info
        public int WaveId;
        public bool InWave;
        public int WaveTotal;
        public float WaveTotalTime;
        public float WaveCurTime;
        
        //drop material
        public float WavePerMaterial;
        public int NoDropMaterialCount;
        public float WaveSaveMaterial;
        
        //drop gold
        public int WavePerGold;
        public float WavePerGoldProb;
        
        //当前CameraFov
        public quaternion CameraRotation;
        public float3 CameraPos;
        
        //game settings
        public bool ShowDamageNumber;

        public bool CameraInited;
    }

    public struct GlobalBossInfo : IComponentData
    {
        public int MonsterId;
        public float HpPercent;
    }

    public struct GlobalMonsterPauseData : IComponentData
    {
        public bool InMonsterPause;
        public bool PauseMonsterTag;
        public bool UnpauseMonsterTag;
        public float Timer;
        public float ContTime;
    }

    public struct GlobalInitTag : IComponentData
    {
    }
    
    public struct GlobalInitialized : IComponentData
    {
    }

    public struct ProgressBarComponent : IComponentData, IEnableableComponent
    {
        public Entity Value;
    }

    public struct RandomSeed : IComponentData
    {
        public Random Value;
    }
    
    //创建子弹的Data, 给BulletFactorySystem创建子弹，传入初始变量
    [InternalBufferCapacity(8)]
    public struct BulletCreateBuffer : IBufferElementData
    {
        public EBulletFrom From;
        public int FromId;
        
        public int BulletId;
        public AtkValue AtkValue;
        public float3 ShootPos; //起点
        public float3 Direction; //方向 
        public Entity ParentCreature;
     
        public bool ImmediatelyBomb; //立即爆炸
        public float BombRadius;
        public Entity ForceHitCreature;
        public float ForceDistance; //强制距离
        public Entity TraceCreature;
     
        public float ScaleFactor; //额外的缩放系数
        public float SpeedFactor;
        
        public bool DisableSplit;   //是否禁止分裂
        public bool DisableHitForce;
        public bool DisableBounce;

        public Entity SkillEntity; //是否是技能触发的
        public bool AllowSkillEndAction; //爆炸是否触发Skill的EndAction(前提要有SkillEntity)
        public int SkillEndActionIndex;
        public Entity TransformParent;
        
        public float3 AroundPos;
        public Entity AroundEntity; //围绕转圈的Entity
        public float AroundRadiusX;   //环绕横轴半径
        public float AroundRadiusY;   //环绕纵轴半径
        public float AroundDefaultAngle;
        public bool ReverseAround;
        
        //other
        public float Offset;
    }
    
    [InternalBufferCapacity(8)]
    public struct EntityDestroyBuffer : IBufferElementData
    {
        public Entity Value;
    }

    //创建伤害数字data
    //创建伤害数字data
    [InternalBufferCapacity(8)]
    public struct DamageNumberCreateBuffer : IBufferElementData
    {
        public int Id;
        public float Value;
        public float3 Position;
        public EDamageNumber Type;
        public EAgainstType AgainstType;
        public EElementReaction Reaction;
    }

    public struct ClearMonsterTag : IComponentData
    {
        public bool ContainBoss;
        public bool BanDrop;
        public float Delay;
        public float Timer;
    }

    [InternalBufferCapacity(8)]
    public struct SummonMonsterCreateBuffer : IBufferElementData
    {
        public int MonsterId;
        public Entity Parent;
        public float3 BornPos;
        public float BornAngle;     //用于召唤环绕情况下的初始角度
    }

    [InternalBufferCapacity(4)]
    public struct MonsterCreateBuffer : IBufferElementData
    {
        public int MonsterId;
        public float3 BornPos;
        public ETeamId TeamId;
        public float HpPercent;
        public bool IsBoss;
        public bool IsElite;
    }

    [InternalBufferCapacity(8)]
    public struct ProgressBarCreateBuffer : IBufferElementData
    {
        public bool IsGreen;
        public float3 StartPos;
        public float Value;
        public Entity Parent;
    }

    [InternalBufferCapacity(16)]
    public struct CreateBuffData : IBufferElementData
    {
        public readonly Entity Master;
        public readonly int BuffId;
        public readonly Entity Attacker;
        public readonly float ContTime;
        public readonly EBuffFrom From;
        public readonly int FromId;

        public CreateBuffData(Entity master, int buffId, Entity attacker, float contTime, EBuffFrom from, int fromId)
        {
            Master = master;
            BuffId = buffId;
            Attacker = attacker;
            ContTime = contTime;
            From = from;
            FromId = fromId;
        }
    }

    [InternalBufferCapacity(16)]
    public struct CreateSkillBuffer : IBufferElementData
    {
        public readonly Entity Master;
        public readonly int SkillId;
        public readonly AtkValue AtkValue;
        public readonly Entity StartEntity;
        public readonly float3 StartPos;

        public readonly int RootSkillId;
        public readonly float CastDelay;
        public readonly int Recursion;
        public readonly int MaxRecursion;
        public readonly SkillPrevTarget PrevTarget;

        public CreateSkillBuffer(Entity master, int skillId, AtkValue atkValue, Entity startEntity, float3 startPos,
            SkillPrevTarget prevInfo = default, int rootSkillId = 0, float castDelay = 0, int recursion = 0, int maxRecursion = 0)
        {
            Master = master;
            SkillId = skillId;
            AtkValue = atkValue;
            StartEntity = startEntity;
            StartPos = startPos;
            RootSkillId = rootSkillId;
            CastDelay = castDelay;
            Recursion = recursion;
            MaxRecursion = maxRecursion;
            PrevTarget = prevInfo;
        }
    }

    [InternalBufferCapacity(8)]
    public struct RemoveSkillBuffer : IBufferElementData
    {
        public readonly int SkillId;
        public readonly Entity Master;

        public RemoveSkillBuffer(Entity master, int skillId)
        {
            Master = master;
            SkillId = skillId;
        }
    }

    public struct AnimationMoveData
    {
        public bool Enable;
        public float Speed;
        public float3 Forward;
        public float TotalTime;
    }

    public struct AnimationRotateData
    {
        public bool Enable;
        public float Speed;
        public float3 CenterPos;
        public float Radius;
        public bool UseAtkRange;
        public float SourceScale;
        public Entity MasterCreature;
    }

    //Effect自动伸展信息, 有data的情况会自动变长并追踪到目标点或Entity
    public struct AnimationStretchData
    {
        public bool Enable;
        public Entity Master;
        public float3 TargetPos;
        public float3 StartPos;
        public float Time; //到终点时间
        public float Width;
    }

    public struct AnimationScaleData
    {
        public bool Enable;
        public float ScaleMultiple;
        public float UseTime;
    }

    //特效Data
    [InternalBufferCapacity(8)]
    public struct EffectCreateBuffer : IBufferElementData
    {
        public int ResourceId;
        public float3 Pos;
        public float3 LocalPos;
        public quaternion Rotation;
        public float DelayDestroy; 
        public float Scale;
        
        //parent info
        public Entity Parent;
        
        public EEffectFrom From;
        public int FromId;
        public bool Loop;

        public AnimationStretchData StretchData;
        public AnimationScaleData ScaleAni;
        public AnimationMoveData MoveData;
        public AnimationRotateData RotateData;
    }

    public enum EEffectFrom
    {
        None,
        Buff,
        Dash,
        Muzzle,
        Element,
        Shield,
    }
    
    public struct DestroyEffectByFrom : IBufferElementData
    {
        public Entity Parent;
        public EEffectFrom From;
        public int FromId;
    }

    //掉落Buffer
    [InternalBufferCapacity(8)]
    public struct DropItemCreateBuffer : IBufferElementData
    {
        public int DropItemId;
        public int Count;
        public float3 Pos;
        public int Value;
        public float RandomRange;
    }

    [InternalBufferCapacity(4)]
    public struct PlaySoundBuffer : IBufferElementData
    {
        public int SoundId;
        public bool IsLoop;
        public bool IsStop;
    }

    [InternalBufferCapacity(4)]
    public struct PlayCameraShakeBuffer : IBufferElementData
    {
        public float Radius;
        public float Time;
        public float3 Pos;
    }

    [InternalBufferCapacity(4)]
    public struct ControllerShakeBuffer : IBufferElementData
    {
        public HapticTypes ShakeType;
    }


    [InternalBufferCapacity(8)]
    public struct MonsterDieEvent : IBufferElementData
    {
        public int SpawnPointId;
        public ECollisionType CollisionType;
        public ECreatureType Type;
        public int MonsterId;
    }
}