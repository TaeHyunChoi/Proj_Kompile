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
        return await AssetMgr.InstantiateGameObjectAsync(code, parent, true);
    }
    public void Asset_ClearAll() => AssetMgr.ClearAll();

    /* Scene */
    public void MapScene_EnterField(int code) => mMapSceneMgr.EnterFieldScene(code);
}
