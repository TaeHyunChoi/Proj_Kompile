using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCoroutine : MonoBehaviour
{
    private Unit owner;
    private Animator animator;

    private delegate void SetCoroutine(int type);
    private SetCoroutine SetNext;
    private delegate bool MoveNextCouroutine(int type);
    private MoveNextCouroutine MoveNext;

    private int curType;
    private int curState;
    private float endTime;

    public void Init(int type)
    {
        owner = transform.GetComponent<Unit>();
        animator = transform.GetComponent<Animator>();

        SetNext = Set;
        MoveNext = Move;

        curType = type;
        curState = 0;
        enabled = true;
    }


    private void Set(int type)
    {
        switch (type)
        {
            case 0: SetNext_Battle(); break;
        }
    }
    private bool Move(int type)
    {
        switch (type)
        {
            case 0: return MoveNext_Battle();
        }
        return false;
    }
    private void Clear()
    {
        owner = null;
        animator = null;
        SetNext = null;
        MoveNext = null;
    }


    private void SetNext_Battle()
    {
        switch (curState)
        {
            case 0:
                endTime = Time.time + 0.1f;
                break;
            case 1:
                owner.PlayAnime(IDxUNIT.ANIME_SKILL);
                break;
            case 2:
                owner.PlayAnime(IDxUNIT.ANIME_IDLE);
                if (owner != UnitMgr.MyPC)
                    break;

                InputMgr.SetMode(IDxINPUT.BASE);
                UnitMgr.Battle_SlowUnitAnime(false, 1.1f);
                break;
            case 3:
                endTime = Time.time + 0.5f;
                break;
            default:
                GameMgr.Battle_NextTurn();
                enabled = false;
                Clear();
                return;
        }
    }
    private bool MoveNext_Battle()
    {
        switch (curState)
        {
            case 0:
                return endTime <= Time.time;
            case 1:
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(IDxUNIT.ANIME_SKILL))
                    return false;

                return state.normalizedTime >= 1;
            case 2:
                return UIMgr.UpdateUI_BattleCombo(false, 1.1f);
            case 3:
                return endTime <= Time.time;
        }

        return false;
    }


    private void Update()
    {
        if (!MoveNext(curType))
            return;

        ++curState;
        SetNext(curType);
    }
}