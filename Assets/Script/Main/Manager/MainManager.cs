using System;

namespace Script.Main
{
    using UnityEngine;
    using Script.Field.Data;
    using Script.Global.Unit.Manager;
    using Script.Map.Manager;
    
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

        private void Awake()
        {
            _cam = transform.GetComponentInChildren<Camera>();

            IMapQueryService dummy = new FieldMapQueryService(_mapManager);
            
            InitSetting_Map();
            InitSetting_Unit(dummy);
        }

        private void Start()
        {
            // _cam.SetPosition();

            _ = _mapManager.PlayStreamingAsync(_cam.transform);
            _ = _unitManager.SpawnUnitByIDAsync(1, Vector3.zero);
            // 여러 개를 비동기로 돌리는 방법이 있겠구나
        }

        // 정리 목적으로 함수로 나누어 작성..
        private void InitSetting_Map()
        {
            _mapRoot = new GameObject("Map").transform;
            _mapRoot.SetParent(this.transform);
            _mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            
            _mapManager = new MapManager(_mapRoot);
        }
        private void InitSetting_Unit(IMapQueryService servie)
        {
            _unitRoot = new GameObject("Unit").transform;
            _unitRoot.SetParent(this.transform);
            _unitRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            
            _unitManager = new UnitManager(_unitRoot, servie);
        }
    }
}