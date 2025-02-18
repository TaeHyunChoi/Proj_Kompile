using Script.Manager;
using System;
using UnityEngine;
using UnityEngine.UI;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Interface;

public class UI_TitleMenuObject : MonoBehaviour, IIngameUpdater, IIngameInput
{
    private enum State
    { 
        NONE,
        UPDATE,
        WAIT,
        CLOSE
    }

    [SerializeField] private Transform menuParent;
    [SerializeField] private Image selectSlotImage;

    private readonly float minAlpha   = 0.3f;
    private readonly float maxAlpha   = 0.7f;
    private readonly float alphaDelta = 0.5f;
    private readonly float waitTime   = 0.125f;

    private Vector2[] anchoredPositions;
    private float alpha;
    private float sign;

    private float lastInputTime;
    private int   index;

    private State state;

    private void Awake()
    {
        anchoredPositions = new Vector2[menuParent.childCount];
        for (int i = 0; i < anchoredPositions.Length; ++i)
        {
            anchoredPositions[i] = menuParent.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
        }
        menuParent = null; // 사용을 마침

        alpha = minAlpha;
        sign  = 1f;

        index = 0;
        lastInputTime = 0;

        state = State.NONE;

        IngameManager.AddUpdater(this);
        IngameManager.AddInput(AssetIndex.UI_TitleMenuObject, this);
    }

    public UpdaterState UpdateState()
    {
        switch (state)
        {
            case State.NONE:
                state = State.UPDATE;
                break;
            case State.UPDATE:
                alpha += sign * Time.deltaTime * alphaDelta;

                if (alpha >= maxAlpha)
                {
                    alpha = maxAlpha;
                    sign = -1f;
                }
                else if (alpha <= minAlpha)
                {
                    alpha = minAlpha;
                    sign = 1f;
                }

                selectSlotImage.color = new Color(0.2232704f, 0.5052339f, 1f, alpha);
                break;
            case State.WAIT:
                //작동 일시중지
                break;
            case State.CLOSE:
                IngameManager.RemoveInput(AssetIndex.UI_TitleMenuObject);
                MessageManager.Publish(new Message_t(MessageType.END_OBJECT_PROCESS, AssetIndex.UI_TitleMenuObject));
                return UpdaterState.SUCCESS;
        }
        return UpdaterState.RUNNING;
    }

    public void Input(InputFlag inputFlag)
    {
        if (true == inputFlag.Contains(InputFlag.ENTER | InputFlag.ACTION))
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
                    // state 유지
                    return;
            }

            return;
        }

        if (Time.time < lastInputTime + waitTime)
        {
            return;
        }
        lastInputTime = Time.time;

        if (true == inputFlag.Contains(InputFlag.UP))
        {
            index = ((index - 1) + 4) % 4;
            selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
        }
        if (true == inputFlag.Contains(InputFlag.DOWN))
        {
            index = ((index + 1) + 4) % 4;
            selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
        }
    }
}
