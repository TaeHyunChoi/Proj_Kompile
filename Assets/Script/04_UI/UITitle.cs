using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static IDxInput;

public class UITitle : UIBase
{
    private Image[]   items;

    private int select;
    private int itemCount;
    private float delta;
    private float alphaMax = 0.7f, alphaMin = 0.3f;
    private bool enabled;

    public override void Init(GameObject go)
    {
        this.transform = go.transform;
        this.gameObject = go;

        Image[] images = transform.GetChild(0).GetComponentsInChildren<Image>(true);
        items = new Image[images.Length - 1];
        itemCount = images.Length - 1;
        for (int i = 1; i < images.Length; ++i)
        {
            items[i - 1] = images[i];
        }
        select = 0;
    }
    public override void Open()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        enabled = true;
    }
    public override void Input(int input)
    {
        // using static IDxInput;
        if (Compare(input, UP))
        {
            SetItemColor(select, 0f);
            select = (--select < 0) ? itemCount - 1 : select;
            SetItemColor(select, alphaMin);
        }
        else if (Compare(input, DOWN))
        {
            SetItemColor(select, 0f);
            select = (++select >= itemCount) ? 0 : select;
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
                    MapData map = DataTable.MapTable.Find(x => x.Code == 100);
                    Main.LevelMgr.LoadSceneAsync(ContentType.Field, map);
                    break;
                case 1:
                    //게임 저장 UI 호출
                    Debug.Log("Saved Data List");
                    break;
                case 2:
                    //옵션창 호출
                    Debug.Log("Option window");
                    break;
                case 3:
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }

        }
        else if (Compare(input, CANCEL))
        {
            //메뉴가 비활성화 == 무언가를 Enter한 상태
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
    public override void Update()
    {
        if (!enabled)
        {
            return;
        }

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
    public override void Close()
    {
        GameObject.Destroy(this.gameObject);
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }
}
