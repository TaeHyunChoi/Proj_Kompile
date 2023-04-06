using System;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitData Data { get; private set; }
    public ushort[] Stat { get; private set; }
    public List<SkillData> Skill { get; private set; }


    public Vector3 Pos { get => transform.position; }
    public Vector3 LocalPos { get => transform.localPosition; }


    public byte Mode { get; private set; }
    public float BattleSpeed { get; private set; }
    public delegate void BattleAction();
    public BattleAction Battle;

    private Animator animator;
    private AnimatorOverrideController aoc;

    public void Init(int code)
    {
        //기본값 정보 저장
        Data = DataMgr.UnitTBL.Find(unit => unit.Code == code);

        //스탯 초기화 (깊은 복사 사용)
        Stat = new ushort[IDxUNIT.STAT_CNT];
        Array.Copy(Data.StatDefault, Stat, IDxUNIT.STAT_CNT);

        //캐릭터 스킬, 공통 스킬
        List<SkillData> skill = DataMgr.SkillTBL.FindAll(skill => (skill.ActorIndex == code) || (skill.ActorIndex == IDxUNIT.COMMON));
        Skill = skill;

        //Battle
        if (Data.Group == IDxUNIT.PLAYER)
            Battle = new BattleAction(UIOpen);
        else if (Data.Group == IDxUNIT.ENEMY)
            Battle = new BattleAction(BattleAI);

        //애니메이션(AOC)
        animator = transform.GetComponent<Animator>();
        aoc = new AnimatorOverrideController(ResourceMgr.AOC);
        animator.runtimeAnimatorController = aoc; //여기가 계속 문제네?
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

    public void SetBattleStat()
    {
        //모드에 따라 스탯 가중치 달라진다.

        SetBattleSpeed();
    }
    public void SetBattleSpeed()
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

        BattleSpeed = Stat[IDxUNIT.AGI] * isLukcy;
    }

    private void UIOpen()
    {
        Debug.Log($"PLY[{Data.Name}] {BattleSpeed:F2}");
        UIMgr.Show(IDxUI.BATTLE, true);
        //GameMgr.Battle_NextTurn();
    }
    private void BattleAI()
    {
        Debug.Log($"ENM[{Data.Name}] {BattleSpeed:F2}");
        GameMgr.Battle_NextTurn();
    }


    public void MoveTo(int mx, int mz)
    {
        transform.position += new Vector3(mx,0,mz) * IDxUNIT.SPEED_MOVE * Time.deltaTime;
    }
}
