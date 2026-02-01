using Unity.Entities;
using Unity.Mathematics;

namespace Dots
{
    
    public struct MapProperties : IComponentData
    {
        public bool Inited;
        public float2 CurChunkIndex;
        public int MapResId;
        public float Scale;
    }
    
    public struct MapGeneratedChunk : IBufferElementData
    {
        public float2 Value;
    }

    public struct MapTag : IComponentData
    {
        public float2 Idx;
    }
}