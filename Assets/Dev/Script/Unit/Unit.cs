using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Unit : MonoBehaviour //Default
{
    public UnitData Data { get => data; }
    private UnitData data;

    private Animator animator;

    public AnimatorOverrideController AOC { get => aoc; }
    private AnimatorOverrideController aoc;

    private SpriteRenderer render;

    public int[] Status { get => status; }
    private int[] status;

    public Vector3 Pos { get => transform.position; }
    public Vector3 LocalPos { get => transform.localPosition; }



    private UnitCoroutine coPlayer;

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

    public Vector3 MoveTo(Vector3 move)
    {
        transform.position += move * IDxUNIT.SPEED_MOVE * Time.deltaTime;
        return transform.position;
    }
    public void SetRenderOrder(int order)
    {
        render.sortingOrder = order;
    }
    public void SetAnimeSpeed(float end, float lerpWeight)
    {
        animator.speed = Mathf.Lerp(animator.speed, end, IDxVALUE.LERP * lerpWeight);
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
    private bool IsAnimeEnd(string name, float wait)
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(name))
            return false;

        if (wait > Time.time)
            return false;

        return true;
    }


    //## Animation Trigger
    public void OnAnime_ReadyToCombo()
    {
        InputMgr.Set_IsCombo(true);
    }
}