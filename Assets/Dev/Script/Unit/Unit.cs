using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class Unit : MonoBehaviour
{
    //Component
    private AnimatorOverrideController  aoc;
    private Animator                    animator;
    private SpriteRenderer              render;
    private UnitCoroutine               coroutine; //custom coroutine

    //뭔가 너저분하게 있어서 아쉬운걸;
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

    public  int LastSelect { get => lastSelect; }
    private int lastSelect;

    public void Init(int code)
    {
        //기본값 정보 저장
        data = DataMgr.UnitTBL.Find(unit => unit.Index == code);

        //스탯 초기화 (깊은 복사 사용)
        status = new int[IDxUNIT.STAT_CNT];
        Array.Copy(Data.StatDefault, status, IDxUNIT.STAT_CNT);
        isFaint = false;

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

        Anime_Play(IDxUNIT.ANIME_IDLE);
    }


    //## Status
    public void Status_SetInBattle()
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
    public  int Status_CalcDamage(Unit hitter, SkillData hitSkill)
    {
        return hitSkill.Power + (hitter.status[IDxUNIT.DEX] >> 2) - status[IDxUNIT.CON];
    } //데미지 계산이면 Mgr급으로 빠져도 될 것 같은데?


    //## Select > Battle
    private int LastSelect_SetAIBattle()
    {
        //## Select Skill
        int idxGroup = IDxSkill.BASIC; //임의 설정
        int idxSkill = UnityEngine.Random.Range(0, skill[idxGroup].Length);

        //## Select Target
        int rnd = UnityEngine.Random.Range(0, 3);
        int flagTarget = UnitMgr.GetFlag_Target(rnd, skill[idxGroup][idxSkill].TargetGroupType);

        //## Update Last Select
        return (flagTarget << BIT.SHIFT_TARGET) | (idxGroup << BIT.SHIFT_MENU) | idxSkill;
    }
    public void LastSelect_Update(int select)
    {
        lastSelect = select;

        int idxTarget  = (lastSelect & BIT.MASK_NOW_TARGET) >> BIT.SHIFT_TARGET;
        int idxMenu    = (lastSelect & BIT.MASK_NOW_MENU) >> BIT.SHIFT_MENU;
        int idxContent = (lastSelect & BIT.MASK_NOW_CONTENT);

        Debug.Log($"[Update:{Data.Name}] {skill[idxMenu][idxContent].Name} => Target:{Convert.ToString(idxTarget, 2).PadLeft(7, '0')}");
    }


    //## Process(Proc)
    public void ProcBattle_Attack()
    {
        if (data.Group == IDxUNIT.ENEMY)
        {
            lastSelect = LastSelect_SetAIBattle();
        }

        //여기도 쪼까 쎄하네? 로직 정리 추가로 필요
        //
        //else if(data.Group == IDxUNIT.PARTY)
        //{
        //    lastSelect = select;
        //}

        coroutine.InitAttack();
    }
    public void ProcBattle_Hit(Unit hitter, SkillData skill)
    {
        coroutine.InitHit(hitter, skill);
    }


    //## Vector
    public Vector3 Move(Vector3 move)
    {
        return transform.position += move;
    }


    //## Anime
    public  void Anime_Play(string type, string code = null)
    {
        if (code == null)
            code = type;

        AnimationClip ac = ResourceMgr.Anime[Data.RcsCode + "/" + code];
        aoc[type] = ac;
        animator.CrossFade(type, 0f);
    }
    public float Anime_GetLength(string code)
    {
        return aoc[code].length;
    }
    public  void Anime_SetSpeed(float end, float lerpWeight)
    {
        animator.speed = Mathf.Lerp(animator.speed, end, IDxVALUE.LERP * lerpWeight);
    }


    //## Render
    public void Render_SetOrder(int order)
    {
        render.sortingOrder = order;
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

        UnitMgr.Anime_PlayHit(this, flagTarget, idxGroup, idxSkill);
    }
}