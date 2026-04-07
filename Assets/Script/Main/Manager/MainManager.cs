namespace Script.Main
{
    using UnityEngine;
    using Script.Field.Manager;

    public class MainManager : MonoBehaviour
    {
        private FieldManager _fieldMgr;


        private Transform _mapRoot;
        private Transform _unitRoot;

        private Camera    _cam;


        public Transform MapRoot => _mapRoot;
        public Transform UnitRoot => _unitRoot;
        public Camera Cam => _cam;

#if UNITY_EDITOR
        private int _editLayerIndex = 0;
#endif

        private void Awake()
        {
            _cam = transform.GetComponentInChildren<Camera>();

            // scene을 건드리기 싫어서 동적 생성;
            _mapRoot = new GameObject("Map").transform;
            _mapRoot.SetParent(this.transform);
            _mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _unitRoot = new GameObject("Unit").transform;
            _unitRoot.SetParent(this.transform);
            _unitRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _fieldMgr = new FieldManager(this);
        }
        private async void Start()
        {
            await _fieldMgr.InitializeAsync(this);
        }


        private async void Update()
        {
#if UNITY_EDITOR
            if (true == Input.GetKeyDown(KeyCode.Space))
            {
                _editLayerIndex = (_editLayerIndex + 1) % 2;
                await _fieldMgr.UpdateLayerAsync(_editLayerIndex);
            }
#endif
        }
    }
}