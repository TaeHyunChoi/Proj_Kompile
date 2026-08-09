// namespace Kompile.Entity
// {
//     using Kompile.Entity.Data; // Entity 클래스가 있는 네임스페이스
//
//     /// <summary> Entity 상속 필드, 전투, NPC 등 모든 유닛 개체의 베이스 클래스
//     /// </summary>
//     public abstract class UnitEntityBase : Entity
//     {
//         // // Manager의 Dictionary<long, TEntity>에서 식별자로 사용될 핵심 키
//         // protected long _instanceID;
//         // protected bool _isInitialized;
//         // protected IUnitBrain _brain;
//         //
//         // public long InstanceID => _instanceID;
//         // public bool IsInitialized => _isInitialized;
//
//         // public virtual void Clear() // 풀링 시 호출될 수 있으므로 virtual 권장
//         // {
//         //     _isInitialized = false;
//         //     _instanceID = 0;
//         //     _brain?.Clear();
//         //     _brain = null;
//         // }
//     }
// }