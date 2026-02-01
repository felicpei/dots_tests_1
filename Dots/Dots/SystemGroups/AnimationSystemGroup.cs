using Dots;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Dots
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EffectSystemGroup))]
    public partial class AnimationSystemGroup : ComponentSystemGroup
    {
        [Preserve]
        public AnimationSystemGroup()
        {
        }
    }
}