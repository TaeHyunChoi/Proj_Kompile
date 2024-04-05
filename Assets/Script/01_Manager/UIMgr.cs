using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMgr
{
    private Canvas canvasOverlay;
    private Canvas canvasCamera;

    public Canvas OverlayCanvas { get => canvasOverlay; }
    public Canvas CameraCanvas  { get => canvasCamera; }

    public UIMgr(Transform transform)
    {
        canvasOverlay = transform.GetChild(0).GetComponent<Canvas>();
        canvasCamera = transform.GetChild(1).GetComponent<Canvas>();
    }
}
