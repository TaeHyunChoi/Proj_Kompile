using System.Threading.Tasks;
using UnityEngine;

public partial class Main_ // Main_Request
{
    /* UI, Canvas */
    public Canvas RequestUI_GetCanvasCamera()
    {
        return mUIMgr.CanvasCamera;
    }
    public Canvas RequestUI_GetCanvasOverlay()
    {
        return mUIMgr.CanvasOverlay;
    }

    /* Asset */
    public async Task<GameObject> RequestAssetAysnc_GetPrefab(EAsset type, Transform parent)
    {
        string code = AssetMgr.GetAddress(type);
        return await AssetMgr.InstantiateGameObjectAsync(code, parent, true);
    }
}
