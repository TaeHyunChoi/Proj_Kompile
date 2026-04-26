namespace Kompile.Unit.Entity
{
    using Kompile.Entity.Data; // Entity 클래스가 있는 네임스페이스
    using Kompile.Unit.Data;
    
    /// <summary> Entity 상속 필드, 전투, NPC 등 모든 유닛 개체의 베이스 클래스
    /// </summary>
    public abstract class UnitEntityBase : Entity
    {
        // Manager의 Dictionary<long, TEntity>에서 식별자로 사용될 핵심 키
        public long InstanceID { get; protected set; }
        public bool IsInitialized { get; protected set; }
        public UnitRuntimeContext Context { get; protected set; }
        
        protected IUnitBrain _brain;

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
        public abstract void ManualUpdate(); // Manager에 의해 호출될 업데이트 수동 제어
    }
}