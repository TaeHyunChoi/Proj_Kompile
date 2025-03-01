using Script.Manager;
using System;
using UnityEngine;
using UnityEngine.UI;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Interface;
using Script.Content;
using Script.Data;

public class UI_TitleMenuObject : MonoBehaviour, IIngameUpdater, IIngameInput
{
    private enum State
    { 
        NONE,
        UPDATE,
        WAIT,
        CLOSE
    }
    private enum MenuType
    { 
        NEW_GAME = 0,
        LOAD_GAME,
        OPTION,
        EXIT
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
        IngameManager.AddInput(AssetCode.UI_TitleMenuObject, this);
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
                IngameManager.RemoveInput(AssetCode.UI_TitleMenuObject);
                MessageManager.Publish(MessageType.END_OBJECT_PROCESS, new OnEndProcess(AssetCode.UI_TitleMenuObject));
                return UpdaterState.SUCCESS;
        }
        return UpdaterState.RUNNING;
    }

    public void Input(InputFlag inputFlag)
    {
        if (true == inputFlag.Contains(InputFlag.ENTER | InputFlag.ACTION))
        {
            switch ((MenuType)index)
            {
                case MenuType.NEW_GAME: // new game
                    new Ingame_EnterField(0);
                    // IngameManager.AddTask(TaskType.OP_START_GAME, TaskUpdateType.UPDATE);
                    // eneter field 만들고
                    // ENTER_FIELD 호출되면 '데이터 해제' 쪽을 정리해야 함. ㄱㄷㄱㄷ..
                    break;
                case MenuType.LOAD_GAME:
                    break;
                case MenuType.OPTION:
                    break;
                case MenuType.EXIT:
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
