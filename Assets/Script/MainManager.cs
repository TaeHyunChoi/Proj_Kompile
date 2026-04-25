using Kompile.Field.Manager;
using UnityEngine;

namespace Kompile
{
    /// <summary>
    /// 런타임 씬의 진입점. 콘텐츠 Manager를 생성·초기화하고 Update를 일괄 위임한다.
    /// </summary>
    public class MainManager : MonoBehaviour
    {
        // --- Content Managers ---
        private FieldManager _fieldManager;

        // --- Root Transforms ---
        private Transform _fieldRoot;

        private void Awake()
        {
            // Field 루트 오브젝트 생성
            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _fieldRoot = fieldGo.transform;

            // 콘텐츠 Manager 일괄 생성·초기화
            _fieldManager = new FieldManager(_fieldRoot);
        }

        private void Start()
        {
            // 콘텐츠 시작 (Camera.main.transform을 스트리밍 앵커로 전달)
            _fieldManager.StartFieldAsync(Camera.main.transform);
        }

        private void Update()
        {
            // 하위 Manager Update 순차 호출 (호출 순서 고정)
            _fieldManager.Update();
        }

        private void OnDestroy()
        {
            _fieldManager.Dispose();
        }
    }
}
