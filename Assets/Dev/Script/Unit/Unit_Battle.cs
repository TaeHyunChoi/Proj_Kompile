using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
        //여기서부터 설계+구현 들어가야 하는군

        //스킬 선정
        int skillGroup = IDxSkill.BASIC; //임의 설정
        int max = skill[skillGroup].Count;
        selectSkill = skill[skillGroup][Random.Range(0, max)];

        //타겟 지정 : Mode에 따라
        targets = Battle_AITargeting();
        Battle_PlayAction(selectSkill, targets);
    }
    private List<Unit> Battle_AITargeting()
    {
        List<Unit> result = new List<Unit>();
        switch (selectSkill.TargetGroup)
        {
            case IDxUNIT.TARGET_PLY_SOLO :
                {
                    //예시 모드는 보통(Rnd), 선제(One), 방어(XOR)로 걸어본다.
                    List<Unit> groups = UnitMgr.Battle_GetUnitGroup(IDxUNIT.PLAYER);
                    switch (mode)
                    {
                        case IDxUNIT.MODE_NORMAL:
                        case IDxUNIT.MODE_DEFENCE:
                            result.Add(groups[Random.Range(0, groups.Count)]);
                            break;
                        case IDxUNIT.MODE_PREEMTIVE:
                            result.Add(groups[(lastSelect & 0x0000_F000) >> 4 * 3]);
                            break;
                    }
                }
                break;
        }

        return result;
    }

    public void Battle_PlayAction(SkillData skill, List<Unit> targets)
    {
        selectSkill = skill;
        StartCoroutine(IEBattle_PlayAction(targets));
    }
    private IEnumerator IEBattle_PlayAction(List<Unit> targets)
    {
        this.targets = targets;

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
        int dmg = CalcDamage(hitter, hitSkill);
        StartCoroutine(IEBattle_Hit(hitSkill, dmg));
    }
    private IEnumerator IEBattle_Hit(SkillData hitSkill, int dmg)
    {
        status[IDxUNIT.HP] -= dmg;

        PlayAnime(IDxUNIT.ANIME_HIT);
        float wait = Time.time + aoc[IDxUNIT.ANIME_HIT].length;
        while (!IsAnimeEnd(IDxUNIT.ANIME_HIT, wait))
        {
            //좌우로 흔들어
            yield return null;
        }

        //Debug.Log($"[{UnitMgr.GetUnitByIndex(hitSkill.ActorCode).data.Name}] Attack [{this.data.Name}] by {hitSkill.Name}");
        PlayAnime(IDxUNIT.ANIME_IDLE);
    }


    private int CalcDamage(Unit hitter, SkillData hitSkill)
    {
        return hitSkill.Power + (hitter.status[IDxUNIT.DEX] >> 2) - status[IDxUNIT.CON];
    }
}
