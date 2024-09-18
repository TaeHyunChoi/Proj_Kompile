using UnityEngine;

public class UIComponent
{
    private EUIGroup   mhGroup;
    private EUIType    mhType;
    private GameObject mhGameObject;

    public EUIGroup UIGroup => mhGroup;
    public EUIType  UIType => mhType;

    public UIComponent(EUIType type, GameObject obj)
    {
        mhType = type;
        switch (type)
        {
            case EUIType.Title: mhGroup = EUIGroup.Title;   break;
            default:            mhGroup = EUIGroup.None;    break;
        }

        mhGameObject = obj;
    }

    public void Open()
    {
        mhGameObject.SetActive(true);
        mhGameObject.transform.SetAsLastSibling();
    }
    public void Close()
    {
        mhGameObject.SetActive(false);
    }
}
