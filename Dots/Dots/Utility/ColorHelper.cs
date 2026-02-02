using Dots;
using Unity.Entities;
using Color = UnityEngine.Color;

public static class ColorHelper
{
    public static bool GetCreatureColor(Entity entity, BufferLookup<BuffEntities> buffEntitiesLookup, ComponentLookup<BuffTag> buffTagLookup, Entity cacheEntity, ComponentLookup<CacheProperties> cacheLookup,
        out float opacity, out Color color)
    {
        if (buffEntitiesLookup.TryGetBuffer(entity, out var buffEntities))
        {
            foreach (var buffEntity in buffEntities)
            {
                if (buffTagLookup.TryGetComponent(buffEntity.Value, out var buffTag))
                {
                    if (CacheHelper.GetBuffConfig(buffTag.BuffId, cacheEntity, cacheLookup, out var buffConfig))
                    {
                        if (buffConfig.Color.a > 0)
                        {
                            color = buffConfig.Color;
                            opacity = buffConfig.Color.a;
                            return true;
                        }
                    }
                }
            }
        }

        opacity = 0f;
        color = default;
        return false;
    }
}