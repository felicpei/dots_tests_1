using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dots
{
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(HybridUpdateTransformSystem))]
    public partial struct CameraSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<LocalToWorld> _localToWorldLookup;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalPlayerTag>();
            state.RequireForUpdate<GlobalInitialized>();
            
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _localToWorldLookup.Update(ref state);

            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());
           

            if (!global.CameraInited)
            {
                var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayerTag>();
                var localPlayerTrans = SystemAPI.GetComponent<LocalToWorld>(localPlayer);
                var lookAtPos = localPlayerTrans.Position;
                lookAtPos.z = global.PlayerBornPos.z;
                UpdateCamera(lookAtPos, SystemAPI.Time.DeltaTime);

                global.CameraRotation = CameraController.GetCameraRotation(out var pos);
                global.CameraPos = pos;
                global.CameraInited = true;
            }
            else
            {
                if (SystemAPI.TryGetSingletonEntity<MainServantTag>(out var mainServant) &&
                    _localToWorldLookup.TryGetComponent(mainServant, out var mainServantTrans))
                {
                    var lookAtPos = mainServantTrans.Position;
                    lookAtPos.z = global.PlayerBornPos.z;

                    UpdateCamera(lookAtPos, SystemAPI.Time.DeltaTime);
                    global.CameraRotation = CameraController.GetCameraRotation(out var pos);
                    global.CameraPos = pos;

                    /*
                    //各种屏幕限制参数
                    global.ScreenHeightRange = new float2(CameraController.ScreenPosLeftDown.y, CameraController.ScreenPosLeftTop.y);
                    global.ScreenWidthRangeTop = new float2(CameraController.ScreenPosLeftTop.x, CameraController.ScreenPosRightTop.x);
                    global.ScreenWidthRangeDown = new float2(CameraController.ScreenPosLeftDown.x, CameraController.ScreenPosRightDown.x);
                    global.UIMaterialPos = CameraController.MaterialPos;
                    global.UIGoldPos = CameraController.GoldPos;
                    global.AimingPos = CameraController.AimingPos;
                    global.ManualAiming = VirtualAimJoystick.InAim;

                    global.CameraData.ValueRW.ScreenPosLeftDown = CameraController.ScreenPosLeftDown;
                    global.CameraData.ValueRW.ScreenPosRightTop = CameraController.ScreenPosRightTop;
                    global.CameraData.ValueRW.ScreenPosLeftTop = CameraController.ScreenPosLeftTop;
                    global.CameraData.ValueRW.ScreenPosRightDown = CameraController.ScreenPosRightDown;
                    */

                    //算rotate y（已知斜边和长直角边，求角度）
                    var leftTopMid = new float2(CameraController.ScreenPosLeftDown.x, CameraController.ScreenPosLeftTop.y);
                    //斜边长度
                    var hypotenuse = math.distance(CameraController.ScreenPosLeftDown, CameraController.ScreenPosLeftTop);
                    //直角边长度1
                    var sideTop = math.distance(CameraController.ScreenPosLeftTop, leftTopMid);
                    //夹角角度（left）
                    var angleRadians = Mathf.Asin(sideTop / hypotenuse) * Mathf.Rad2Deg;
                    //global.ScreenRotateAngle = angleRadians;
                }
            }
        }

        public static void UpdateCamera(float3 lookAtPos, float deltaTime)
        {
            CameraController.LookAtPos = lookAtPos; 
            CameraController.OnUpdate(deltaTime);
        }
    }
}