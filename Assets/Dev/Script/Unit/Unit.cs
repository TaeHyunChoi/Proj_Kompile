using System;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitData Data { get => data; }
    private UnitData data;

    public Dictionary<int, List<SkillData>> Skill { get => skill; }
    private Dictionary<int, List<SkillData>> skill = new Dictionary<int, List<SkillData>>();

    public ushort[] Stat { get => stat; }
    private ushort[] stat;

    public Vector3 Pos { get => transform.position; }
    public Vector3 LocalPos { get => transform.localPosition; }


    public int LastAction { get => lastAction; }
    private int lastAction;

    public byte Mode { get => Mode; }
    private byte mode;

    public float Priority { get => priority; }
    private float priority;

    public delegate void BattleAction();
    public BattleAction Battle;
    private GameObject targetingArrow;

    private Animator animator;
    private AnimatorOverrideController aoc;

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

        //Battle
        if (Data.Group == IDxUNIT.PLAYER)
            Battle = new BattleAction(UIOpen);
        else if (Data.Group == IDxUNIT.ENEMY)
            Battle = new BattleAction(BattleAI);

        targetingArrow = transform.GetChild(0).gameObject;

        //애니메이션(AOC)
        animator = transform.GetComponent<Animator>();
        aoc = new AnimatorOverrideController(ResourceMgr.AOC);
        animator.runtimeAnimatorController = aoc;
        PlayAnime(IDxUNIT.IDLE);
    }

    public void PlayAnime(string type, string code = null)
    {
        if (code == null)
            code = type;

        AnimationClip ac = ResourceMgr.Anime[Data.RcsCode + "/" + code];
        aoc[type] = ac;
        animator.CrossFade(type, 0f);
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
        lastAction = act;
    }

    private void UIOpen()
    {
        Debug.Log($"PLY[{Data.Name}] {Priority:F2}");
        UIMgr.Show(IDxUI.BATTLE, true);
        //GameMgr.Battle_NextTurn();
    }
    private void BattleAI()
    {
        Debug.Log($"ENM[{Data.Name}] {Priority:F2}");
        GameMgr.Battle_NextTurn();
    }

    public void MoveTo(int mx, int mz)
    {
        transform.position += new Vector3(mx, 0, mz) * IDxUNIT.SPEED_MOVE * Time.deltaTime;
    }

    public void BeTargeted(bool betargeted)
    {
        targetingArrow.SetActive(betargeted);
        //쉐이더 반짝도 건드리고 싶긴 해~
    }
}