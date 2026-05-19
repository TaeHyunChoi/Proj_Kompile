using Kompile.Field.Data;
using Kompile.Field.Entity;
using Kompile.Map.Manager;
using Kompile.Asset.Provider;
using Kompile.Asset.Data;
using Kompile.Unit.Data;
using static Kompile.Input.Data.Definition;

namespace Kompile.Field.Manager
{
    using UnityEngine;

    public class FieldManager
    {
        private readonly MapManager _mapManager;
        private readonly FieldMapQueryService _mapQueryService;

        private readonly Transform _fieldRoot;
        private readonly Transform _unitRoot;
        private FieldEntity _playerEntity;


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


        public async Awaitable StartFieldAsync(Transform cameraTransform)
        {
            _playerEntity = await SpawnFieldEntityAsync(0, UnitBrainType.Player);
#if UNITY_EDITOR
            _playerEntity.transform.position = Vector3.up;
#endif

            _ = _mapManager.PlayStreamingAsync(cameraTransform);
        }
        private async Awaitable<FieldEntity> SpawnFieldEntityAsync(int key, UnitBrainType brainType)
        {
            FieldEntity fieldEntity = await AssetProvider.GetOrNewEntityInstanceAsync<FieldEntity>(_unitRoot);
            await fieldEntity.InitializeAsync(key, brainType, _mapQueryService);

            return fieldEntity;
        }


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


        public void Dispose()
        {
            _mapManager.StopStreaming();

            if (_playerEntity)
            {
                _playerEntity.Clear();
                AssetProvider.ReleaseInstance(_playerEntity.Key, _playerEntity.gameObject);
                _playerEntity = null;
            }
        }
    }
}
