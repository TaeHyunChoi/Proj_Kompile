using UnityEngine;
using UnityEngine.UI;

public class UIManager_
{
    public Canvas CanvasCamera { get; private set; }
    public Canvas CanvasOverlay { get; private set; }

    public UIManager_(Transform transfrom)
    {
        CanvasCamera  = transfrom.Find("CanvasCamera").GetComponent<Canvas>();
        CanvasOverlay = transfrom.Find("CanvasOverlay").GetComponent<Canvas>();
    }
}
