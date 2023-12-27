using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static IDxInput;

public class UITitle : UIBase
{
    private Image[] item;
    private int select;
    private int itemCount;
    private float delta, alphaMax = 0.7f, alphaMin = 0.3f;

    private void Awake()
    {
        Image[] images = transform.GetChild(0).GetComponentsInChildren<Image>(true);
        item = new Image[images.Length - 1];
        itemCount = images.Length - 1;
        for (int i = 1; i < images.Length; ++i)
        {
            item[i - 1] = images[i];
        }
         select = 0;
    }
    public override void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Main.GetInputManager().SetUIInput(UIType.Title);
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
        else if (Compare(input, ENTER) || Compare(input, ACTION) || Compare(input, RIGHT))
        {
            SetItemColor(select, alphaMax);
            //call func
            switch (select)
            {
                case 0:
                    {
                        Debug.Log("In game");
                    }
                    break;
                case 1:
                    {
                        //게임 저장 UI 호출
                        Debug.Log("Saved Data List");
                    }
                    break;
                case 2:
                    {
                        //옵션창 호출
                        Debug.Log("Option window");
                    }
                    break;
                case 3:
                    {
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
                        EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    }
                    break;
            }
            enabled = false;
        }
        else if (Compare(input, CANCEL) || Compare(input, LEFT))
        {
            if (!enabled)  //메뉴가 비활성화 == 무언가를 Enter한 상태
            {
                enabled = true;
            }
        }
    }
    private void SetItemColor(int index, float alpha)
    {
        Color target = item[index].color;
        item[index].color = new Color(target.r, target.g, target.b, alpha);
    }
    private void Update()
    {
        if (item[select].color.a <= alphaMin)
        {
            delta = Time.deltaTime;
        }
        else if (item[select].color.a >= alphaMax)
        {
            delta = -Time.deltaTime;
        }

        item[select].color += new Color(0, 0, 0, delta * 0.75f);
    }

     public override void Close()
    {
        Main.ReturnContentInput(); //원래 콘텐츠로 입력 복귀...?
        //이거 입력 단을 다시 한 번만 생각해보자.
    }
}
