using UnityEngine;
using UnityEngine.UI;
using static Index.IDxInput;

public class UITitle : UIBase, IInputHandler
{
    private static readonly float ALPHA_MAX = 0.6f;
    private static readonly float ALPHA_MIN = 0.3f;

    private Image[] mSelectionItems;
    private int   mSelect;
    private int   mItemCount;
    private float mDeltaTime;
    private float mOffsetTime;

    private void Awake()
    {
        Image[] images = transform.GetChild(0).GetComponentsInChildren<Image>(true);
        mSelectionItems = new Image[images.Length - 1];
        mItemCount = images.Length - 1;
        for (int i = 1; i < images.Length; ++i)
        {
            mSelectionItems[i - 1] = images[i];
        }

        mSelect = 0;
        mOffsetTime = 0f;
    }
    private void Start()
    {
        Main.InputMgr.Updater = this;
    }
    public override void Pop(bool isOn)
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(isOn);
    }

    private void Update()
    {
        if (mSelectionItems[mSelect].color.a <= ALPHA_MIN)
        {
            mDeltaTime = Time.deltaTime;
        }
        else if (mSelectionItems[mSelect].color.a >= ALPHA_MAX)
        {
            mDeltaTime = -Time.deltaTime;
        }

        mSelectionItems[mSelect].color += new Color(0, 0, 0, mDeltaTime * 0.75f);
    }
    public void Input(EInput input)
    {
        if(true == input.Contains(EInput.ENTER | EInput.ACTION))
        {
            SetItemColor(mSelect, ALPHA_MAX);
            enabled = false;

            switch (mSelect)
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

        if(true == input.Contains(EInput.CANCEL))
        {
            if (!enabled)
            {
                enabled = true;
            }
        }

        if (Time.time < mOffsetTime)
        {
            return;
        }
        mOffsetTime = Time.time + Time.fixedDeltaTime * 10f;

        if(true == input.Contains(EInput.UP))
        {
            SetItemColor(mSelect, 0f); //prev
            mSelect = (mSelect - 1 + mItemCount) % mItemCount;

            SetItemColor(mSelect, ALPHA_MIN); //next
        }

        if (true == input.Contains(EInput.DOWN))
        {
            SetItemColor(mSelect, 0f);
            mSelect = (mSelect + 1 + mItemCount) % mItemCount;
            SetItemColor(mSelect, ALPHA_MIN);
        }
    }
    private void SetItemColor(int index, float alpha)
    {
        Color target = mSelectionItems[index].color;
        mSelectionItems[index].color = new Color(target.r, target.g, target.b, alpha);
    }

    public override void Release()
    {
        GameObject.Destroy(gameObject);
        AssetMgr.ReleaseGameObject(gameObject.GetInstanceID());
    }
}
