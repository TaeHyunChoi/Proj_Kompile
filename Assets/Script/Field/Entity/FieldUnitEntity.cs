namespace Script.Field.Entity
{
    using Script.Field.Data;
    using UnityEngine;

    /// <summary> 
    /// Field 상의 모든 유닛 개체 (Player, NPC, Enemy 공통). 
    /// 오직 Component들의 조합과 식별 정보만 유지합니다. 
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class FieldUnitEntity : MonoBehaviour
    {
        // --- Identity ---
        public long InstanceId { get; private set; }
        public string AssetAddress { get; private set; }
        public bool IsInitialized { get; private set; }

        // 유닛의 런타임 상태 데이터 (여기에 UnitType이 포함됨)
        private UnitRuntimeContext _context;
        public UnitRuntimeContext Context => _context;

        // --- Components ---
        // 성능이 중요한 환경이므로, GetComponent를 최소화하고 필요한 컴포넌트는 캐싱합니다.
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;
        private IUnitBrainComponent _brainComponent; // PlayerInput, AI 등에 따라 다르게 부착됨

        public void SetAssetAddress(string address)
        {
            AssetAddress = address;
        }

        public void Initialize(long instanceId, UnitRuntimeContext context)
        {
            InstanceId = instanceId;
            _context = context; // Manager가 생성한 초기 컨텍스트(Type 포함) 주입

            // Awake 대신 Initialize 시점에 캐싱 (풀링 재사용 시 안전성 확보)
            _moveComponent = GetComponent<UnitMoveComponent>();
            _animComponent = GetComponent<UnitAnimComponent>();
            _brainComponent = GetComponent<IUnitBrainComponent>();

            if (_moveComponent) _moveComponent.Initialize(this);
            if (_animComponent) _animComponent.Initialize(this);
            if (_brainComponent != null) _brainComponent.Initialize(this);

            IsInitialized = true;
        }

        public void Clear()
        {
            IsInitialized = false;
            InstanceId = 0;
            _context = null;
        }

        public void ManualUpdate()
        {
            // 타입 분기 없이, 부착된 컴포넌트들의 업데이트만 순차적으로 호출합니다.
            if (_brainComponent != null) _brainComponent.ManualUpdate();
            if (_moveComponent) _moveComponent.ManualUpdate();
            if (_animComponent) _animComponent.ManualUpdate();
        }
    }
}