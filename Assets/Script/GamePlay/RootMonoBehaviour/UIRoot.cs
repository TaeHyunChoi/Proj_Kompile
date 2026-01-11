namespace Script.GamePlay
{
    using UnityEngine;

    public partial class Main
    {
        /// <summary> UI 생성 시, 부모 캔버스 설정을 위함. 이외의 기능은 상정하지 않음 </summary>
        public class UICanvas : MonoBehaviour
        {
            public readonly Transform CameraCanvas;
            public readonly Transform OverlayCanvas;
            public readonly Transform CurtainCanvas;

            public UICanvas(Transform cameraCanvas, Transform overlayCanvas, Transform curtainCanvas)
            {
                CameraCanvas  = cameraCanvas;
                OverlayCanvas = overlayCanvas;
                CurtainCanvas = curtainCanvas;
            }
        }        
    }
}