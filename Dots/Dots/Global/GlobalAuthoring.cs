using Lobby;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Dots
{
    public class GlobalAuthoring : MonoBehaviour
    {
        public class FactoryAuthoringBaker : Baker<GlobalAuthoring>
        {
            public override void Bake(GlobalAuthoring authroing)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var emptyObj = XResource.LoadEditorAsset("Default/Empty.prefab") as GameObject;
                var monsterObj = XResource.LoadEditorAsset("Default/Monster.prefab") as GameObject;
                var monsterFlyObj = XResource.LoadEditorAsset("Default/MonsterFly.prefab") as GameObject;
                var monsterNoneObj = XResource.LoadEditorAsset("Default/MonsterNone.prefab") as GameObject;
                var playerObj = XResource.LoadEditorAsset("Default/Player.prefab") as GameObject;
                var servantObj = XResource.LoadEditorAsset("Default/Servant.prefab") as GameObject;
                var bulletPhysics = XResource.LoadEditorAsset("Default/BulletPhysics.prefab") as GameObject;
                var mapGround = XResource.LoadEditorAsset("Default/MapGround.prefab") as GameObject;
                
                AddComponent(entity, new GlobalPrefabs
                {
                    Empty = GetEntity(emptyObj, TransformUsageFlags.Dynamic),
                    Monster = GetEntity(monsterObj, TransformUsageFlags.Dynamic),
                    MonsterFly = GetEntity(monsterFlyObj, TransformUsageFlags.Dynamic),
                    MonsterNone = GetEntity(monsterNoneObj, TransformUsageFlags.Dynamic),
                    Player = GetEntity(playerObj, TransformUsageFlags.Dynamic),
                    Servant = GetEntity(servantObj, TransformUsageFlags.Dynamic),
                    BulletPhysics = GetEntity(bulletPhysics, TransformUsageFlags.Dynamic),
                    MapGround = GetEntity(mapGround, TransformUsageFlags.Dynamic),
                });
                
                AddBuffer<BulletCreateBuffer>(entity);
                AddBuffer<EffectCreateBuffer>(entity);
                AddBuffer<DestroyEffectByFrom>(entity);
                AddBuffer<SummonMonsterCreateBuffer>(entity);
                AddBuffer<MonsterCreateBuffer>(entity);
                AddBuffer<ProgressBarCreateBuffer>(entity);
                AddBuffer<DropItemCreateBuffer>(entity);
                AddBuffer<DamageNumberCreateBuffer>(entity);
                AddBuffer<CreateBuffData>(entity);
                AddBuffer<CreateSkillBuffer>(entity);
                AddBuffer<RemoveSkillBuffer>(entity);
                AddBuffer<EntityDestroyBuffer>(entity);
                AddBuffer<PlaySoundBuffer>(entity);
                AddBuffer<PlayCameraShakeBuffer>(entity);
                AddBuffer<ControllerShakeBuffer>(entity);
                
                //event 
                AddBuffer<MonsterDieEvent>(entity);
                AddBuffer<MoveHistory>(entity);
                
                //初始化标记 
                AddComponent<GlobalInitTag>(entity);
            }
        }
    }


    public readonly partial struct GlobalAspect : IAspect
    {
        public readonly Entity Entity;
        public readonly RefRW<RandomSeed> Random;

        public readonly DynamicBuffer<MoveHistory> MoveHistory;
        public readonly DynamicBuffer<EntityDestroyBuffer> EntityDestroyBuffer;
        public readonly DynamicBuffer<BulletCreateBuffer> BulletCreateBuffer;
        public readonly DynamicBuffer<EffectCreateBuffer> EffectCreateBuffer;
        public readonly DynamicBuffer<DestroyEffectByFrom> EffectDestroyBuffer;
        public readonly DynamicBuffer<DropItemCreateBuffer> DropItemCreateBuffer;
        public readonly DynamicBuffer<SummonMonsterCreateBuffer> SummonCreateBuffer;
        public readonly DynamicBuffer<MonsterCreateBuffer> MonsterCreateBuffer;
        public readonly DynamicBuffer<ProgressBarCreateBuffer> ProgressBarCreateBuffer;
        
        public readonly DynamicBuffer<PlaySoundBuffer> PlaySoundBuffer;
        public readonly DynamicBuffer<PlayCameraShakeBuffer> PlayCameraShakeBuffer;
        public readonly DynamicBuffer<ControllerShakeBuffer> ControllerShakeBuffer;
        public readonly DynamicBuffer<DamageNumberCreateBuffer> DamageNumberCreateBuffer;
        public readonly DynamicBuffer<CreateBuffData> CreateBuffData;
        public readonly DynamicBuffer<CreateSkillBuffer> CreateSkillBuffer;
        public readonly DynamicBuffer<RemoveSkillBuffer> RemoveSkillBuffer;

        public readonly DynamicBuffer<MonsterDieEvent> MonsterDieEvent;
        
        private readonly RefRO<GlobalPrefabs> _prefabs;
        private readonly RefRW<GlobalData> _data;
        private readonly RefRW<GlobalMonsterPauseData> _monsterPauseData;
        private readonly RefRW<GlobalBossInfo> _bossInfo;

        public int MissionId => _data.ValueRO.MissionId;
        public int LocalPlayerId => _data.ValueRO.LocalPlayerId;

        public float3 PlayerBornPos => new(10000, 0, 0);
        public float MonsterBrightness => 1f;
        
        public bool ShowDamageNumber
        {
            get => _data.ValueRO.ShowDamageNumber;
            set => _data.ValueRW.ShowDamageNumber = value;
        }

        public bool CameraInited
        {
            get => _data.ValueRO.CameraInited;
            set => _data.ValueRW.CameraInited = value;
        }
        
        public int WaveTotal => _data.ValueRO.WaveTotal;
        
        public int WaveId
        {
            set => _data.ValueRW.WaveId = value;
            get => _data.ValueRO.WaveId;
        }
        public bool InWave
        {
            set => _data.ValueRW.InWave = value;
            get => _data.ValueRO.InWave;
        }
    
        public float WaveTotalTime
        {
            set => _data.ValueRW.WaveTotalTime = value;
            get => _data.ValueRO.WaveTotalTime;
        }

        public float WaveCurTime
        {
            set => _data.ValueRW.WaveCurTime = value;
            get => _data.ValueRO.WaveCurTime;
        }
        
        public int WavePerGold
        {
            set => _data.ValueRW.WavePerGold = value;
            get => _data.ValueRO.WavePerGold;
        }

        public float WavePerGoldProb
        {
            set => _data.ValueRW.WavePerGoldProb = value;
            get => _data.ValueRO.WavePerGoldProb;
        }
        
        public float WavePerMaterial
        {
            set => _data.ValueRW.WavePerMaterial = value;
            get => _data.ValueRO.WavePerMaterial;
        }

        public int NoDropMaterialCount
        {
            set => _data.ValueRW.NoDropMaterialCount = value;
            get => _data.ValueRO.NoDropMaterialCount;
        }

        public float WaveSaveMaterial
        {
            set => _data.ValueRW.WaveSaveMaterial = value;
            get => _data.ValueRO.WaveSaveMaterial;
        }
        
        public int TotalMonsterCount
        {
            get => _data.ValueRO.TotalMonsterCount;
            set => _data.ValueRW.TotalMonsterCount = value;
        }
        
        public bool PauseTag
        {
            set => _data.ValueRW.PauseTag = value;
            get => _data.ValueRO.PauseTag;
        }

        public bool UnPauseTag
        {
            set => _data.ValueRW.UnpauseTag = value;
            get => _data.ValueRO.UnpauseTag;
        }

        public bool InPause
        {
            set => _data.ValueRW.InPause = value;
            get => _data.ValueRO.InPause;
        }

        public bool InMonsterPause
        {
            set => _monsterPauseData.ValueRW.InMonsterPause = value;
            get => _monsterPauseData.ValueRO.InMonsterPause;
        }

        public bool PauseMonsterTag
        {
            set => _monsterPauseData.ValueRW.PauseMonsterTag = value;
            get => _monsterPauseData.ValueRO.PauseMonsterTag;
        }

        public bool UnPauseMonsterTag
        {
            set => _monsterPauseData.ValueRW.UnpauseMonsterTag = value;
            get => _monsterPauseData.ValueRO.UnpauseMonsterTag;
        }

        public float Time
        {
            get => _data.ValueRO.Time;
            set => _data.ValueRW.Time = value;
        }

        public int CurrBossId => _bossInfo.ValueRO.MonsterId;
        public float CurrBossHpPercent => _bossInfo.ValueRO.HpPercent;

        public float4 ScreenBound
        {
            set => _data.ValueRW.ScreenBound = value;
            get => _data.ValueRO.ScreenBound;
        }

        public quaternion CameraRotation
        {
            set => _data.ValueRW.CameraRotation = value;
            get => _data.ValueRO.CameraRotation;
        }

        public float3 CameraPos
        {
            set => _data.ValueRW.CameraPos = value;
            get => _data.ValueRO.CameraPos;
        }

        public Entity Empty => _prefabs.ValueRO.Empty;
        public GlobalPrefabs Prefabs => _prefabs.ValueRO;
        public FightProps PlayerProps => _data.ValueRO.PlayerProps;
        public FightProps MonsterProps => _data.ValueRO.MonsterProps;

        public Random CreateRandomSeed()
        {
            return Unity.Mathematics.Random.CreateFromIndex(Random.ValueRW.Value.NextUInt(0, uint.MaxValue));
        }

        public bool GetSubServantPos(int idx, out float3 targetPos)
        {
            if (MoveHistory.Length > 0)
            {
                if (idx == 0)
                {
                    if (MoveHistory.Length > 0)
                    {
                        targetPos = MoveHistory[^1].Value;
                        return true;
                    }
                }
                else
                {
                    var aIdx = idx * 4;
                    if (aIdx > 0 && aIdx <= MoveHistory.Length)
                    {
                        targetPos = MoveHistory[^aIdx].Value;
                        return true;
                    }
                }
            }

            targetPos = default;
            return false;
        }
    }
}