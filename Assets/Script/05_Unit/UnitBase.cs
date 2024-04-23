using UnityEngine;
using static EAnimeCodeToString;

public abstract class UnitBase /* : MonoBehaviour */
{
    protected Transform transform;
    public  Transform Transform { get => transform; }

    protected AnimationClip[] animationClips;
    protected Animator        animator;
    protected int             indexUnit;

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

    public bool Release()
    {
        //TODO: 오브젝트 풀링을 해야 할까?
        GameObject.Destroy(transform.gameObject);

        string address = AssetMgr.GetAssetAddress(EAssetType.AnimCtrl, indexUnit);
        return AssetMgr.ReleaseAsset(address);
    }
}

