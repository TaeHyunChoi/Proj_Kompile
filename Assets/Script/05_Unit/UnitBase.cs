using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Collections.Generic;

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
    public void SetAnimeClips(AnimationClip[] clips)
    {
        animationClips = clips;

        string groupCode = AssetMgr.GetAnimeGroupCode(indexUnit);
        foreach (var clip in animationClips)
        {
            Debug.Log(groupCode + "_" + clip.name);
        }
    }

    ~UnitBase()
    {
        //TODO: 오브젝트 풀링 고려?
        GameObject.Destroy(transform.gameObject);

        string groupCode = AssetMgr.GetAnimeGroupCode(indexUnit);
        AssetMgr.ReleaseGroupAsset(groupCode);
    }
}

//TODO: 문서에 기록할 것
//1. animation override controller 삭제
//2. animation controller 삭제
//이유: 상태 전환을 직접하려고 함. 2d sprite이므로 mixed, offset duration 등의 기능이 필요 없음.
//3. UnitBase는 Monobehaviour를 상속하지 않는다.
//이유: Update() 콜을 여러 번 돌릴 이유는 없는 듯? + 몇몇 타입은 사용하지도 않는다.
