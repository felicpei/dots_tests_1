using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Dots;
using UnityEngine;
using Object = UnityEngine.Object;

public class GFight : MonoBehaviour
{
    public static string PathMissionCanvas = "UI/Battle3D/MissionCanvas.prefab";
    public static string PathPlayerHud = "UI/Battle3D/PlayerHud.prefab";
    public static string PathServantBubble = "UI/Battle3D/ServantBubble.prefab";
    
    public static GameObject Canvas;

    //calc fps
    private int frameCount;
    private float deltaTime;
    private const float UpdateInterval = 1.0f; // 更新间隔（秒）
    public static float fps;
    
    private async void Awake()
    {
        //DotsEventCenter.Init();
        UIDataTransfer.InFight = true;
        Canvas = await LoadGameObject(PathMissionCanvas);
        await DamageNumberPool.InitPool();
    }

    private void Update()
    {
        frameCount++;
        deltaTime += Time.deltaTime;

        if (deltaTime > UpdateInterval)
        {
            fps = frameCount / deltaTime; // 计算平均帧率
            frameCount = 0; // 重置帧数
            deltaTime = 0.0f; // 重置时间
        }
    }

    private void OnDestroy()
    {
        fps = 0;
        UIDataTransfer.InFight = false;
        //DotsEventCenter.Dispose();

        if (Canvas != null)
        {
            Destroy(Canvas);
            Canvas = null;
        }
        //recycle pool
        DamageNumberPool.DestroyAll();
        ReleaseObjCache();
    }
    
    
    public static Dictionary<string, Object> m_addressableObjCache = new();

    public static void ReleaseObjCache()
    {
        foreach (var (k, v) in m_addressableObjCache)
        {
            XResource.ReleaseAsset(k);
        }
        m_addressableObjCache.Clear();
    }

    public static async UniTask<GameObject> LoadGameObject(string url)
    {
        if (m_addressableObjCache.TryGetValue(url, out var cacheObj))
        {
            return cacheObj.Instantiate();
        }

        // 用 TaskCompletionSource 包裝 coroutine
        var obj = await XResource.LoadAssetAsync<Object>(url);
        m_addressableObjCache[url] = obj;
        return obj.Instantiate();
    }
    
}