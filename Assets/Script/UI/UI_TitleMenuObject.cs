using Script.Manager;
using System;
using UnityEngine;
using UnityEngine.UI;
using Script.Index;
using static Script.Index.IDxInput;

public class UI_TitleMenuObject : MonoBehaviour
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

    public bool OnSelect_Move(EInputFlag inputUpDown)
    {
        if (Time.time < lastInputTime + waitTime)
        {
            return false;
        }
        lastInputTime = Time.time;

        if (true == inputUpDown.Contains(EInputFlag.UP | EInputFlag.UP_HOLD))
        {
            index = ((index - 1) + 4) % 4;
        }
        if (true == inputUpDown.Contains(EInputFlag.DOWN | EInputFlag.DOWN_HOLD))
        {
            index = ((index + 1) + 4) % 4;
        }

        selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
        return true;
    }
    public bool OnSelect_Enter()
    {
        switch (index)
        {
            case 0: // new game
                IngameManager.AddTask(TaskType.OP_START_GAME, TaskUpdateType.UPDATE);
                break;
            case 1:  // load game
                break;
            case 2:  // option
                break;
            case 3:  // exit
                break;
            default: // error
                return false;
        }

        return true;
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
}
