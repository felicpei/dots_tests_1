using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dots
{
    public struct MaterialBlend : IComponentData
    {
        public float4 Color;
        public float Value;
    }
    
    public struct MaterialFillRate : IComponentData
    {
        public float Value;
    }
}