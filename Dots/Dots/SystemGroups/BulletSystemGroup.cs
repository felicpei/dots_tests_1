using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BulletSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public BulletSystemGroup()
        {
        }
    }
}