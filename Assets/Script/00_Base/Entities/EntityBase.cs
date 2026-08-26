namespace Kompile.Entity
{
    using UnityEngine;
    using Kompile.Data;

    /// <summary> Component들의 조합으로 구현된 게임 내 논리적 실체. Manager에 의해 생명주기가 관리 </summary>
    public abstract class EntityBase : MonoBehaviour
    {
        public AssetKey _key;
        protected long _instanceID; // Manager의 Dictionary<long, TEntity>에서 식별자로 사용될 핵심 키
        protected bool _isInitialized;

        
        /// <summary> 이 Entity를 생성하는 데 사용된 에셋 식별자. 풀링(반환) 시에 사용되며, Manager나 Provider가 인스턴스화할 때 주입 </summary>
        public AssetKey Key => _key;
        public long InstanceID => _instanceID;
        public bool IsInitialized => _isInitialized;
        
        
        /// <summary> Manager가 이 Entity를 팩토리/풀에서 꺼내어 초기화할 때 키를 세팅 </summary>
        public virtual void SetAssetKey(AssetKey key)
        {
            _key = key;
        }
        public virtual void Clear() // 풀링 시 호출될 수 있으므로 virtual 권장
        {
            _isInitialized = false;
            _instanceID = 0;
        }
    }

}