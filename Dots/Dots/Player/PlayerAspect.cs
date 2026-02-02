using System;
using Deploys;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    public readonly partial struct Player1Aspect : IAspect
    {
        public readonly Entity Entity;
        public readonly RefRW<RandomSeed> Random;


        public readonly RefRW<LocalPlayerTag> Properties;
        public readonly RefRW<LocalTransform> Transform;
        public readonly RefRW<PlayerAttrData> Attr;
        public readonly RefRO<ShieldProperties> Shield;
        private readonly DynamicBuffer<PlayerAttrModify> _attrModify;
        
        
        public readonly DynamicBuffer<ServantList> Servants;
        public readonly DynamicBuffer<DpsAppendBuffer> DpsAppend;
        public readonly DynamicBuffer<PlayerDpsBuffer> DpsBuffer;
        public readonly DynamicBuffer<UIUpdateBuffer> UIUpdateBuffer;


        public float3 Position => Transform.ValueRO.Position;

        public int ServantCount => Servants.Length;

        public void AddAttrModify(EAttr type, float add)
        {
            var bFind = false;
            PlayerAttrModify attr = default;
            for (var i = 0; i < _attrModify.Length; i++)
            {
                if (_attrModify[i].Type == type)
                {
                    attr = _attrModify[i];
                    bFind = true;
                    _attrModify.RemoveAt(i);
                    break;
                }
            }

            if (bFind)
            {
                attr.Value += add;
            }
            else
            {
                attr.Type = type;
                attr.Value = add;
            }
            _attrModify.Add(attr);
        }
    }
}