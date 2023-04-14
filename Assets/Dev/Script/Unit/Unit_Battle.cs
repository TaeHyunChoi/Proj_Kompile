using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Unit : MonoBehaviour
{
    public Dictionary<int, List<SkillData>> Skill { get => skill; }
    private Dictionary<int, List<SkillData>> skill = new Dictionary<int, List<SkillData>>();

    //Battle: 전투 중 마지막 선택
    public int LastSelect { get => lastSelect; }
    private int lastSelect;

    public byte Mode { get => Mode; }
    private byte mode;

    public float Priority { get => priority; }
    private float priority;

    private GameObject targetingArrow;

    private List<Unit> targets = new List<Unit>();
    private SkillData selectSkill;


    public void Battle_SetStatus()
    {
        //모드에 따라 스탯 가중치 달라진다.

        Battle_SetSpeed();
    }
    public void Battle_SetSpeed()
    {
        float isLukcy = Status[IDxUNIT.LUK];
        float rnd = UnityEngine.Random.Range(0, 10000); //이러면 불러올 때마다 값이 바뀌는구나 흠

        if (rnd == 0)
            isLukcy = 0.5f;
        else if (rnd <= isLukcy)
            isLukcy = 2;
        else
            isLukcy = 1;

        //테스트용
        if (Data.Group == IDxUNIT.ENEMY)
            isLukcy *= UnityEngine.Random.Range(0.9f, 1.1f);

        priority = Status[IDxUNIT.AGI] * isLukcy;
    }
    public void Battle_BeTargeted(bool betargeted)
    {
        targetingArrow.SetActive(betargeted);
        //쉐이더 반짝도 건드리고 싶긴 해~
    }
    public void Battle_SaveLastAction(int act)
    {
        lastSelect = act;
    }
    public void Battle_AI()
    {
        //얘는 뭘 어찌해야 할까?


        Debug.Log($"ENM[{Data.Name}] {Priority:F2} => Next");
        GameMgr.Battle_NextTurn();
    }


    public void Battle_PlayAction(SkillData skill, int group, int solo)
    {
        StartCoroutine(IEBattle_PlayAction(skill, group, solo));
    }
    private IEnumerator IEBattle_PlayAction(SkillData skill, int group, int solo)
    {
        //입력값: 스킬 저장
        selectSkill = skill;

        //입력값: 타겟 저장
        targets.Clear();
        switch (group)
        {
            case IDxUNIT.TARGET_ENM_SOLO:
            case IDxUNIT.TARGET_PLY_SOLO:
                group = (group == IDxUNIT.TARGET_ENM_SOLO) ? IDxUNIT.ENEMY : IDxUNIT.PLAYER;
                targets.Add(UnitMgr.Battle_GetUnit(group, solo));
                break;
            case IDxUNIT.TARGET_ENM_ALL:
            case IDxUNIT.TARGET_PLY_ALL:
                group = (group == IDxUNIT.TARGET_ENM_ALL) ? IDxUNIT.ENEMY : IDxUNIT.PLAYER;
                targets.AddRange(UnitMgr.Battle_GetUnitGroup(group));
                break;
            case IDxUNIT.TARGET_SELF:
                targets.Add(this);
                break;
            case IDxUNIT.TARGET_XOR_SELF:
                targets.AddRange(UnitMgr.Battle_GetUnitGroup(IDxUNIT.TARGET_ENM_ALL));
                targets.AddRange(UnitMgr.Battle_GetUnitGroup(IDxUNIT.TARGET_PLY_ALL));
                targets.Remove(this);
                break;
        }

        //잠시 버퍼 후 애니메이션 재생
        float wait = Time.time + 0.25f;
        while (wait > Time.time)
            yield return null;

        string code;
        PlayAnime(code = IDxUNIT.ANIME_SKILL);
        wait = Time.time + aoc[code].length;
        while (!IsAnimeEnd(code, wait))
            yield return null;


        PlayAnime(code = IDxUNIT.ANIME_IDLE);
        wait = Time.time + aoc[code].length + 0.25f;
        while (!IsAnimeEnd(code, wait))
            yield return null;

        GameMgr.Battle_NextTurn();
        yield break;
    }


    public void OnAnime_Hit()
    {
        for (int i = 0; i < targets.Count; ++i)
            targets[i].ProcHit(this, selectSkill);
    }
    public void ProcHit(Unit hitter, SkillData hitSkill)
    {
        ushort dmg = CalcDamage(hitter, hitSkill);
        Debug.Log($"{dmg:F2}");
        StartCoroutine(IEBattle_Hit(hitSkill, dmg));
    }
    private IEnumerator IEBattle_Hit(SkillData hitSkill, ushort dmg)
    {
        status[IDxUNIT.HP] -= dmg;

        PlayAnime(IDxUNIT.ANIME_HIT);
        float wait = Time.time + aoc[IDxUNIT.ANIME_HIT].length;
        while (!IsAnimeEnd(IDxUNIT.ANIME_HIT, wait))
        {
            //좌우로 흔들어
            yield return null;
        }

        Debug.Log($"[{UnitMgr.GetUnitByIndex(hitSkill.ActorIndex).data.Name}] Attack [{this.data.Name}] by {hitSkill.Name}");
        PlayAnime(IDxUNIT.ANIME_IDLE);
    }


    private ushort CalcDamage(Unit hitter, SkillData hitSkill)
    {
        return (ushort)(hitSkill.Power + (hitter.status[IDxUNIT.DEX] >> 2) - status[IDxUNIT.CON]);
    }
}
