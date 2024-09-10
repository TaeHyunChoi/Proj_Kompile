using UnityEngine;
using UnityEngine.UI;

public class UIManager_
{
    public Canvas CanvasCamera { get; private set; }
    public Canvas CanvasOverlay { get; private set; }

    public UIManager_(Transform mainTransform)
    {
        Transform uiTransform = mainTransform.Find("UI");
        CanvasCamera  = uiTransform.GetChild(1).GetComponent<Canvas>();
        CanvasOverlay = uiTransform.GetChild(1).GetComponent<Canvas>();
    }
}
