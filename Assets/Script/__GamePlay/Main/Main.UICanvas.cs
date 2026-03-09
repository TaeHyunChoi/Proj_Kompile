namespace Script.GamePlay
{
    using Script.Asset;
    using Script.GameSystem;
    using Script.Asset.Provider;
    using System.Collections.Generic;
    using UnityEngine;

    public partial class Main
    {
        /// <summary> UI 생성 시, 부모 캔버스 설정을 위함. 이외의 기능은 상정하지 않음 </summary>
        public class UISystem : MonoBehaviour
        {
            // Canvas Transform Root
            private readonly Transform camRoot;
            private readonly Transform gameplayRoot;
            private readonly Transform popupRoot;
            private readonly Transform systemRoot;
            private readonly Transform loadingRoot;

            public Transform CamRoot      => camRoot;
            public Transform GamePlayRoot => gameplayRoot;
            public Transform PopupRoot    => popupRoot;
            public Transform SystemRoot   => systemRoot;
            public Transform LoadingRoot  => loadingRoot;

            public UISystem(Transform root)
            {
                camRoot         = root.GetChild(0);
                gameplayRoot    = root.GetChild(1);
                popupRoot       = root.GetChild(2);
                systemRoot      = root.GetChild(3);
                loadingRoot     = root.GetChild(4);
            }

            private Dictionary<PrefabID, GameObject> cachedUIs = new Dictionary<PrefabID, GameObject>();

            public async Awaitable<GameObject> ShowUI(PrefabID id, Transform root)
            {
                // 이미 갖고 있으면 활성화
                if (true == cachedUIs.TryGetValue(id, out GameObject instance))
                {
                    instance.SetActive(true);
                    instance.transform.SetAsLastSibling(); // 맨 앞으로 배치
                    return instance;
                }
                // 인스턴스를 찾을 수 없다면 캐싱에서 삭제
                else
                {
                    cachedUIs.Remove(id);
                }

                // 인스턴스가 없으니 생성
                //instance = await AssetProvider.GetOrNewInstanceAsync(id, root);
                //if (null != instance)
                //{
                //    cachedUIs[id] = instance;
                //}

                return instance;
            }

            public void HideUI(PrefabID id)
            {
                if (true == cachedUIs.TryGetValue(id, out var instance))
                {
                    if (null != instance)
                    {
                        instance.SetActive(false);
                        // 여기서 AssetSystem.ReleaseInstance를 호출하지 않습니다!
                        // 호출하면 AssetSystem이 가져가서 비활성화하고 풀에 넣어버리거나 파괴합니다.
                        // UIManager가 "내가 계속 쓸 거야"라고 쥐고 있는 상태입니다.
                    }
                }
            }

            /// <summary>
            /// 완전히 메모리에서 제거하고 싶을 때 (씬 이동 등)
            /// </summary>
            public void ReleaseUniqueUI(PrefabID id)
            {
                //if (cachedUIs.TryGetValue(id, out var uiInstance))
                //{
                //    if (uiInstance != null)
                //    {
                //        AssetProvider.ReleaseInstance(id, uiInstance);
                //    }

                //    cachedUIs.Remove(id);
                //}
            }

            public async Awaitable SetLoadingCurtain(bool on)
            {
                var obj = await ShowUI(PrefabID.UI_LoadingCurtain, loadingRoot);
                var loading = obj.GetComponent<UILoadingCurtain>();
                await loading.SetLoadingCurtain(on);
            }
        }        
    }
}