using Deploys;
using Unity.Burst;
using Unity.Collections;

namespace Dots
{
    public struct DamageNumberQueue 
    {
        public float Value;
        public EElement Element;
        public EDamageNumber Type;
        public EAgainstType Against;
        public EElementReaction Reaction;
    }
    
    public static class EventCenter
    {
        // 1. 定义一个唯一的 Key (随便起个名字，空类即可)
        // 这个 Key 只是为了让 Burst 区分不同的静态变量
        private class DamageQueueKey {}

        // 2. 使用 SharedStatic<T> 替代普通的 static T
        public static readonly SharedStatic<NativeQueue<DamageNumberQueue>> DamageQueueRef = 
            SharedStatic<NativeQueue<DamageNumberQueue>>.GetOrCreate<DamageQueueKey>();

        // 3. 初始化
        public static void Init()
        {
            // 注意：要通过 .Data 属性来赋值
            DamageQueueRef.Data = new NativeQueue<DamageNumberQueue>(Allocator.Persistent);
        }

        // 4. 销毁
        public static void Dispose()
        {
            if (DamageQueueRef.Data.IsCreated)
            {
                DamageQueueRef.Data.Dispose();
            }
        }
    }
}