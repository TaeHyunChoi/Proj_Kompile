using System;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public enum BattleMode
    {
        Normal,     //보통
        Charge,     //돌격
        Defence,    //방어
        Preemptive, //선제
        Counter,    //반격
    }

    public UnitData Data { get; private set; }
    public ushort[] Stat { get; private set; }
    public List<SkillData> Skill { get; private set; }


    public Vector3 Pos { get => transform.position; }
    public Vector3 LocalPos { get => transform.localPosition; }


    public BattleMode Mode { get; private set; }
    public float BattleSpeed { get; private set; }

    private Animator animator;
    private AnimatorOverrideController aoc;

    public void Init(int unitIndex)
    {
        //기본값 정보 저장
        Data = DataMgr.UnitTBL.Find(unit => unit.Code == unitIndex);

        //스탯 초기화 (깊은 복사 사용)
        Stat = new ushort[(ushort)StatIndex.CNT];
        Array.Copy(Data.StatDefault, Stat, (ushort)StatIndex.CNT);

        //캐릭터 스킬
        List<SkillData> skill = DataMgr.SkillTBL.FindAll(skill => skill.ActorIndex == unitIndex);
        Skill = skill;

        //공통 스킬
        skill = DataMgr.SkillTBL.FindAll(skill => skill.ActorIndex == 255); //공통스킬
        for (int i = 0; i < skill.Count; ++i)
            Skill.Add(skill[i]);

        //애니메이션(AOC)
        aoc = new AnimatorOverrideController(ResourceMgr.AOC);
        animator = transform.GetComponent<Animator>();
        animator.runtimeAnimatorController = aoc;
        PlayAnime(AocCode.IDLE);
    }
    
    public void PlayAnime(string type, string code = null)
    {
        if (code == null)
            code = type;

        AnimationClip ac = ResourceMgr.Anime[Data.RcsCode + "/" + code];
        aoc[type] = ac;
        animator.CrossFade(type, 0f);
    }

    public void SetBattleSpeed()
    {
        float isLukcy = Stat[(ushort)StatIndex.LUK];
        float rnd = UnityEngine.Random.Range(0, 10000); //이러면 불러올 때마다 값이 바뀌는구나 흠

        if (rnd == 0)
            isLukcy = 0.5f;
        else if (rnd <= isLukcy)
            isLukcy = 2;
        else
            isLukcy = 1;

        //테스트용
        if (Data.Group == UnitMgr.GROUP_ENM)
            isLukcy *= UnityEngine.Random.Range(0.9f, 1.1f);

        BattleSpeed = Stat[(ushort)StatIndex.AGI] * isLukcy;
    }


    public void MoveTo(Vector3 dir)
    {
        transform.position += dir * Define.SPEED_MOVE * Time.deltaTime;
    }
}
