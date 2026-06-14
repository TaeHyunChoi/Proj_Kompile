namespace Kompile.Field.Manager
{
    using Kompile.Asset.Data;
    using Kompile.Asset.Provider;
    using Kompile.Field.Entity;
    using Kompile.Map.Data;
    using Kompile.Map.Manager;
    using Kompile.Unit.Data;
    using Kompile.Unit.Manager;
    using Unity.Collections;
    using UnityEngine;
    using static Kompile.Input.Data.Definition;

    /// <summary>
    /// [Manager 계층] 필드의 게임 흐름, 맵 스트리밍, 엔티티 스폰 타이밍 및 유닛 무브먼트를 총괄 조율합니다.
    /// </summary>
    public class FieldManager
    {
        private readonly MapManager _mapManager;
        private readonly UnitMoveManager _unitMoveManager;
        private readonly FieldEntityManager _entityManager; // 신설된 인스턴스 전담 하위 매니저

        private NativeArray<int> _validGridKeys;
        private readonly Transform _fieldRoot;
        private readonly Transform _unitRoot;
        private FieldEntity _playerEntity;
        private AnimatorOverrideController _baseAOC;

        public FieldManager(Transform fieldRoot)
        {
            _fieldRoot = fieldRoot;

            Transform mapRoot = new GameObject("Map").transform;
            mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mapRoot.SetParent(fieldRoot);

            _unitRoot = new GameObject("Unit").transform;
            _unitRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _unitRoot.SetParent(fieldRoot);
            
            _mapManager = new MapManager(mapRoot);
            _unitMoveManager = new UnitMoveManager();
            _entityManager = new FieldEntityManager(_unitRoot); // 매니저 생성 및 루트 트랜스폼 하달
        }

        public async Awaitable AwakeAsync()
        {
            MapRegistryData registryData = await AssetProvider.ReadBinaryDataAsync<MapRegistryData>("MapRegistry");
            if (registryData == null || registryData.BakedGridKeys == null)
            {
                return;
            }
            
            int count = registryData.BakedGridKeys.Length;
            _validGridKeys = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _validGridKeys.CopyFrom(registryData.BakedGridKeys);
            AssetProvider.RegisterToCurrentSession(_validGridKeys);
            
            _baseAOC = await AssetProvider.LoadAssetAsync<AnimatorOverrideController>(new AssetKey("aoc_field_unit"));

            // 💡 플레이어 엔티티에 사용될 프리팹 인스턴스를 초기화(로딩) 시점에 미리 생성해 둡니다.
            AssetKey prefabKey = new AssetKey(AssetConst.UNIT_PREFAB_FIELD);
            await _entityManager.PrewarmEntitiesAsync(prefabKey, 1);
        }

        public async Awaitable StartAsync(Transform cameraTransform)
        {
            // 대기 없이 즉시 풀에서 동기식으로 꺼내와 연결 프로세스를 밟습니다.
            _playerEntity = await SpawnFieldEntityAsync(1);
#if UNITY_EDITOR
            if (_playerEntity)
            {
                _playerEntity.transform.position = Vector3.forward;
            }
#endif
            _ = _mapManager.PlayStreamingAsync(cameraTransform, _validGridKeys);
        }

        private async Awaitable<FieldEntity> SpawnFieldEntityAsync(int index)
        {
            AssetKey prefabKey = new AssetKey(AssetConst.UNIT_PREFAB_FIELD);
            FieldUnitTableData data = FieldUnitTableProvider.GetData(index);
            FieldUnitAnimClipContext clip = await data.GetAnimClipsAsync();

            // 💡 [Zero-GC 핵심 변경] 비동기 방식인 GetOrNewEntityInstanceAsync 대신, 
            // 하위 매니저의 풀(Pool)에서 동기식으로 즉각 인스턴스 참조를 획득합니다.
            FieldEntity fieldEntity = _entityManager.Spawn(prefabKey);
            
            if (fieldEntity)
            {
                fieldEntity.Initialize(data, clip, _baseAOC);
                _unitMoveManager.Register(fieldEntity);
            }
            
            return fieldEntity;
        }

        // --- Update ---
        public void Update(in InputState inputState)
        {
            // 💡 [완벽 교정] 키 입력 여부와 상관없이 매 프레임 Input 상태를 해석하여 엔티티에 하달합니다.
            // 이를 통해 키를 떼었을 때도 Vector2.zero(정지 상태)가 컴포넌트 버퍼에 실시간 동기화됩니다.
            if (_playerEntity)
            {
                UnitIntent playerMoveIntent = Input2Intent(inputState);
                _playerEntity.UpdateIntent(in playerMoveIntent);
            }

            // -- 파티원 및 NPC 의도 처리 확장 공간 (DOD 파이프라인 유지) --

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
            _entityManager?.Dispose(); // 엔티티 풀 초기화 및 참조 해제
            
            if (_playerEntity)
            {
                _playerEntity = null;
            }
            
            AssetProvider.EndAndReleaseSession();
        }
    }
}