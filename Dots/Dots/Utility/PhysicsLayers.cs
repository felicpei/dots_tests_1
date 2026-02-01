using Unity.Physics;

namespace Dots
{
    public static class PhysicsLayers
    {
        public const uint None = 0;
        public const uint Monster = 1u << 2;
        public const uint Character = 1u << 3;
        public const uint Ground = 1u << 4;
        public const uint Obstacle = 1u << 5;
        public const uint Bullet = 1u << 6;
         
        public const uint Creature = Monster | Character;
        public const uint DontMove = Obstacle | Ground; 

        public static CollisionFilter GetFilter(uint layer)
        { 
            return new CollisionFilter
            { 
                BelongsTo = ~0u, // all 1s, so all layers, collide with everything, 
                CollidesWith = layer, 
                GroupIndex = 0 
            }; 
        }
    }
}