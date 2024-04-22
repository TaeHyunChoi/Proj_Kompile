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
    private float offsetTime;

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
        offsetTime = 0f;
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
    public void Input(EInput input)
    {
        if (Compare(input, EInput.ENTER, EInput.ACTION))
        {
            SetItemColor(select, alphaMax);
            enabled = false;

            switch (select)
            {
                case 0:
                    Debug.Log("New game For Test (map code: 100)");
                    Main.SceneMgr.LoadSceneAsync(EGameStateFlag.Field, 100);
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
        if (Compare(input, EInput.CANCEL))
        {
            if (!enabled)
            {
                enabled = true;
            }
        }

        if (Time.time < offsetTime)
        {
            return;
        }
        offsetTime = Time.time + Time.fixedDeltaTime * 10f;

        if (Compare(input, EInput.UP))
        {
            SetItemColor(select, 0f); //prev
            select = (select - 1 + itemCount) % itemCount;

            SetItemColor(select, alphaMin); //next
        }
        if (Compare(input, EInput.DOWN))
        {
            SetItemColor(select, 0f);
            select = (select + 1 + itemCount) % itemCount;
            SetItemColor(select, alphaMin);
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
        AssetMgr.ReleaseGameObject(gameObject.GetInstanceID());
    }
}
