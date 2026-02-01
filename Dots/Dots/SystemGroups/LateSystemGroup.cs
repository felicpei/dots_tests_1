using Unity.Entities;
using UnityEngine.Scripting;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class LateSystemGroup : ComponentSystemGroup
{
    [Preserve]
    public LateSystemGroup()
    {
    }
}