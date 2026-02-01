using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class MapSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public MapSystemGroup()
        {
        }
    }
}