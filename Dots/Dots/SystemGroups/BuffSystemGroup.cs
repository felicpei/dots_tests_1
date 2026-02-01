using Dots;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkillSystemGroup))]
    public partial class BuffSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public BuffSystemGroup()
        {
        }
    }
}