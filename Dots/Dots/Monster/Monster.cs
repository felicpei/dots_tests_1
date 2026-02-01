    using Deploys;
    using Unity.Entities;
    using Unity.Mathematics;



    namespace Dots
    {
        public struct MonsterProperties : IComponentData
        {
            public int Id;
            public int SpawnPointId;
            public float3 BornPos;
            public float3 CenterPos;
        }

        public struct MonsterDelayDestroy : IComponentData, IEnableableComponent
        {
            public float DelayTime;
            public float Timer;
        }

        public struct MonsterMove : IComponentData
        {
            //readonly params
            public EMonsterMoveMode Mode;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
            public float Param5;
            public float Param6;
            
            //temp variant
            public bool Init;
            public bool IsMoving;
            public float Timer;
            public float DistCounter;
            public Entity CurrentTarget;
            public Entity PrevTarget;
            public float3 Dir;
            public float3 TargetPos;
            
            //Around
            public float AroundAngle;
            
            //ToCenter
            public float FormationMinDist;
            public float3 BornPos;
            public float3 CenterPos;
            public bool ToCenter;
            public bool MoveLeft;
        }
            
        public struct MonsterInitTag : IComponentData
        {
        }
        
        [InternalBufferCapacity(2)]
        public struct MonsterDropInfo : IBufferElementData
        {
            public bool IsExp;
            public bool IsGold;
            public int ItemId;
            public int ItemCount;
        }

        public struct MonsterDropMaterial : IComponentData
        {
            public int Count;
        }
    
        public struct MonsterDropGold : IComponentData
        {
            public int Count;
        }
        
        public struct MonsterTarget : IComponentData
        {
            public bool HasTarget;
            public float3 Pos;
            public Entity HatredEnemy;
        }

        public struct MonsterDestroyTag : IComponentData, IEnableableComponent
        {
            public float DestroyDelay;
            public float Timer;
        }
        

        public struct InBornTag : IComponentData, IEnableableComponent
        {
            public float Timer;
        }

    }