namespace Script.Manager
{
    using UnityEngine;
    using UnityEngine.UI;
    using Script.Index;

    /// <summary>
    /// Scene에 저장된 오브젝트 저장
    /// </summary>
    public static partial class AssetManager // MonoBehaviour
    {
        private static Transform[]  canvasParents;
        private static CanvasGroup  loadingCurtain;
        private static Transform    unitParent;

        public static Transform GetCanvas(CanvasType type)
        {
            switch (type)
            {
                case CanvasType.CAMERA:  return canvasParents[0];
                case CanvasType.OVERLAY: return canvasParents[1];
                default: return null;
            }
        }

        public static void Initialize(Transform mainTransform)
        {
            canvasParents = new Transform[2];
            Transform uiParent = mainTransform.GetChild(1);
            canvasParents[0] = uiParent.GetChild(0);
            canvasParents[1] = uiParent.GetChild(1);
            loadingCurtain = uiParent.GetChild(2).GetComponentInChildren<CanvasGroup>();

            unitParent = mainTransform.GetChild(2);
        }
    }
}

