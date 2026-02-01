using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Dots
{
    public static class DamageNumberPool
    {
        private static Queue<ControllerDamageNumber> _caches = new();
        private static Queue<ControllerDamageElement> _cachedElements = new();
        private const int MaxCacheCount = 300;
        private const int MaxElementCount = 30;
        
        public static async UniTask InitPool()
        {
            DestroyAll();

            //cache damage number
            {
                var sourceObj = await GFight.LoadGameObject(Table.GetResourceDeploy((int)EResourceId.DamageNumber).Url);
            
                //cache 100个
                for (var i = 0; i < MaxCacheCount; i++)
                {
                    var gameObj = sourceObj.InstantiateGo();
                    gameObj.transform.position = new Vector3(10000, 0, 0);
                    var script = gameObj.AddComponent<ControllerDamageNumber>();
                    _caches.Enqueue(script);    
                }
            }
           
            //cache reaction
            {
                var sourceElement = await GFight.LoadGameObject(Table.GetResourceDeploy((int)EResourceId.DamageElement).Url);
                for (var i = 0; i < MaxElementCount; i++)
                {
                    var gameObj = sourceElement.InstantiateGo();
                    gameObj.transform.position = new Vector3(10000, 0, 0);
                    var script = gameObj.GetComponent<ControllerDamageElement>();
                    _cachedElements.Enqueue(script);    
                }
            }
         
        }

        public static ControllerDamageElement GetElement()
        {
            if (_cachedElements.Count > 0)
            {
                return _cachedElements.Dequeue();
            }
            return null;
        }

        public static ControllerDamageNumber Get()
        {
            if (_caches.Count > 0)
            {
                return _caches.Dequeue();
            }
            return null;
        }

        public static void RecycleElement(ControllerDamageElement obj)
        {
            if (obj == null)
            {
                return;
            }
            if (_cachedElements.Count > MaxCacheCount)
            {
                obj.OnRecycle();
                Object.Destroy(obj.gameObject);
            }
            else
            {
                obj.transform.position = new Vector3(10000, 0, 0);
                obj.OnRecycle();
                _cachedElements.Enqueue(obj);    
            }
        }

        public static void Recycle(ControllerDamageNumber obj)
        {
            if (obj == null)
            {
                return;
            }
            if (_caches.Count > MaxCacheCount)
            {
                obj.OnRecycle();
                Object.Destroy(obj.gameObject);
            }
            else
            {
                obj.transform.position = new Vector3(10000, 0, 0);
                obj.OnRecycle();
                _caches.Enqueue(obj);    
            }
        }

        public static void DestroyAll()
        {
            foreach (var queue in _caches)
            {
                if (queue != null && queue.gameObject != null)
                {
                    Object.Destroy(queue.gameObject);
                }
            }
            _caches.Clear();
            
            foreach (var queue in _cachedElements)
            {
                if (queue != null && queue.gameObject != null)
                {
                    Object.Destroy(queue.gameObject);
                }
            }
            _cachedElements.Clear();
        }
    }
}