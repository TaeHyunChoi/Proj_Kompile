namespace Kompile.Field.Manager
{
    using Kompile.Asset.Provider;
    using Kompile.Field.Data;
    using Kompile.Field.Entity;
    using Kompile.Asset.Data;
    using Kompile.Map.Manager;
    using Kompile.Unit.Data;
    using UnityEngine;
    using static Kompile.Input.Data.Definition;

    using Unity.Collections;
    using Kompile.Map.Data;

    public class FieldManager
    {
        private readonly MapManager _mapManager;
        private readonly FieldMapQueryService _mapQueryService;
        private NativeArray<int> _validGridKeys;

        private readonly Transform _fieldRoot;
        private readonly Transform _unitRoot;
        private FieldEntity _playerEntity;

        private AnimatorOverrideController _templeteAOC;
        
        public FieldManager(Transform fieldRoot)
        {
            _fieldRoot = fieldRoot;

            Transform mapRoot = new GameObject("Map").transform;
            mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mapRoot.SetParent(fieldRoot);
            _mapManager = new MapManager(mapRoot);

            // 버전, 빌드 버전에 따라 교체할 수 있다.
            _mapQueryService = new FieldMapQueryService(_mapManager);

            _unitRoot = new GameObject("Unit").transform;
            _unitRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _unitRoot.SetParent(fieldRoot);
        }

        public async Awaitable AwakeAsync()
        {
            MapRegistryData registryData = await AssetProvider.ReadBinaryDataAsync<MapRegistryData>("MapRegistry");
            if (registryData?.BakedGridKeys == null)
            {
                return;                
            }
            
            // 메모리 효율을 위해 캐싱용 네이티브 배열 생성 -> 안전하게 데이터 복사 (이미 에디터 빌드 시점에 순서 정렬)
            int count = registryData.BakedGridKeys.Length;
            _validGridKeys = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _validGridKeys.CopyFrom(registryData.BakedGridKeys);
            AssetProvider.RegisterToCurrentSession(_validGridKeys);
        }

        // --- Start --- 
        public async Awaitable StartAsync(Transform cameraTransform)
        {
            _templeteAOC = await AssetProvider.LoadAssetAsync<AnimatorOverrideController>(new AssetKey("aoc_field_unit"));
            _playerEntity = await SpawnFieldEntityAsync(1, _templeteAOC);
#if UNITY_EDITOR
            _playerEntity.transform.position = Vector3.back;
#endif

            _ = _mapManager.PlayStreamingAsync(cameraTransform, _validGridKeys);
        }
        private async Awaitable<FieldEntity> SpawnFieldEntityAsync(int index, AnimatorOverrideController baseAOC)
        {
            AssetKey                 prefabKey = new AssetKey(AssetConst.UNIT_PREFAB_FIELD);
            FieldUnitTableData       data      = FieldUnitTableProvider.GetData(index);
            FieldUnitAnimClipContext clip      = await data.GetAnimClipsAsync();

            FieldEntity fieldEntity = await AssetProvider.GetOrNewEntityInstanceAsync<FieldEntity>(prefabKey, _unitRoot);
            fieldEntity.Initialize(data, clip, baseAOC, _mapQueryService);

            return fieldEntity;
        }

        // --- Update ---
        public void Update(in InputState inputState)
        {
            // -- player -- 
            UnitIntent playerMoveIntent = Input2Intent(inputState);
            _playerEntity.UpdateIntent(in playerMoveIntent);

            // -- party --
            // (later)


            // -- npc --
            // (later)
        }
        public UnitIntent Input2Intent(in InputState inputState)
        {
            float x = 0f, z = 0f;
            if (inputState.IsPressing(IDxInput.RIGHT))  { x += 1f; }
            if (inputState.IsPressing(IDxInput.LEFT))   { x -= 1f; }
            if (inputState.IsPressing(IDxInput.UP))     { z += 1f; }
            if (inputState.IsPressing(IDxInput.DOWN))   { z -= 1f; }

            return new UnitIntent
            {
                MoveInput = new Vector2(x, z),
                AnimCommand = UnitAnimCmd.None,
            };
        }


        // public void Dispose()
        // {
        //     _mapManager.StopStreaming();
        //     
        //     if (_playerEntity)
        //     {
        //         _playerEntity.Clear();
        //         AssetProvider.ReleaseInstance(_playerEntity.Key, _playerEntity.gameObject);
        //         AssetProvider.ReleaseAsset(_templeteAOC.GetHashCode());
        //         _playerEntity = null;
        //     }
        // }
    }
}
