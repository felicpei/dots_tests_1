using Dots;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PlayerSystemGroup))]
    public partial struct InputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            state.RequireForUpdate<LocalPlayerTag>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
            if (!SystemAPI.HasComponent<LocalTransform>(localPlayer))
            {
                return;
            }

            //更新input相关参数
            var inputProperties = SystemAPI.GetSingletonRW<InputProperties>();
            inputProperties.ValueRW.MoveDirection = VirtualMoveJoystick.Direction;
            inputProperties.ValueRW.MovePercent = VirtualMoveJoystick.Percent;
            inputProperties.ValueRW.MoveMode = VirtualMoveJoystick.Mode;
        }
    }
}