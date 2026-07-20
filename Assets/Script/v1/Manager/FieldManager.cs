namespace Kompile.Manager
{
    using Kompile.Provider;
    using Kompile.Data;
    using Kompile.Entity;
    using Unity.Collections;
    using UnityEngine;
    using static Kompile.Input.Data.Definition;

    public class FieldManager
    {
        private readonly MapManager _mapManager;
        private readonly FieldMapQueryService _mapQueryService;
        private readonly UnitMoveManager _unitMoveManager;

        private NativeArray<int> _validGridKeys;
        private readonly Transform _fieldRoot;
        private readonly Transform _unitRoot;
        private FieldEntity _playerEntity;
        private AnimatorOverrideController _templateAOC;

        public FieldManager(Transform fieldRoot)
        {
            _fieldRoot = fieldRoot;

            Transform mapRoot = new GameObject("Map").transform;
            mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mapRoot.SetParent(fieldRoot);

            _mapManager = new MapManager(mapRoot);
            _mapQueryService = new FieldMapQueryService(_mapManager);
            _unitMoveManager = new UnitMoveManager();

            _unitRoot = new GameObject("Unit").transform;
            _unitRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _unitRoot.SetParent(fieldRoot);
        }

        public async Awaitable AwakeAsync()
        {
            MapRegistryData registryData = await AssetProvider.ReadBinaryDataAsync<MapRegistryData>("MapRegistry");
            if (null == registryData
                || null == registryData.BakedGridKeys)
            {
                return;
            }
            
            int count = registryData.BakedGridKeys.Length;
            _validGridKeys = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _validGridKeys.CopyFrom(registryData.BakedGridKeys);
            AssetProvider.RegisterToCurrentSession(_validGridKeys);
            
            _templateAOC = await AssetProvider.LoadAssetAsync<AnimatorOverrideController>(new AssetKey("aoc_field_unit"));
        }

        public async Awaitable StartAsync(Transform cameraTransform)
        {
            _playerEntity = await SpawnFieldEntityAsync(1);
#if UNITY_EDITOR
            _playerEntity.transform.position = Vector3.forward;
#endif
            _ = _mapManager.PlayStreamingAsync(cameraTransform, _validGridKeys);
        }

        private async Awaitable<FieldEntity> SpawnFieldEntityAsync(int index)
        {
            FieldEntity fieldEntity = await AssetProvider.Field.GetOrNewEntityInstanceAsync(1, _unitRoot, _templateAOC, null);
            _unitMoveManager.Register(fieldEntity.MoveComponent);
            
            return fieldEntity;
        }

        // --- Update ---
        public void Update(in InputState inputState)
        {
            // 💡 [완벽 교정] 키 입력 여부와 상관없이 매 프레임 Input 상태를 해석하여 엔티티에 하달합니다.
            // 이를 통해 키를 떼었을 때도 Vector2.zero(정지 상태)가 컴포넌트 버퍼에 실시간 동기화됩니다.
            UnitIntent playerMoveIntent = Input2Intent(inputState);
            _playerEntity.UpdateIntent(in playerMoveIntent);

            // -- 파티원 및 NPC 의도 처리 확장 공간 (DOD 파이프라인 유지) --
            // ....

            // 2. 물리적 이동 일괄 처리 위임 (UnitMoveManager)
            if (_mapManager.NativeTileMap.IsCreated)
            {
                _unitMoveManager.ExecuteMoveJobs(Time.deltaTime, _mapManager.NativeTileMap);
            }
        }

        public UnitIntent Input2Intent(in InputState inputState)
        {
            float x = 0f, z = 0f;
            if (inputState.IsPressing(IDxInput.RIGHT)) { x += 1f; }
            if (inputState.IsPressing(IDxInput.LEFT)) { x -= 1f; }
            if (inputState.IsPressing(IDxInput.UP)) { z += 1f; }
            if (inputState.IsPressing(IDxInput.DOWN)) { z -= 1f; }

            return new UnitIntent
            {
                MoveInput = new Vector2(x, z),
                AnimCommand = UnitAnimCmd.None,
            };
        }

        public void Dispose()
        {
            _mapManager?.StopStreaming();
            _unitMoveManager?.Dispose();

            if (_playerEntity)
            {
                _playerEntity.Clear();
                AssetProvider.ReleaseInstance(_playerEntity.Key, _playerEntity.gameObject);
                AssetProvider.ReleaseAsset(_templateAOC.GetHashCode());
                _playerEntity = null;
            }
        }
    }
}