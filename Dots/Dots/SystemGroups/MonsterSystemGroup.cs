using Dots;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerSystemGroup))]
    public partial class MonsterSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public MonsterSystemGroup()
        {
        }
    }
}