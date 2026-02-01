using Dots;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CreatureSystemGroup))]
    public partial class PlayerSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public PlayerSystemGroup()
        {
        }
    }
}