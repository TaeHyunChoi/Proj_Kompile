namespace Script.Field.Entity
{
    using UnityEngine;
    using Script.Field.Data;

    public abstract class FieldUnitEntityBase : MonoBehaviour
    {
        // --- Identity ---
        public long   InstanceId    { get; protected set; }
        public string AssetAddress  { get; protected set; } // 풀 반환용 키
        public bool   IsInitialized { get; protected set; }

        // 유닛의 현재 상태(체력, 버프, 팩션 등)를 담는 순수 데이터 객체
        protected UnitRuntimeContext _context;

        // 생명주기 및 초기화 (Manager에 의해 제어됨)
        public void SetAssetAddress(string address)
        {
            AssetAddress = address;
        }
        public void Initialize(long instanceId)
        {
            InstanceId = instanceId;
            _context = new UnitRuntimeContext();
            IsInitialized = true;
        }
        public void Clear()
        {
            IsInitialized   = false;
            InstanceId      = 0;
            _context        = null;
        }


        public abstract void ManualUpdate();
    }
}