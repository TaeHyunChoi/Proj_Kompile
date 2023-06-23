using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public partial class Unit : MonoBehaviour //Battle
{
    public  Dictionary<int, List<SkillData>> Skill { get => skill; }
    private Dictionary<int, List<SkillData>> skill = new Dictionary<int, List<SkillData>>();

    public  int LastSelect { get => lastSelect; }
    private int lastSelect;

    public  byte Mode { get => Mode; }
    private byte mode;

    public  float Priority { get => priority; }
    private float priority;

    public  bool IsFaint { get => isFaint; }
    private bool isFaint;

    public void Battle_SetStatus()
    {
        //모드에 따라 스탯 가중치 달라진다.

        Battle_SetSpeed();
    }
    public void Battle_SetSpeed()
    {
        float isLukcy = Status[IDxUNIT.LUK];
        float rnd = Random.Range(0, 10000); //이러면 불러올 때마다 값이 바뀌는구나 흠

        if (rnd == 0)
            isLukcy = 0.5f;
        else if (rnd <= isLukcy)
            isLukcy = 2;
        else
            isLukcy = 1;

        //테스트용
        if (Data.Group == IDxUNIT.ENEMY)
            isLukcy *= Random.Range(0.9f, 1.1f);

        priority = Status[IDxUNIT.AGI] * isLukcy;
    }

    public void ProcBattle_AI()
    {
        //## Select Skill
        int idxGroup = IDxSkill.BASIC; //임의 설정
        int idxSkill = Random.Range(0, skill[idxGroup].Count);

        //## Select Target
        BattleAI_SelectTarget(out int flagTarget); //out으로 꺼내는게 결과값 형태 보기에 더 좋은 듯

        //## Update Last Select
        lastSelect = 0;
        lastSelect = (flagTarget << BIT.SHIFT_TARGET) | (idxGroup << BIT.SHIFT_MENU) | idxSkill;

        //## Play Action
        coPlayer.InitAttack();
    }

    private void BattleAI_SelectTarget(out int flag)
    {
        //임의로 랜덤으로 경우의 수 돌렸다.
        int rnd = Random.Range(0, 3);
        if (rnd == 0)
        {
            flag = GetTargetFlag_HPHighest(target: ETargetGroup.Party);
        }
        else
        {
            flag = GetTargetFlag_HPLowest(target: ETargetGroup.Party);
        }
    }
    private int GetTargetFlag_HPHighest(ETargetGroup target)
    {
        int flag = 0;
        int min = (target == ETargetGroup.Enemy) ? 3 : 0;
        int max = (target == ETargetGroup.Enemy) ? 6 : 2;

        int saved = min;
        Unit uComp, uCurrent;
        for (int i = min + 1; i <= max; ++i)
        {
            uComp = UnitMgr.Battle_GetUnit(i);
            if (uComp == null || uComp.isFaint)
            {
                continue;
            }

            uCurrent = UnitMgr.Battle_GetUnit(saved);
            if (uCurrent.status[IDxUNIT.HP] < uComp.status[IDxUNIT.HP])
            {
                saved = i;
            }
        }

        flag |= (1 << saved);
        return flag;
    }
    private int GetTargetFlag_HPLowest(ETargetGroup target)
    {
        int flag = 0;
        int min = (target == ETargetGroup.Enemy) ? 3 : 0;
        int max = (target == ETargetGroup.Enemy) ? 6 : 2;

        int saved = min;
        Unit uComp, uCurrent;
        for (int i = min + 1; i <= max; ++i)
        {
            uComp = UnitMgr.Battle_GetUnit(i);
            if (uComp == null || uComp.isFaint)
            {
                continue;
            }

            uCurrent = UnitMgr.Battle_GetUnit(saved);
            if (uCurrent.status[IDxUNIT.HP] > uComp.status[IDxUNIT.HP])
            {
                saved = i;
            }
        }

        flag |= (1 << saved);
        return flag;
    }


    public void OnAnimeSkill_HitTarget()
    {
        int idxGroup = (lastSelect & BIT.MASK_NOW_MENU) >> BIT.SHIFT_MENU;
        int idxSkill = (lastSelect & BIT.MASK_NOW_CONTENT);
        int flagTarget = (lastSelect >> BIT.SHIFT_TARGET);

        for (int i = 0; i < 7; ++i)
        {
            if ((flagTarget >> i) == 1)
            {
                Unit target = UnitMgr.Battle_GetUnit(i);
                SkillData skill = UnitMgr.Battle_GetSkill(i, idxGroup, idxSkill);
                target.coPlayer.InitHit(target, skill);
            }
        }
    }
    public int CalcDamage(Unit hitter, SkillData hitSkill)
    {
        return hitSkill.Power + (hitter.status[IDxUNIT.DEX] >> 2) - status[IDxUNIT.CON];
    }
}