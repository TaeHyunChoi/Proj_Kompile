namespace Script
{
    using UnityEngine;
    using Script.Field.Manager;

    public class Main : MonoBehaviour
    {
        private FieldManager _fieldMgr;



        private Transform _mapRoot;
        public Transform MapRoot => _mapRoot;

        private Camera _cam;
        public Camera Cam => _cam;

        private void Awake()
        {
            _cam = transform.GetComponentInChildren<Camera>();

            // scene을 건드리기 싫어서 동적 생성;
            _mapRoot = new GameObject("Map").transform;
            _mapRoot.SetParent(this.transform);
            _mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _fieldMgr = new FieldManager(this);
        }
        private async void Start()
        {
            await _fieldMgr.Map_InitializeAsync(_cam.transform);
        }

        // 다시 일괄 update()를 생각한다면...
        private int layer = 0;
        private void Update()
        {
            if (true == Input.GetKeyDown(KeyCode.Space))
            {
                layer = (layer + 1) % 2;
                _fieldMgr.UpdateLayer(layer);
            }
        }
    }
}