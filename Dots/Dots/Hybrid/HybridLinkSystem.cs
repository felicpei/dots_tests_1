using Deploys;
using Lobby;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using System.Threading.Tasks;

namespace Dots
{
    [UpdateInGroup(typeof(GlobalSystemGroup))]
    public partial class HybridLinkSystem : SystemBase
    {
        private ComponentLookup<LocalToWorld> _localToWorldLookup;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<GlobalInitialized>();
            _localToWorldLookup = GetComponentLookup<LocalToWorld>(true);
        }

        protected override void OnUpdate()
        {
            // 同步部分使用 Temp ECB
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var globalEntity = SystemAPI.GetSingletonEntity<GlobalInitialized>();
            var global = SystemAPI.GetAspect<GlobalAspect>(globalEntity);
            
            _localToWorldLookup.Update(this);

            foreach (var (tag, entity) in SystemAPI.Query<HybridLinkTag>().WithEntityAccess())
            {
                // 1. 立即禁用標籤，防止重入
                ecb.SetComponentEnabled<HybridLinkTag>(entity, false);

                switch (tag.Type)
                {
                    case EHybridType.DamageNumber:
                        CreateDamageNumber(tag, entity, ecb);
                        break;
                    case EHybridType.DamageElement:
                        CreateDamageElement(tag, entity, ecb);
                        break;
                    default:
                        // 2. 觸發異步加載
                        CreateDefaultAsync(global.MonsterBrightness, tag, entity);
                        break;
                }
            }

            // 確保先結束同步循環並應用禁用 Tag 的操作
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private static void CreateDamageNumber(HybridLinkTag tag, Entity entity, EntityCommandBuffer ecb)
        {
            var number = DamageNumberPool.Get();
            if (number != null)
            {
                number.Init(tag.Tile, new Color(tag.Color.x, tag.Color.y, tag.Color.z, tag.Color.w));
                ecb.AddComponent(entity, new HybridDamageNumberController { Value = number });
            }
        }

        private static void CreateDamageElement(HybridLinkTag tag, Entity entity, EntityCommandBuffer ecb)
        {
            var element = DamageNumberPool.GetElement();
            if (element != null)
            {
                element.Init((EElementReaction)tag.Id, new Color(tag.Color.x, tag.Color.y, tag.Color.z, tag.Color.w));
                ecb.AddComponent(entity, new HybridDamageElementController { Value = element });
            }
        }

        private static async void CreateDefaultAsync(float brightness, HybridLinkTag tag, Entity entity)
        {
            // 【關鍵】強制跳出當前的 System 疊代棧，防止 Structural Change 錯誤
            await Task.Yield();

            var resDp = Table.GetResourceDeploy(tag.ResId);
            if (resDp == null)
            {
                Debug.LogError($"link hybrid error, resId:{tag.ResId}");
                return;
            }

            // 執行加載
            var gameObj = await GFight.LoadGameObject(resDp.Url);
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.EntityManager.Exists(entity))
            {
                if (gameObj != null) Object.Destroy(gameObj);
                return;
            }

            var em = world.EntityManager;

            // 異步完成後，我們直接操作 EntityManager (因為已經不在 foreach 循環中了)
            // 如果你擔心性能或競態，也可以在這裡創建一個臨時 ECB 並手動 Playback
            switch (tag.Type)
            {
                case EHybridType.LocalPlayer:
                    var pCtrl = gameObj.AddComponentIfNotExists<ControllerLocalPlayer>();
                    em.AddComponentData(entity, new HybridPlayerController { Value = pCtrl });
                    break;

                case EHybridType.Monster:
                    var mCtrl = gameObj.AddComponentIfNotExists<ControllerMonster>();
                    mCtrl.Init(tag.Id);
                    mCtrl.InitShader(tag.CreatureType, brightness);
                    em.AddComponentData(entity, new HybridMonsterController { Value = mCtrl });
                    break;

                case EHybridType.Servant:
                    await SetupServant(entity, gameObj, tag, em);
                    break;

                case EHybridType.ProgressBar:
                    var barCtrl = gameObj.AddComponent<ControllerProgressBar>();
                    em.AddComponentData(entity, new HybridProgressBarController { Value = barCtrl });
                    break;

                case EHybridType.DropItem:
                    if (tag.Id == 1)
                    {
                        var iCtrl = gameObj.AddComponent<ControllerDropItem>();
                        em.AddComponentData(entity, new HybridDropItemController { Value = iCtrl });
                    }
                    break;
            }

            // 最後綁定 Transform
            var prefabScale = gameObj.transform.localScale;
            em.AddComponentData(entity, new HybridTransform 
            { 
                Value = gameObj.transform, 
                PrefabScale = prefabScale 
            });
        }

        private static async Task SetupServant(Entity entity, GameObject gameObj, HybridLinkTag tag, EntityManager em)
        {
            var servantCtrl = gameObj.AddComponentIfNotExists<ControllerServant>();
            servantCtrl.Init(tag.Id, tag.Rarity);
            
            var hybridServant = new HybridServantController { Value = servantCtrl };
            
            // HUD
            var hudObj = await GFight.LoadGameObject(GFight.PathPlayerHud);
            if (hudObj != null)
            {
                hudObj.transform.SetParent(GFight.Canvas.transform, false);
                var hud = hudObj.GetComponent<ServantHud>();
                hud.Init(gameObj.transform, tag.Id, tag.IsMainServant);
                hybridServant.Hud = hud;
            }

            // Bubble
            var bubbleObj = await GFight.LoadGameObject(GFight.PathServantBubble);
            if (bubbleObj != null)
            {
                bubbleObj.transform.SetParent(GFight.Canvas.transform, false);
                var bubble = bubbleObj.GetComponent<ServantBubble>();
                bubble.Init(gameObj.transform, tag.Id);
                hybridServant.Bubble = bubble;
            }

            if (em.Exists(entity))
                em.AddComponentData(entity, hybridServant);
            else
                Object.Destroy(gameObj);
        }
    }
}