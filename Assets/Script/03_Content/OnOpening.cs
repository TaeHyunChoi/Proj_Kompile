using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public partial class OnOpening : ContentBase
{
    private static OnOpening instance;
    private Transform transform;

    public static async Task<OnOpening> InitAsync(Transform canvas_camera)
    {
        GameObject go = await AssetMgr.InstantiateGameObjectAsync("OpeningGame", canvas_camera, true);
        OnOpening opening = new OnOpening(go.transform);
        return opening;
    }
    private OnOpening(Transform transform)
    {
        instance = this;
        this.transform = transform;
        state = 0;

        Image[] img = transform.GetComponentsInChildren<Image>();
        for (int i = 0; i < img.Length; ++i)
        {
            img[i].color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void Set()
    {
        switch (state)
        {
            case 0:
                Main.Instance.SetContent(this);
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
                Main.InputMgr.ReleaseUpdater();
                Main.UIMgr.Pop(UIType.Title, true);
                break;
            default:
                return;
        }

        state += 1;
    }
    public override void Dispose()
    {
        GameObject obj = transform.gameObject;
        GameObject.Destroy(obj);
        if (false == AssetMgr.ReleaseGameObject(obj.GetInstanceID()))
        {
            Debug.LogError($"Can`t Release Asset: {obj.name}({obj.GetInstanceID()})");
        }
    }
}
