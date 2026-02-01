
using Dots;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MonsterSystemGroup))]
    public partial class DropItemSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public DropItemSystemGroup()
        {
        }
    }
}