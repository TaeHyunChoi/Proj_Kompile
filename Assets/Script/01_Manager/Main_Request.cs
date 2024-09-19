using IECoroutine;
using System.Threading.Tasks;
using UnityEngine;

public partial class Main_ // Main_Request
{
    /* Input */
    public static Index.IDxInput.EInput Input_Get()
    {
        return Instance.mInputReserved;
    }


    /* UI, Canvas */
    public Canvas UI_GetCanvasCamera() => mUIMgr.CanvasCamera;
    public Canvas UI_GetCanvasOverlay() => mUIMgr.CanvasOverlay;
    public void UI_AddNew(EUIType type, GameObject obj) => mUIMgr.AddNew(type, obj);
    public void UI_Open(EUIType type) => mUIMgr.Open(type);
    public void UI_Close(EUIType type) => mUIMgr.Close(type);
    public void UI_Clear(EUIGroup groupType) => mUIMgr.Clear(groupType);


    /* Asset */
    public async Task<GameObject> Asset_InstantiateAysnc(EAsset type, Transform parent)
    {
        string code = AssetMgr.GetAddress(type);

        // 게임 오브젝트 생성 기다리고
        GameObject obj = await AssetMgr.InstantiateGameObjectAsync(code, parent, true);

        // 에셋 타입에 따라 Manager 급에 cache 한다면?
        switch (type)
        {
            case EAsset.OpeningGame: mMapSceneMgr.AddNewObject(obj.GetInstanceID());  break;
            case EAsset.UITitle:     mUIMgr.AddNew(EUIType.Title, obj);               break;
        }

        return obj;
    }
    public void Asset_ClearAll() => AssetMgr.ClearAll();

    /* Scene */
    public void MapScene_EnterField(int code) => mMapSceneMgr.EnterFieldScene(code);
    public void MapScene_Clear() => mMapSceneMgr.ClearObjects();
}
