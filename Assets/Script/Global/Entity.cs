namespace Script.Global.Entity.Data
{
    using UnityEngine;
    using Script.Asset.Data;

    /// <summary>
    /// [Framework] 핵심 계층: Entity
    /// Component들의 조합으로 구현된 게임 내 논리적 실체입니다.
    /// 기존 IngameMonoBehaviourBase를 대체하며, Manager에 의해 생명주기가 관리됩니다.
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        /// <summary> 
        /// 이 Entity를 생성하는 데 사용된 에셋 식별자입니다. 
        /// 풀링(반환) 시에 사용되며, Manager나 Provider가 인스턴스화할 때 주입합니다.
        /// </summary>
        public AssetKey Key { get; private set; }

        /// <summary>
        /// Manager가 이 Entity를 팩토리/풀에서 꺼내어 초기화할 때 키를 세팅해줍니다.
        /// </summary>
        public virtual void SetAssetKey(AssetKey key)
        {
            Key = key;
        }
    }

}