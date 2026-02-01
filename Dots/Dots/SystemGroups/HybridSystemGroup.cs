using Dots;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(LateSystemGroup))]
    public partial class HybridSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public HybridSystemGroup()
        {
        }
    }
}