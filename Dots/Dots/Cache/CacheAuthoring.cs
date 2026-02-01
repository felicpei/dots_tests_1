using Deploys;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    public class CacheAuthoring : MonoBehaviour
    {
        public class CacheAuthoringBaker : Baker<CacheAuthoring>
        {
            public override void Bake(CacheAuthoring authroing)
            {
                var entity = GetEntity(TransformUsageFlags.None);
              
                //初始化标记 
                AddComponent<CacheInitTag>(entity);
            }
        }
    }

    public readonly partial struct CacheAspect : IAspect
    {
        public readonly RefRO<CacheProperties> CacheProperties;

        public bool GetDropItemConfig(int id, out DropItemConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.DropItemConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.DropItemConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.DropItemConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }

        public bool GetMonsterConfig(int id, out MonsterConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.MonsterConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.MonsterConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.MonsterConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }

        public bool GetServantConfig(int id, out ServantConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.ServantConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.ServantConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.ServantConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }

        public bool GetBulletConfig(int id, out BulletConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.BulletConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.BulletConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.BulletConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }

        public bool GetBuffConfig(int buffId, out BuffConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.BuffConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.BuffConfig.Value.Value[i].Id == buffId)
                {
                    config = CacheProperties.ValueRO.BuffConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }


        public bool GetSkillConfig(int skillId, out SkillConfig skillConfig)
        {
            skillConfig = default;
            for (var i = 0; i < CacheProperties.ValueRO.SkillConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.SkillConfig.Value.Value[i].Id == skillId)
                {
                    skillConfig = CacheProperties.ValueRO.SkillConfig.Value.Value[i];
                    return true;
                }
            }

            return false;
        }

        public bool GetBulletBehaviour(int id, out BulletBehaviourConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.BulletBehaviourConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.BulletBehaviourConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.BulletBehaviourConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }
        
        public bool GetResourceConfig(int id, out ResourceCfg cfg)
        {
            for (var i = 0; i < CacheProperties.ValueRO.ResourceConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.ResourceConfig.Value.Value[i].Id == id)
                {
                    cfg = CacheProperties.ValueRO.ResourceConfig.Value.Value[i];
                    return true;
                }
            }

            cfg = default;
            return false;
        }

        public bool GetDamageNumberConfig(out DamageNumberConfig config, int id = 0)
        {
            for (var i = 0; i < CacheProperties.ValueRO.DamageNumberConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.DamageNumberConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.DamageNumberConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }
        
        public bool GetLocalPlayerConfig(int id, out LocalPlayerConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.LocalPlayerConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.LocalPlayerConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.LocalPlayerConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }

        
        public bool GetElementConfig(EElement id, out ElementConfig config)
        {
            for (var i = 0; i < CacheProperties.ValueRO.CacheElementConfig.Value.Value.Length; i++)
            {
                if (CacheProperties.ValueRO.CacheElementConfig.Value.Value[i].Id == id)
                {
                    config = CacheProperties.ValueRO.CacheElementConfig.Value.Value[i];
                    return true;
                }
            }

            config = default;
            return false;
        }
        
        public float GetAgainstFactor(EElement attacker, EElement defender)
        {
            if (!GetElementConfig(attacker, out var attackerDp))
            {
                Debug.LogError($"get attacker AgainstFactor error, attacker:{attacker}");
                return 1f;
            }

            switch (defender)
            {
                case EElement.None:
                    return attackerDp.None;
                case EElement.Water:
                    return attackerDp.Water;
                case EElement.Fire:
                    return attackerDp.Fire;
                case EElement.Ice:
                    return attackerDp.Ice;
                case EElement.Lighting:
                    return attackerDp.Lighting;
                case EElement.Stone:
                    return attackerDp.Stone;
                default:
                {
                    Debug.LogError($"get AgainstFactor error, defender element:{defender}");
                    return 1f;
                }
            }
        }
    }
}