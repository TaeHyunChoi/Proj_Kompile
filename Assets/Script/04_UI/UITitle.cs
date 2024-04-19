using UnityEngine;
using UnityEngine.UI;
using static Index.IDxInput;

public class UITitle : UIBase, IInputHandler
{
    private Image[] items;

    private int select;
    private int itemCount;
    private float delta;
    private float alphaMax = 0.6f, alphaMin = 0.3f;

    private void Awake()
    {
        Image[] images = transform.GetChild(0).GetComponentsInChildren<Image>(true);
        items = new Image[images.Length - 1];
        itemCount = images.Length - 1;
        for (int i = 1; i < images.Length; ++i)
        {
            items[i - 1] = images[i];
        }

        select = 0;
    }
    private void Start()
    {
        Main.InputMgr.SetUpdater(this);
    }
    public override void Pop(bool isOn)
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(isOn);
    }

    private void Update()
    {
        if (items[select].color.a <= alphaMin)
        {
            delta = Time.deltaTime;
        }
        else if (items[select].color.a >= alphaMax)
        {
            delta = -Time.deltaTime;
        }

        items[select].color += new Color(0, 0, 0, delta * 0.75f);
    }
    public void Input(int input)
    {
        if (Compare(input, UP))
        {
            SetItemColor(select, 0f); //prev
            select = (select - 1 + itemCount) % itemCount;

            SetItemColor(select, alphaMin); //next
        }
        else if (Compare(input, DOWN))
        {
            SetItemColor(select, 0f);
            select = (select + 1 + itemCount) % itemCount;
            SetItemColor(select, alphaMin);
        }
        else if (Compare(input, ENTER, ACTION))
        {
            SetItemColor(select, alphaMax);
            enabled = false;

            switch (select)
            {
                case 0:
                    Debug.Log("New game For Test (map code: 100)");
                    Main.SceneMgr.LoadSceneAsync(GameState.Field, 100);
                    break;
                case 1:
                    Debug.Log("Saved Data List");
                    break;
                case 2:
                    Debug.Log("Option window");
                    break;
                case 3:
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    break;
            }

        }
        else if (Compare(input, CANCEL))
        {
            if (!enabled)
            {
                enabled = true;
            }
        }
    }
    private void SetItemColor(int index, float alpha)
    {
        Color target = items[index].color;
        items[index].color = new Color(target.r, target.g, target.b, alpha);
    }

    public override void Dispose()
    {
        GameObject.Destroy(gameObject);
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }
}
