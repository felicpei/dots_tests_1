using Unity.Collections;
using Unity.Entities;


namespace Dots
{
    public struct SpawnWaveInitTag : IComponentData
    {
    }

    public struct MissionWaveInited : IComponentData
    {
        
    }
    
    public struct SpawnMonsterPoint
    {
        public ESpawnShape Shape;
        public int TotalCount;
        public float Width;
        public float Height;
        public float RandomRange;
        public float TimeInterval;
        public float DistInterval;
    }

    public struct SpawnMonsterFormation
    {
        public ESpawnShape Shape;
        public float SplitInterval;
        public float Width;
        public float Height;
        public float DelayDestroy;
    }

    public struct SpawnLinear
    {
        public ESpawnShape Shape;
        public float Width;
        public float Height;
        public float RandomRange;
        public int MaxAliveLimit;
        public float TotalSec;
        public int StartCount;
        public int EndCount;
        public int WaveCount;
        public int CountInterval;   //每波增加数量
        public int CachingCount;
    }

    public struct SpawnLinearGroup
    {
        public ESpawnShape Shape;
        public float Width;
        public float Height;
        public int RandomRange;
        public int WaveCount;
        public float TimeInterval;
        public int StartCount;
        public int IntervalAddCount;
        public int IntervalAddHp;
        public float DistInterval;
    }

    public struct SpawnMonsterProperties : IComponentData
    {
        public int Id;
        public NativeList<int> MonsterId;
        
        public float Hp;
        public float Atk;
        public float SpeedFac;
        public ETeamId Team;
        
        public ESpawnMode Mode;
        public SpawnMonsterPoint PointData;
        public SpawnMonsterFormation FormationData;
        public SpawnLinear LinearData;
        public SpawnLinearGroup GroupLinearData;
        
        public int RefreshedCount;
        public int AliveCount;
        public int ReadyCount;
        public bool RefreshFinished;
        public float StartTime;
        public float Timer;
        
        //linear用
        public int CurrWaveId;
        
        //notice
        public EBattleNotice NoticeId;
        public float CameraFov;
        
        //怪物类型
        public ECreatureType MonsterType;
        public int TimelineId;
        
        //覆盖默认移动方式
        public MonsterMoveConfig MoveConfig; 
        public int Mass;
    }
}