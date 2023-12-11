using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private Transform transform;

    public UIManager(Transform transform)
    {
        this.transform = transform;
    }

    public Transform GetTransform()
    {
        return transform;
    }
    
    //public Canvas GetCanvas(int index)
    //{
    //    if (index == 0)
    //    {
    //        return canvas_overlay;
    //    }

    //    return canvas_camera;
    //}
    //public async void Set(ContentType type)
    //{
    //    switch (type)
    //    {
    //        case ContentType.Title:
    //            //Asset.Instantiate() + Asset.Release() 방식
    //            //await AssetManager.InstantiateUI(type, canvas_overlay.transform);
    //            break;
    //        case ContentType.Field:
    //        case ContentType.Battle:
    //            //최초에 생성하고 .SetActive(isOn) 방식
    //            //=> 그냥 처음에 UI 관련 에셋 전부 생성해버리는게 좋겠는데? 어차피 비동기니께...?
    //            break;
    //    }

    //}
}