using Kompile.Field.Data;
using Kompile.Field.Entity;
using Kompile.Map.Manager;
using Kompile.Asset.Provider;
using Kompile.Asset.Data;
using Kompile.Unit.Data;
using System;

namespace Kompile.Field.Manager
{
    using UnityEngine;

    public class FieldManager
    {
        private readonly MapManager _mapManager;
        private readonly FieldMapQueryService _mapQueryService;

        private readonly Transform _fieldRoot;
        private readonly Transform _unitRoot;
        private FieldPlayerEntity _playerEntity;

        private bool _isFieldActive;

        public IMapQueryService MapQueryService => _mapQueryService;

        // --- Constructor ---
        public FieldManager(Transform fieldRoot)
        {
            _fieldRoot = fieldRoot;

            Transform mapRoot = new GameObject("Map").transform;
            mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mapRoot.SetParent(fieldRoot);
            _mapManager = new MapManager(mapRoot);

            _mapQueryService = new FieldMapQueryService(_mapManager);

            _unitRoot = new GameObject("Unit").transform;
            _unitRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _unitRoot.SetParent(fieldRoot);

            _isFieldActive = false;
        }

        public void StartFieldAsync(Transform cameraTransform)
        {
            _isFieldActive = true;
            _ = _mapManager.PlayStreamingAsync(cameraTransform); // fire and forget
            _ = SpawnPlayerAsync();                              // fire and forget
            // _ = SpawnUnitsAsync(units);
        }

        /// <summary> 플레이어 유닛을 비동기 생성·초기화. StartFieldAsync에서 fire and forget으로 호출. </summary>
        private async Awaitable SpawnPlayerAsync()
        {
            try
            {
                _playerEntity = await AssetProvider.GetOrNewUnitInstanceAsync<FieldPlayerEntity>(_unitRoot);

                UnitTableData tableData = UnitTableProvider.GetUnitData(1);
                UnitRuntimeContext ctx = new UnitRuntimeContext(tableData.Type, default);
                _playerEntity.Initialize(ctx, _mapQueryService);

                // for test
                _playerEntity.transform.SetPositionAndRotation(Vector3.back, Quaternion.identity);
#if UNITY_EDITOR
                _playerEntity.gameObject.name = "player";
#endif
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }

        public void Update()
        {
            _playerEntity?.UpdateManual();
            // TODO: 하위 Manager·Service Update 순차 호출 (향후 추가)
        }

        public void Dispose()
        {
            _mapManager.StopStreaming();

            if (_playerEntity)
            {
                AssetProvider.ReleaseInstance(_playerEntity.Key, _playerEntity.gameObject);
                _playerEntity = null;
            }

            _isFieldActive = false;
        }
    }
}
