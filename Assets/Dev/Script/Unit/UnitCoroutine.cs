using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCoroutine : MonoBehaviour
{
    private Unit owner;
    private Animator animator;

    private delegate void SetNextCoroutine(int type);
    private SetNextCoroutine SetNext;
    private delegate bool MoveNextCouroutine(int type);
    private MoveNextCouroutine MoveNext;

    private int curType;
    private float endTime;

    private float fValue;

    private Dictionary<int, int> state;

    private void InitBase(int type)
    {
        enabled = false;

        owner = transform.GetComponent<Unit>();
        animator = transform.GetComponent<Animator>();

        curType = type;
        state = new Dictionary<int, int>();
        state.Add(curType, 0);

        SetNext = Set;
        SetNext(curType);

        MoveNext = Move;
    }
    private void Set(int type)
    {
        switch (type)
        {
            case 0: SetNext_Attack(); break;
            case 1: SetNext_Hit(); break;
        }
    }
    private bool Move(int type)
    {
        switch (type)
        {
            case 0: return MoveNext_Attack();
            case 1: return MoveNext_Hit();
        }

        return false;
    }
    private void Clear()
    {
        owner = null;
        animator = null;
        SetNext = null;
        MoveNext = null;
        state = null;
    }


    public void InitAttack()
    {
        InitBase(type: 0);
        enabled = true;
    }
    private void SetNext_Attack()
    {
        switch (state[curType])
        {
            case 0:
                endTime = Time.time + 0.1f;
                break;
            case 1:
                owner.Anime_Play(IDxUNIT.ANIME_SKILL);
                break;
            case 2:
                owner.Anime_Play(IDxUNIT.ANIME_IDLE);
                if (owner != UnitMgr.MyPC)
                    break;
                
                UnitMgr.Anime_PlaySlow(slow: false, 1.1f);
                break;
            case 3:
                endTime = Time.time + 0.5f;
                break;
            default:
                UnitMgr.Select_SetNextUnit();
                enabled = false;
                Clear();
                return;
        }
    }
    private bool MoveNext_Attack()
    {
        switch (state[curType])
        {
            case 0:
                return endTime <= Time.time;
            case 1:
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(IDxUNIT.ANIME_SKILL))
                    return false;

                return state.normalizedTime >= 1;
            case 2:
                return UIMgr.Battle_UpdateUICombo(false, 1.1f);
            case 3:
                return endTime <= Time.time;
        }

        return false;
    }

    public void InitHit(Unit hitter, SkillData hitSkill)
    {
        InitBase(type: 1);
        fValue = owner.Status_CalcDamage(hitter, hitSkill);
        enabled = true;
    }
    private void SetNext_Hit()
    {
        switch (state[curType])
        {
            case 0:
                owner.Status[IDxUNIT.HP] -= (int)fValue;
                owner.Anime_Play(IDxUNIT.ANIME_HIT);
                endTime = Time.time + owner.Anime_GetLength(IDxUNIT.ANIME_HIT);
                break;
            default:
                owner.Anime_Play(IDxUNIT.ANIME_IDLE);
                enabled = false;
                Clear();
                return;
        }
    }
    private bool MoveNext_Hit()
    {
        switch (state[curType])
        {
            case 0:
                return endTime <= Time.time;
        }

        return false;
    }


    private void Update()
    {
        if (!MoveNext(curType))
            return;

        ++state[curType];
        //++curState;
        SetNext(curType);
    }
}