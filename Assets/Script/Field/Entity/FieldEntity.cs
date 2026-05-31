namespace Kompile.Field.Entity
{
    using Kompile.Asset.Data;
    using Kompile.Asset.Provider;
    using Kompile.Field.Data;
    using Kompile.Unit.Component;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;
    using UnityEngine;
    
    [RequireComponent(typeof(UnitMoveComponent), typeof(UnitAnimComponent))]
    public class FieldEntity : UnitEntityBase
    {
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;

        private AnimatorOverrideController _aoc;

        public void Initialize(FieldUnitTableData data, FieldUnitAnimClipContext clip, AnimatorOverrideController baseAOC, FieldMapQueryService mapQuery)
        {
            // brain
            switch (data.BrainType)
            {
                case UnitBrainType.Player: _brain = new PlayerControlBrain(this); break;
                default:
                    break;
            }

            // anim-component
            _animComponent = transform.GetComponent<UnitAnimComponent>();
            _animComponent.Initialize(baseAOC, in clip);  

            // move-component
            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            _moveComponent.Initialize(this, mapQuery);
        }

        //public async Awaitable InitializeAsync(int index, UnitBrainType brainType, FieldMapQueryService mapQuery)
        //{

        //    UnitTableData tableData = UnitTableProvider.GetUnitData(index);
        //    if (!AssetProvider.TryGetFieldUnitAnim(tableData.Address, out FieldUnitAnimClipContext clipSet))
        //    {
        //        Debug.LogError($"[Debug] fail to get unit anim (unit index: {index})");
        //        return;
        //    }

        //    _moveComponent = transform.GetComponent<UnitMoveComponent>();
        //    _animComponent = transform.GetComponent<UnitAnimComponent>();

        //    _instanceID = GetInstanceID();


        //    //AssetKey aocKey = new AssetKey(tableData.AocAddressStr);
        //    //_aoc = await AssetProvider.LoadAssetAsync<AnimatorOverrideController>(aocKey);

        //    _moveComponent.Initialize(this, mapQuery);
        //    //_animComponent.Initialize(_aoc, in clipSet);

        //    switch (brainType)
        //    {
        //        case UnitBrainType.Player: _brain = new PlayerControlBrain(this); break;
        //        default:
        //            break;
        //    }
        //}

        /// <summary> _brain을 사용하여 owner가 intent를 직접 판단 </summary>
        public void UpdateIntent()
        {
            UnitIntent intent = _brain.Update();
            _moveComponent.UpdateIntent(in intent);
            _animComponent.UpdateIntent(in intent);
        }

        /// <summary> _brain을 사용하지 않고 직접 intent를 주입하는 경우 </summary>
        public void UpdateIntent(in UnitIntent intent)
        {
            //_brain.Update(in intent);
            _moveComponent.UpdateIntent(in intent);
            _animComponent.UpdateIntent(in intent);
        }

        private void OnDisable()
        {
            AssetProvider.ReleaseAsset(_aoc.GetInstanceID());
        }
    }
}
