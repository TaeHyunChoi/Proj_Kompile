namespace Kompile.Entity.Data
{
    using UnityEngine;
    using Kompile.Asset.Data;

    /// <summary> Component들의 조합으로 구현된 게임 내 논리적 실체. Manager에 의해 생명주기가 관리 </summary>
    public abstract class Entity : MonoBehaviour
    {
        /// <summary> 이 Entity를 생성하는 데 사용된 에셋 식별자. 
        /// 풀링(반환) 시에 사용되며, Manager나 Provider가 인스턴스화할 때 주입.
        /// </summary>
        public AssetKey Key { get; private set; }

        /// <summary> Manager가 이 Entity를 팩토리/풀에서 꺼내어 초기화할 때 키를 세팅 </summary>
        public virtual void SetAssetKey(AssetKey key)
        {
            Key = key;
        }
    }

}