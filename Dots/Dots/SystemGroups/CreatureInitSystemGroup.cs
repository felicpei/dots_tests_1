using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(GlobalSystemGroup))]
    public partial class CreatureInitSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public CreatureInitSystemGroup()
        {
        }
    }
}