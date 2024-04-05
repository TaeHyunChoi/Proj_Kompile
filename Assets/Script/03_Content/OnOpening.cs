using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class OnOpening : ContentBase
{
    private class PlayLogo : IUpdateRoutine, IGetInput
    {
        private Image imageLogo;
        private float wait;
        private float alpha;
        private int state;

        public int Update(int index)
        {
            if (state != index)
            {
                index = state;
            }

            switch (index)
            {
                case 0:
                    if (alpha < 1)
                    {
                        alpha += Time.unscaledDeltaTime;
                        imageLogo.color = new Color(1f, 1f, 1f, alpha);
                        return index;
                    }
                    alpha = 1f;
                    break;
                case 1:
                    if (wait < 1f)
                    {
                        wait += Time.unscaledDeltaTime;
                        return index;
                    }
                    break;
                case 2:
                    if (alpha > 0)
                    {
                        alpha -= Time.unscaledDeltaTime * 2f;
                        imageLogo.color = new Color(1f, 1f, 1f, alpha);
                        return index;
                    }
                    break;
                default:
                    Main.Instance.SetInputGetter(null);
                    instance.MoveNext();
                    return -1;
            }
            return state = index + 1;
        }

        public void Input(int input)
        {
            //TODO: compare input (any action key)

            if (0 == state)
            {
                alpha = 1f;
                imageLogo.color = new Color(1f, 1f, 1f, alpha);
                state = 2;
            }
            // In other cases, input doesn`t processed.
        }

        public PlayLogo(Transform transform)
        {
            transform.gameObject.SetActive(true);
            imageLogo = transform.GetComponent<Image>();
            imageLogo.color = new Color(1f, 1f, 1f, 0f);
            alpha = 0;
            wait = 0;
            state = 0;

            Main.Instance.SetInputGetter(this);
        }
    }

    private static OnOpening instance;
    private Transform transform;

    public static async Task<OnOpening> InitAsync(Transform canvas_ui)
    {
        GameObject go = await AssetManager.InstantiateAsync("OpeningGame", canvas_ui, true);
        OnOpening opening = new OnOpening(go.transform);
        return opening;
    }
    public OnOpening(Transform transform)
    {
        instance = this;
        this.transform = transform;
        state = 0;
    }

    public void MoveNext()
    {
        switch (state)
        {
            case 0:
                Main.Instance.SetContent(this);
                PlayLogo logo = new PlayLogo(transform.GetChild(0));
                CoroutineUpdater.Get.SetHandler(new CCoroutine<PlayLogo>(logo));
                break;
            default:
                state = -1;
                break;
        }

        state += 1;
    }
    public override void Dispose()
    {
        GameObject obj = transform.gameObject;
        GameObject.Destroy(obj);
        AssetManager.ReleaseAsset(obj.GetInstanceID());
    }
}
