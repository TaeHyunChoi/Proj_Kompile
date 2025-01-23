using UnityEngine;

public struct UIComponent
{
    private GameObject mhGameObject;
    private EUIGroup   mhGroup;
    private int        mhInstanceID;

    public EUIGroup UIGroup => mhGroup;
    public int InstanceID => mhInstanceID;

    public UIComponent(EUIType type, GameObject obj)
    {
        //mhType = type;
        switch (type)
        {
            case EUIType.Title: mhGroup = EUIGroup.Title;   break;
            default:            mhGroup = EUIGroup.None;    break;
        }

        mhGameObject = obj;
        mhInstanceID = obj.GetInstanceID();
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
