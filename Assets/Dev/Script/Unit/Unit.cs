using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public partial class Unit : MonoBehaviour
{
    public UnitData Data { get => data; }
    private UnitData data;

    private Animator animator;
    private AnimatorOverrideController aoc;

    public int[] Status { get => status; }
    private int[] status;

    public Vector3 Pos { get => transform.position; }
    public Vector3 LocalPos { get => transform.localPosition; }

    public void Init(int code)
    {
        //기본값 정보 저장
        data = DataMgr.UnitTBL.Find(unit => unit.Code == code);

        //스탯 초기화 (깊은 복사 사용)
        status = new int[IDxUNIT.STAT_CNT];
        Array.Copy(Data.StatDefault, status, IDxUNIT.STAT_CNT);

        //캐릭터 스킬, 공통 스킬
        List<SkillData> skill = DataMgr.SkillTBL.FindAll(skill => (skill.ActorCode == code) || (skill.ActorCode == IDxUNIT.COMMON));

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

    public void MoveTo(int mx, int mz)
    {
        transform.position += new Vector3(mx, 0, mz) * IDxUNIT.SPEED_MOVE * Time.deltaTime;
    }

    private void PlayAnime(string type, string code = null)
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
}