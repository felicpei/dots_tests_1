using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GlobalSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public GlobalSystemGroup()
        {
        }
    }
}