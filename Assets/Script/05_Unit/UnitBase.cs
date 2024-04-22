using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Collections.Generic;

public class UnitBase
{
    public Transform transform { get; set; }
    private Animator animator;
    private AnimationClip[] animationClips;

    public async Task<UnitBase> AwakeAsync(int indexUnit, Transform transform)
    {
        this.transform = transform;
        animator = transform.GetComponent<Animator>();

        //그룹별로 끌어오지 않고, '배운 스킬까지만' 불러오는 방법도 가능할 것 같은데...
        string groupCode = GetAnimeGroupCode(indexUnit);
        AsyncOperationHandle<IList<AnimationClip>> handle = Addressables.LoadAssetsAsync<AnimationClip>(groupCode, null);
        await handle.Task;

        animationClips = new List<AnimationClip>(handle.Result).ToArray();

        //TODO: 필요한 만큼만 애니메이션 클립 로드 (ex. 아직 배우지 않은 스킬을 로드할 필요 없음)
        //for (int i = 0; i < animationClips.Length; ++i)
        //{
        //    if (false == ShouldLoadAnimationClip(indexUnit, i))
        //    {
        //        animationClips[i] = null;
        //    }
        //}

        UnityEngine.Assertions.Assert.IsNotNull(animationClips, "Null Anime Clip: " + groupCode);

        foreach (var clip in animationClips)
        {
            Debug.Log(groupCode + " " + clip.name);
        }

        handle.Task.Dispose();
        return this;
    }

    private string GetAnimeGroupCode(int index)
    {
        switch (index)
        {
            case 0: return "Anime_Ataho";

        }
        return null;
    }
    private bool ShouldLoadAnimationClip(int indexUnit, int indexClip)
    { 
        return true;
    }
}

//TODO: 문서에 기록할 것
//1. animation override controller 삭제
//2. animation controller 삭제
//이유: 상태 전환을 직접하려고 함. 2d sprite이므로 mixed, offset duration 등의 기능이 필요 없음.
