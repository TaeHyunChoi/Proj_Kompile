// namespace Script.GameSystem
// {
//     using Script.Asset;
//     using UnityEngine;
//     using UnityEngine.UI;
//
//     public class UILoadingCurtain : IngameMonoBehaviourBase
//     {
//         private Image image;
//         private float delta;
//         private float alpha;
//
//         public override PrefabID PrefabID => PrefabID.UI_LoadingCurtain;
//
//         private void Awake()
//         {
//             image = transform.GetComponent<Image>();
//         }
//         public async Awaitable SetLoadingCurtain(bool on)
//         {
//             if (on)
//             {
//                 alpha = 0f;
//                 delta = 1f;
//             }
//             else
//             {
//                 alpha = 1f;
//                 delta = -1f;
//             }
//
//
//             do
//             {
//                 // idea: 이걸 sin 함수로 구현할 순 없나?
//                 alpha = System.Math.Clamp(alpha + delta * Time.deltaTime, 0f, 1f);
//                 image.color = new Color(0f, 0f, 0f, alpha);
//                 await Awaitable.NextFrameAsync();
//             }
//             while (0f < alpha && alpha < 1f);
//         }
//     }
// }