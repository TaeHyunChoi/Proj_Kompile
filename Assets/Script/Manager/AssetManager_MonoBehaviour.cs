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
        private static UI_LoadingCurtainObject loadingCurtain;
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
        public static UI_LoadingCurtainObject GetLoadingCurtain()
        {
            return loadingCurtain;
        }

        public static void Initialize(Transform mainTransform)
        {
            canvasParents = new Transform[2];
            Transform uiParent = mainTransform.GetChild(1);
            canvasParents[0] = uiParent.GetChild(0);
            canvasParents[1] = uiParent.GetChild(1);
            loadingCurtain = uiParent.GetChild(2).GetChild(0).GetComponentInChildren<UI_LoadingCurtainObject>();

            unitParent = mainTransform.GetChild(2);
        }
    }
}

