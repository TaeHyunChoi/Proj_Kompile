using System.Threading.Tasks;
using UnityEngine;

public partial class Main_ // Main_Request
{
    /* UI, Canvas */
    public Canvas GetUICanvasCamera()
    {
        return mUIMgr.CanvasCamera;
    }
    public Canvas GetUICanvasOverlay()
    {
        return mUIMgr.CanvasOverlay;
    }

    /* Asset */
    public async Task<GameObject> InstantiateAssetAysnc(EAsset type, Transform parent)
    {
        string code = AssetMgr.GetAddress(type);
        return await AssetMgr.InstantiateGameObjectAsync(code, parent, true);
    }
}
