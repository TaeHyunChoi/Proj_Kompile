using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Unit : MonoBehaviour //Default
{
    public  AnimatorOverrideController AOC { get => aoc; }
    private AnimatorOverrideController aoc;
    private Animator animator;
    private SpriteRenderer render;
    private UnitCoroutine coPlayer;

    public  UnitData Data { get => data; }
    private UnitData data;

    public  SkillData[][] Skill { get => skill; }
    private SkillData[][] skill;

    public Vector3 Pos { get => transform.position; }

    public  int[] Status { get => status; }
    private int[] status;

    public  byte Mode { get => Mode; }
    private byte mode;

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
        coPlayer = transform.GetComponent<UnitCoroutine>();

        PlayAnime(IDxUNIT.ANIME_IDLE);
    }

    //## Move
    public Vector3 MoveTo(Vector3 move)
    {
        transform.position += move * IDxUNIT.SPEED_MOVE * Time.deltaTime;
        return transform.position;
    }

    //## Render
    public void SetRenderOrder(int order)
    {
        render.sortingOrder = order;
    }


    //## Animation
    public void PlayAnime(string type, string code = null)
    {
        if (code == null)
            code = type;

        AnimationClip ac = ResourceMgr.Anime[Data.RcsCode + "/" + code];
        aoc[type] = ac;
        animator.CrossFade(type, 0f);
    }
    public void SetAnimeSpeed(float end, float lerpWeight)
    {
        animator.speed = Mathf.Lerp(animator.speed, end, IDxVALUE.LERP * lerpWeight);
    }
    public void OnAnime_ReadyToCombo()
    {
        InputMgr.Set_IsCombo(true);
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
                Unit target = UnitMgr.InBattle[i];
                SkillData skill = target.Skill[idxGroup][idxSkill];
                target.coPlayer.InitHit(target, skill);
            }
        }
    }
}