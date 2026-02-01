using Unity.Entities;
using UnityEngine;

namespace Dots
{
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial struct GlobalFactoryNoBurstSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var global = SystemAPI.GetAspect<GlobalAspect>(SystemAPI.GetSingletonEntity<GlobalInitialized>());

            //播放音效
            for (var i = global.PlaySoundBuffer.Length - 1; i >= 0; i--)
            {
                var soundId = global.PlaySoundBuffer[i].SoundId;
                var isStop = global.PlaySoundBuffer[i].IsStop;
                var isLoop = global.PlaySoundBuffer[i].IsLoop;
                global.PlaySoundBuffer.RemoveAt(i);

                if (soundId > 0)
                {
                    if (isLoop)
                    {
                        Sound.PlayLoop(soundId);
                    }
                    else if (isStop)
                    {
                        Sound.StopLoop(soundId);
                    }
                    else
                    {
                        Sound.PlayAudioUI(soundId);
                    }
                }
            }

            //播放摄像机震动
            for (var i = global.PlayCameraShakeBuffer.Length - 1; i >= 0; i--)
            {
                var info = global.PlayCameraShakeBuffer[i];
                global.PlayCameraShakeBuffer.RemoveAt(i);

                if (info.Radius > 0 && info.Time > 0)
                {
                    CameraController.ShakeCamera(info.Time, info.Radius, info.Pos);
                }
            }
            
            //手柄、手机震动
            for (var i = global.ControllerShakeBuffer.Length - 1; i >= 0; i--)
            {
                var info = global.ControllerShakeBuffer[i];
                global.ControllerShakeBuffer.RemoveAt(i);
                PhoneVibrateHelper.Vibrate(info.ShakeType);
            }
        }
    }
}