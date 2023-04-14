using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Unit : MonoBehaviour
{
    public UnitData Data { get => data; }
    private UnitData data;

    private Animator animator;
    private AnimatorOverrideController aoc;
    public Dictionary<int, List<SkillData>> Skill { get => skill; }
    private Dictionary<int, List<SkillData>> skill = new Dictionary<int, List<SkillData>>();

    public ushort[] Stat { get => stat; }
    private ushort[] stat;

    public Vector3 Pos { get => transform.position; }
    public Vector3 LocalPos { get => transform.localPosition; }

    //Battle: 전투 중 마지막 선택
    public int LastSelect { get => lastSelect; }
    private int lastSelect;

    //Battle: 전투 모드
    public byte Mode { get => Mode; }
    private byte mode;

    //Battle: 전투 우선순위
    public float Priority { get => priority; }
    private float priority;
    
    //Battle: UI Arrow
    private GameObject targetingArrow;

    //Batte: Action
    private List<Unit> targets = new List<Unit>();
    private SkillData selectSkill;

    public void Init(int code)
    {
        //기본값 정보 저장
        data = DataMgr.UnitTBL.Find(unit => unit.Code == code);

        //스탯 초기화 (깊은 복사 사용)
        stat = new ushort[IDxUNIT.STAT_CNT];
        Array.Copy(Data.StatDefault, stat, IDxUNIT.STAT_CNT);

        //캐릭터 스킬, 공통 스킬
        List<SkillData> skill = DataMgr.SkillTBL.FindAll(skill => (skill.ActorIndex == code) || (skill.ActorIndex == IDxUNIT.COMMON));

        this.skill.Add(IDxSkill.BASIC, new List<SkillData>());
        this.skill.Add(IDxSkill.SOLO, new List<SkillData>());
        this.skill.Add(IDxSkill.GROUP, new List<SkillData>());
        this.skill.Add(IDxSkill.SPECIAL, new List<SkillData>());

        for(int i = 0; i < skill.Count; ++i)
            this.skill[skill[i].SkillGroup].Add(skill[i]);

        targetingArrow = transform.GetChild(0).gameObject;

        //애니메이션(AOC)
        animator = transform.GetComponent<Animator>();
        aoc = new AnimatorOverrideController(ResourceMgr.AOC);
        animator.runtimeAnimatorController = aoc;
        PlayAnime(IDxUNIT.ANIME_IDLE);
    }
    private void PlayAnime(string type, string code = null)
    {
        if (code == null)
            code = type;

        AnimationClip ac = ResourceMgr.Anime[Data.RcsCode + "/" + code];
        aoc[type] = ac;
        animator.CrossFade(type, 0f);
    }

    public void MoveTo(int mx, int mz)
    {
        transform.position += new Vector3(mx, 0, mz) * IDxUNIT.SPEED_MOVE * Time.deltaTime;
    }

    public void Battle_BeTargeted(bool betargeted)
    {
        targetingArrow.SetActive(betargeted);
        //쉐이더 반짝도 건드리고 싶긴 해~
    }
    public void Battle_SetStat()
    {
        //모드에 따라 스탯 가중치 달라진다.

        Battle_SetSpeed();
    }
    public void Battle_SetSpeed()
    {
        float isLukcy = Stat[IDxUNIT.LUK];
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

        priority = Stat[IDxUNIT.AGI] * isLukcy;
    }
    public void Battle_SaveLastAction(int act)
    {
        lastSelect = act;
    }
    public void Battle_AI()
    {
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
        float wait = 0.25f;
        while (wait > 0)
            yield return wait -= Time.deltaTime;


        PlayAnime(IDxUNIT.ANIME_SKILL);
        AnimatorStateInfo anime = animator.GetCurrentAnimatorStateInfo(0);
        while (!anime.IsName(IDxUNIT.ANIME_SKILL) || anime.normalizedTime < 1)
            yield return anime = animator.GetCurrentAnimatorStateInfo(0);

        //애니메이션 끝난 후 다시 IDLE로 복귀
        PlayAnime(IDxUNIT.ANIME_IDLE);
        GameMgr.Battle_NextTurn();
        yield break;
    }
    public void OnAnime_Hit()
    {
        for (int i = 0; i < targets.Count; ++i)
            targets[i].ProcHit(selectSkill);
    }
    public void ProcHit(SkillData hitSkill)
    {
        //PlayAnime(IDxUNIT.ANIME_HIT);
        Debug.Log($"[{UnitMgr.GetUnitByIndex(hitSkill.ActorIndex).data.Name}] Attack [{this.data.Name}] by {hitSkill.Name}");
    }
}