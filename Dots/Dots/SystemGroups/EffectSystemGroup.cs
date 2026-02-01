using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DropItemSystemGroup))]
    public partial class EffectSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public EffectSystemGroup()
        {
        }
    }
}