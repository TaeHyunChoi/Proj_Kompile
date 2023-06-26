using UnityEngine;

public partial class Unit : MonoBehaviour //Battle
{
    public void Battle_SetStatus()
    {
        //## Mode => Status Weighted
        //추후 개발

        //## Set Speed
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
    public int CalcDamage(Unit hitter, SkillData hitSkill)
    {
        return hitSkill.Power + (hitter.status[IDxUNIT.DEX] >> 2) - status[IDxUNIT.CON];
    }

    //## Battle AI
    public void ProcBattle_AI()
    {
        //## Select Skill
        int idxGroup = IDxSkill.BASIC; //임의 설정
        int idxSkill = Random.Range(0, skill[idxGroup].Length);


        //## Select Target
        int flagTarget;
        int rnd = Random.Range(0, 3);
        if (rnd == 0)
        {
            flagTarget = GetTargetFlag_HPHighest(target: ETargetGroup.Party);
        }
        else
        {
            flagTarget = GetTargetFlag_HPLowest(target: ETargetGroup.Party);
        }


        //## Update Last Select
        lastSelect = 0;
        lastSelect = (flagTarget << BIT.SHIFT_TARGET) | (idxGroup << BIT.SHIFT_MENU) | idxSkill;


        //## Play Action
        coPlayer.InitAttack();
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
            uComp = UnitMgr.InBattle[i];
            if (uComp == null || uComp.isFaint)
            {
                continue;
            }

            uCurrent = UnitMgr.InBattle[saved];
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
            uComp = UnitMgr.InBattle[i];
            if (uComp == null || uComp.isFaint)
            {
                continue;
            }

            uCurrent = UnitMgr.InBattle[saved];
            if (uCurrent.status[IDxUNIT.HP] > uComp.status[IDxUNIT.HP])
            {
                saved = i;
            }
        }

        flag |= (1 << saved);
        return flag;
    }
}