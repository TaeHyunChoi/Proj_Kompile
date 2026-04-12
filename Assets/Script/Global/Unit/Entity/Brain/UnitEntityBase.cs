namespace Script.Unit.Entity
{
    using Script.Entity.Data; // Entity 클래스가 있는 네임스페이스
    using Script.Unit.Data;
    
    /// <summary>
    /// [Framework] Entity 상속: 
    /// 필드, 전투, NPC 등 모든 유닛 개체의 베이스 클래스
    /// </summary>
    public abstract class UnitEntityBase : Entity // MonoBehaviour 대신 Entity를 상속받음
    {
        // Manager의 Dictionary<long, TEntity>에서 식별자로 사용될 핵심 키
        public long InstanceID { get; protected set; }
        
        public bool IsInitialized { get; protected set; } // 오타 수정 (IsInitalized -> IsInitialized)

        // Runtime 상태를 들고 있는 데이터 (가이드라인: Runtime Context)
        public UnitRuntimeContext Context { get; protected set; }
        
        protected IUnitBrain _brain;

        // AssetAddress 필드 및 SetAssetAddress 메서드 제거 
        // -> 부모 클래스(Entity)의 Key와 SetAssetKey()를 활용하여 Provider/Manager와 연동

        public void SetBrain(IUnitBrain newBrain)
        {
            _brain?.Clear();

            if (newBrain != null)
            {
                _brain = newBrain;
                _brain.Initialize(this);
            }
        }

        public virtual void Clear() // 풀링 시 호출될 수 있으므로 virtual 권장
        {
            IsInitialized = false;
            InstanceID = 0;
            Context = default(UnitRuntimeContext);

            _brain?.Clear();
            _brain = null;
        }

        public abstract void Initialize(long instanceID, UnitRuntimeContext context);
        
        // Manager에 의해 호출될 업데이트 수동 제어
        public abstract void ManualUpdate(); 
    }
}