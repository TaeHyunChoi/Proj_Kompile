using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Unit : MonoBehaviour
{
    //Component (모두 private을 지향)
    private AnimatorOverrideController  aoc;
    private Animator                    animator;
    private SpriteRenderer              render;
    private UnitCoroutine               coroutine; //custom coroutine

    public  UnitData Data { get => data; }
    private UnitData data;

    public  SkillData[][] Skill { get => skill; }
    private SkillData[][] skill;

    public Vector3 Pos { get => transform.position; }

    public  int[] Status { get => status; }
    private int[] status;

    public  float Priority { get => priority; }
    private float priority;

    public  bool IsFaint { get => isFaint; }
    private bool isFaint;

    private int lastSelect;

    public void Init(int code)
    {
        //기본값 정보 저장
        data = DataMgr.UnitTBL.Find(unit => unit.Index == code);

        //스탯 초기화 (깊은 복사 사용)
        status = new int[IDxUNIT.STAT_CNT];
        Array.Copy(Data.StatDefault, status, IDxUNIT.STAT_CNT);

        //캐릭터 스킬, 공통 스킬
        skill = new SkillData[4][];
        List<SkillData> skillData = DataMgr.SkillTBL.FindAll(skill => (skill.ActorCode == code) || (skill.ActorCode == IDxUNIT.COMMON));
        skill[IDxSkill.BASIC]     = skillData.FindAll(skill => skill.SkillGroup == IDxSkill.BASIC).ToArray();
        skill[IDxSkill.SOLO]      = skillData.FindAll(skill => skill.SkillGroup == IDxSkill.SOLO).ToArray();
        skill[IDxSkill.GROUP]     = skillData.FindAll(skill => skill.SkillGroup == IDxSkill.GROUP).ToArray();
        skill[IDxSkill.SPECIAL]   = skillData.FindAll(skill => skill.SkillGroup == IDxSkill.SPECIAL).ToArray();

        //애니메이션(AOC)
        aoc = new AnimatorOverrideController(ResourceMgr.AOC);
        animator = transform.GetComponent<Animator>();
        animator.runtimeAnimatorController = aoc;
        render = transform.GetComponent<SpriteRenderer>();
        coroutine = transform.GetComponent<UnitCoroutine>();

        PlayAnime(IDxUNIT.ANIME_IDLE);
    }

    
    public Vector3 MoveTo(Vector3 move)
    {
        transform.position += move * IDxUNIT.SPEED_MOVE * Time.deltaTime;
        return transform.position;
    }

    //역시 뒤숭숭했군..
    public void BattleProc_AISelect()
    {
        //## Select Skill
        int idxGroup = IDxSkill.BASIC; //임의 설정
        int idxSkill = UnityEngine.Random.Range(0, skill[idxGroup].Length);

        //## Select Target
        int rnd = UnityEngine.Random.Range(0, 3);
        int flagTarget = UnitMgr.GetTargetFlag(rnd, (ETargetGroup)skill[idxGroup][idxSkill].TargetGroupType);

        //## Update Last Select
        lastSelect = 0;
        lastSelect = (flagTarget << BIT.SHIFT_TARGET) | (idxGroup << BIT.SHIFT_MENU) | idxSkill;

        //## Play Action
        coroutine.InitAttack();
    }     //For AI


    public void PlayAnime(string type, string code = null)
    {
        if (code == null)
            code = type;

        AnimationClip ac = ResourceMgr.Anime[Data.RcsCode + "/" + code];
        aoc[type] = ac;
        animator.CrossFade(type, 0f);
    }
    public void PlayCoroutine(int idxHitter, SkillData skill)
    {
        coroutine.InitHit(UnitMgr.InBattle[idxHitter], skill);
    }


    public float GetAnimeLength(string code)
    {
        return aoc[code].length;
    }

    public void SetStatus()
    {
        //## Mode => Status Weighted
        //추후 개발

        //## Set Priority
        float isLukcy = Status[IDxUNIT.LUK];
        float rnd = UnityEngine.Random.Range(0, 10000);

        if (rnd == 0)
        {
            isLukcy = 0.5f;
        }
        else if (rnd <= isLukcy)
        {
            isLukcy = 2;
        }
        else
        {
            isLukcy = 1;
        }

        //테스트용
        if (Data.Group == IDxUNIT.ENEMY)
        {
            isLukcy *= UnityEngine.Random.Range(0.9f, 1.1f);
        }

        priority = Status[IDxUNIT.AGI] * isLukcy;
    }
    public void SetRenderOrder(int order)
    {
        render.sortingOrder = order;
    }
    public void SetAnimeSpeed(float end, float lerpWeight)
    {
        animator.speed = Mathf.Lerp(animator.speed, end, IDxVALUE.LERP * lerpWeight);
    }


    //얘도 고민 좀. 데미지 계산이면 Mgr급으로 빠져도 될 것 같은데?
    public int CalcDamage(Unit hitter, SkillData hitSkill)
    {
        return hitSkill.Power + (hitter.status[IDxUNIT.DEX] >> 2) - status[IDxUNIT.CON];
    }

    //## Animation Tag
    public void OnAnime_ReadyToCombo()
    {
        InputMgr.Set_IsCombo(true);
    }
    public void OnAnimeSkill_HitTarget()
    {
        int idxGroup = (lastSelect & BIT.MASK_NOW_MENU) >> BIT.SHIFT_MENU;
        int idxSkill = (lastSelect & BIT.MASK_NOW_CONTENT);
        int flagTarget = (lastSelect >> BIT.SHIFT_TARGET);

        UnitMgr.PlayAnime_Hit(GameMgr.NowOrder, flagTarget, idxGroup, idxSkill);
    }
}