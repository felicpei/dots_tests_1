using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class MonsterSpawnSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public MonsterSpawnSystemGroup()
        {
        }
    }
}