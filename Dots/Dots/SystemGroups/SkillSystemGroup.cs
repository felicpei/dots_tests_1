using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletSystemGroup))]
    public partial class SkillSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public SkillSystemGroup()
        {
        }
    }
}