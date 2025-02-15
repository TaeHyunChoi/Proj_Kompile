using Script.Manager;
using System;
using UnityEngine;
using UnityEngine.UI;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Interface;

public class UI_TitleMenuObject : MonoBehaviour, IIngameUpdater
{
    [SerializeField] private Transform menuParent;
    [SerializeField] private Image selectSlotImage;

    private readonly float minAlpha   = 0.3f;
    private readonly float maxAlpha   = 0.7f;
    private readonly float alphaDelta = 0.5f;

    private Vector2[] anchoredPositions;
    private float alpha;
    private float sign;

    private readonly float waitTime = 0.125f;
    private float lastInputTime;
    private int   index;

    public void OnSelect_Move(EInputFlag inputUpDown)
    {
        if (Time.time < lastInputTime + waitTime)
        {
            return;
        }
        lastInputTime = Time.time;

        if (true == inputUpDown.Contains(EInputFlag.UP))
        {
            index = ((index - 1) + 4) % 4;
        }
        if (true == inputUpDown.Contains(EInputFlag.DOWN))
        {
            index = ((index + 1) + 4) % 4;
        }

        selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
    }
    public int OnSelect_Enter()
    {
        switch (index)
        {
            case 0: // new game
                //IngameManager.AddTask(TaskType.OP_START_GAME, TaskUpdateType.UPDATE);
                break;
            case 1:  // load game
                break;
            case 2:  // option
                break;
            case 3:  // exit
                break;
            default: // error
                break;
        }


        return index;
    }

    private void Awake()
    {
        anchoredPositions = new Vector2[menuParent.childCount];
        for (int i = 0; i < anchoredPositions.Length; ++i)
        {
            anchoredPositions[i] = menuParent.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
        }
        menuParent = null;

        alpha = minAlpha;
        sign  = 1f;

        index = 0;
        lastInputTime = -waitTime; // 즉각 입력하려고
    }

    // 얘네도 그냥 ITask로 처리하는게 차라리 좋았으려나?
    // 규칙에서 벗어난 느낌쓰~!!
    private void Update()
    {
        alpha += sign * Time.deltaTime * alphaDelta;

        if (alpha >= maxAlpha)
        {
            alpha = maxAlpha;
            sign  = -1f;
        }
        else if (alpha <= minAlpha)
        {
            alpha = minAlpha;
            sign  = 1f;
        }
    }
    private void LateUpdate()
    {
        selectSlotImage.color = new Color(0.2232704f, 0.5052339f, 1f, alpha);
    }

    // 여러 개 붙이는 경우도 생기는구나? 아이고 흐음..
    // 그러면 TryAddTask를 2개 붙여야 하나? 그러면.. 아.. 음.. 흠...
    public UpdaterState UpdateState()
    {
        return UpdaterState.RUNNING;
    }
}
