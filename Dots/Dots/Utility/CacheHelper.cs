using Dots;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Dots
{
    public class CacheHelper
    {
        public static IntRandomArr CreateRandomArray(int[] arr)
        {
            var result = new IntRandomArr();
            for (var i = 0; arr != null && i < arr.Length; i++)
            {
                if (i == 0) result.Id0 = arr[i];
                else if (i == 1) result.Id1 = arr[i];
                else if (i == 2) result.Id2 = arr[i];
                else if (i == 3) result.Id3 = arr[i];
                else if (i == 4) result.Id4 = arr[i];
                else if (i == 5) result.Id5 = arr[i];
                else if (i == 6) result.Id6 = arr[i];
                else if (i == 7) result.Id7 = arr[i];
                else if (i == 8) result.Id8 = arr[i];
                else if (i == 9) result.Id9 = arr[i];
            }
            return result;
        }

        public static NativeList<int> RandomToNativeList(IntRandomArr arr)
        {
            var result = new NativeList<int>(Allocator.Temp);
            if(arr.Id0 != 0) result.Add(arr.Id0);
            if(arr.Id1 != 0) result.Add(arr.Id1);
            if(arr.Id2 != 0) result.Add(arr.Id2);
            if(arr.Id3 != 0) result.Add(arr.Id3);
            if(arr.Id4 != 0) result.Add(arr.Id4);
            if(arr.Id5 != 0) result.Add(arr.Id5);
            if(arr.Id6 != 0) result.Add(arr.Id6);
            if(arr.Id7 != 0) result.Add(arr.Id7);
            if(arr.Id8 != 0) result.Add(arr.Id8);
            if(arr.Id9 != 0) result.Add(arr.Id9);
            return result;
        }

        public static int RandomInt(RefRW<RandomSeed> random, IntRandomArr arr)
        {
            var list = RandomToNativeList(arr);
            var result = 0;
            if (list.Length > 0)
            {
                result = list[random.ValueRW.Value.NextInt(0, list.Length)];
            }

            list.Dispose();   
            return result;
        }
        
        public static bool GetBuffConfig(int buffId, Entity entity, ComponentLookup<CacheProperties> cacheLookup, out BuffConfig result)
        {
            if (cacheLookup.TryGetComponent(entity, out var cache))
            {
                for (var i = 0; i < cache.BuffConfig.Value.Value.Length; i++)
                {
                    if (cache.BuffConfig.Value.Value[i].Id == buffId)
                    {
                        result = cache.BuffConfig.Value.Value[i];
                        return true;
                    }
                }
            }

            Debug.LogError($"get buffConfig error, id:{buffId}");
            result = default;
            return false;
        }
        
        public static bool GetBulletConfig(int bulletId, Entity entity, ComponentLookup<CacheProperties> cacheLookup, out BulletConfig result)
        {
            if (cacheLookup.TryGetComponent(entity, out var cache))
            {
                for (var i = 0; i < cache.BulletConfig.Value.Value.Length; i++)
                {
                    if (cache.BulletConfig.Value.Value[i].Id == bulletId)
                    {
                        result = cache.BulletConfig.Value.Value[i];
                        return true;
                    }
                }
            }

            Debug.LogError($"GetBulletConfig error, id:{bulletId}");
            result = default;
            return false;
        }
        
        public static bool GetBulletBehaviourConfig(int id, Entity entity, ComponentLookup<CacheProperties> cacheLookup, out BulletBehaviourConfig result)
        {
            if (cacheLookup.TryGetComponent(entity, out var cache))
            {
                for (var i = 0; i < cache.BulletBehaviourConfig.Value.Value.Length; i++)
                {
                    if (cache.BulletBehaviourConfig.Value.Value[i].Id == id)
                    {
                        result = cache.BulletBehaviourConfig.Value.Value[i];
                        return true;
                    }
                }
            }
            Debug.LogError($"GetBulletBehaviourConfig error, id:{id}");
            result = default;
            return false;
        }
        
        public static bool GetSkillConfig(int skillId, Entity entity, ComponentLookup<CacheProperties> cacheLookup, out SkillConfig result)
        {
            if (cacheLookup.TryGetComponent(entity, out var cache))
            {
                for (var i = 0; i < cache.SkillConfig.Value.Value.Length; i++)
                {
                    if (cache.SkillConfig.Value.Value[i].Id == skillId)
                    {
                        result = cache.SkillConfig.Value.Value[i];
                        return true;
                    }
                }
            }
            Debug.LogError($"GetSkillConfig error, id:{skillId}");
            result = default;
            return false;
        }
        
        public static bool GetMonsterConfig(int id, Entity entity, ComponentLookup<CacheProperties> cacheLookup, out MonsterConfig result)
        {
            if (cacheLookup.TryGetComponent(entity, out var cache))
            {
                for (var i = 0; i < cache.MonsterConfig.Value.Value.Length; i++)
                {
                    if (cache.MonsterConfig.Value.Value[i].Id == id)
                    {
                        result = cache.MonsterConfig.Value.Value[i];
                        return true;
                    }
                }
            }
            Debug.LogError($"GetMonsterConfig error, id:{id}");
            result = default;
            return false;
        }
        
        public static bool GetDropItemConfig(int id, Entity entity, ComponentLookup<CacheProperties> cacheLookup, out DropItemConfig result)
        {
            if (cacheLookup.TryGetComponent(entity, out var cache))
            {
                for (var i = 0; i < cache.DropItemConfig.Value.Value.Length; i++)
                {
                    if (cache.DropItemConfig.Value.Value[i].Id == id)
                    {
                        result = cache.DropItemConfig.Value.Value[i];
                        return true;
                    }
                }
            } 
            Debug.LogError($"GetDropItemConfig error, id:{id}");
            result = default;
            return false;
        }
    }
}