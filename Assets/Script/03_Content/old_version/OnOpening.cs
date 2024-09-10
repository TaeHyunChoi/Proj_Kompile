using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public partial class OnOpening 
{
    private static OnOpening instance;
    private Transform transform;
    private byte mState;

    public static async Task<OnOpening> InitAsync(Transform canvas_camera)
    {
        GameObject go = await AssetMgr.InstantiatePrefabAsync(EAsset.OpeningGame, canvas_camera, true);
        OnOpening opening = new OnOpening(go.transform);
        return opening;
    }
    public OnOpening(Transform transform)
    {
        instance = this;
        this.transform = transform;
        mState = 0;

        Image[] img = transform.GetComponentsInChildren<Image>();
        for (int i = 0; i < img.Length; ++i)
        {
            img[i].color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void Set()
    {
        switch (mState)
        {
            case 0:
                OpeningLogo logo = new OpeningLogo(transform.GetChild(0));
                CoroutineUpdater.SetHandler(new CCoroutine<OpeningLogo>(logo));
                break;
            case 1:
                OpeningDemo demo = new OpeningDemo(transform.GetChild(1));
                CoroutineUpdater.SetHandler(new CCoroutine<OpeningDemo>(demo));
                break;
            case 2:
                OpeningTitle title = new OpeningTitle(transform.GetChild(2));
                CoroutineUpdater.SetHandler(new CCoroutine<OpeningTitle>(title));
                break;
            case 3:
                Main.UIMgr.Pop(EUIType.Title, true);
                break;
            default:
                return;
        }

        mState += 1;
    }
}
