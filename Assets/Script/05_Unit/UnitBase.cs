using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

public class UnitBase : MonoBehaviour
{
    public AnimatorOverrideController aoc;
    private Animator animator;

    public AnimationClip[] clips;
    private string[] addresses = new string[] { "ATAHO_IDLE", "ATAHO_SKILL", "ATAHO_HIT" }; //for test
    private AsyncOperationHandle<GameObject>[] loadHandles;


    private void Awake()
    {
        //애니메이터 설정
        animator = transform.GetComponent<Animator>();
        UnityEngine.Assertions.Assert.IsNotNull(animator);
        aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);

        //TODO: Awake()에서 이렇게 호출하는게 바람직한가..?
        //차라리 MonoBehavior 상속 안 받고 처리하는 게 나을 수도 있겠다.
        //그러면 new() 사용하기 좋다. => Invoke, Coroutine 사용 안하니까 굳이 안써도 되겠다?
        Task taskLoadAnime = InitAnimation();
        while (false == taskLoadAnime.IsCompletedSuccessfully)
        {
            continue;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            string clipName = addresses[i];
            UnityEngine.Assertions.Assert.IsNotNull(clipName);
            aoc[clipName] = clip;
        }
    }
    public async Task InitAnimation()
    {
        //애니메이션 클립 가져오기
        loadHandles = new AsyncOperationHandle<GameObject>[addresses.Length];
        clips = new AnimationClip[addresses.Length];
        for (int i = 0; i < addresses.Length; i++)
        {
            string address = addresses[i];
            AnimationClip clip = await AssetMgr.LoadAssetAsync<AnimationClip>(address);
            UnityEngine.Assertions.Assert.IsNotNull(clip);
            clips[i] = clip;
        }
    }
    private void OnDestroy()
    {
        clips = null;

        if (false == AssetMgr.ReleaseAsset(addresses))
        {
            Debug.LogError($"Can`t Release Asset: Animation Clips)");
        }
        addresses = null;
    }
}
