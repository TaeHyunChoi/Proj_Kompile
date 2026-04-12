namespace Script.Main.Manager
{
    using UnityEngine;
    using System;
    using Script.Field.Data;
    using Script.Unit.Manager;
    using Script.Map.Manager;
    using Script.Asset.Provider;
    
    public class MainManager : MonoBehaviour
    {
        private Transform _mapRoot;
        private Transform _unitRoot;

        private Camera    _cam; // 얘도 _camManager;로 만들어야할텐데?
        
        private MapManager _mapManager;
        private UnitManager _unitManager;

        public Transform MapRoot => _mapRoot;
        public Transform UnitRoot => _unitRoot;
        public Camera Cam => _cam;


        
#if UNITY_EDITOR
        private int _editLayerIndex = 0;
#endif

        private async void Awake()
        {
            _cam = transform.GetComponentInChildren<Camera>();

            IMapQueryService dummy = new FieldMapQueryService(_mapManager);

            _ = InitializeAsync_DataTable();
            Initialize_Map();
            Initialize_Unit(dummy);
        }

        private void Start()
        {
            // settings for text;
            
            // _cam.SetPosition();

            _ = _mapManager.PlayStreamingAsync(_cam.transform);
            _ = _unitManager.SpawnUnitByIDAsync(1, Vector3.zero);
        }

        private async Awaitable InitializeAsync_DataTable()
        {
            try
            {
                await UnitTableProvider.InitializeAsync();
                // 테이블을 하나씩 추가
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private void Initialize_Map()
        {
            _mapRoot = new GameObject("Map").transform;
            _mapRoot.SetParent(this.transform);
            _mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            
            _mapManager = new MapManager(_mapRoot);
        }
        private void Initialize_Unit(IMapQueryService service)
        {
            _unitRoot = new GameObject("Unit").transform;
            _unitRoot.SetParent(this.transform);
            _unitRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            
            _unitManager = new UnitManager(_unitRoot, service);
        }
    }
}