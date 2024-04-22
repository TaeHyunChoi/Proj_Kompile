using UnityEngine;
using static EAnimeCodeToString;

public abstract class UnitBase
{
    public    Transform transform { get; set; }
    protected Animator animator;
    protected AnimationClip[] animationClips;
    protected int indexUnit;

    public void Awake(int indexUnit, Transform transform)
    {
        this.transform = transform;
        this.indexUnit = indexUnit;
        animator = transform.GetComponent<Animator>();

    }
    public void SetAnimeController(RuntimeAnimatorController controller)
    {
        animator.runtimeAnimatorController = controller;
        PlayAnime(IDLE_FRONT);
    }
    protected void PlayAnime(EAnimeCodeToString code)
    {
        string anime = null;
        switch (code)
        {
            default:
                anime = code.ToString();
                break;
            case NONE:
                break;
        }

        animator.Play(anime, 0);
    }

    //소멸자가 명시적으로 호출되지 않는다고 하니... 직접 Release()하겠다.
    public bool Release()
    {
        //TODO: 오브젝트 풀링 고려?
        GameObject.Destroy(transform.gameObject);

        string address = AssetMgr.GetAssetAddress(EAssetType.AnimCtrl, indexUnit);
        return AssetMgr.ReleaseAsset(address);
    }
}

//TODO: 문서에 기록할 것
//1. animation override controller 삭제
//2. animation controller 삭제
//이유: 상태 전환을 직접하려고 함. 2d sprite이므로 mixed, offset duration 등의 기능이 필요 없음.
//3. UnitBase는 Monobehaviour를 상속하지 않는다.
//이유: Update() 콜을 여러 번 돌릴 이유는 없는 듯? + 몇몇 타입은 사용하지도 않는다.
