using Kompile.Asset.Provider;
using Kompile.Field.Manager;
using Kompile.Input.Provider;
using UnityEngine;
using static Kompile.Input.Data.Definition;

namespace Kompile
{
    /// <summary> 런타임 씬의 진입점. 콘텐츠 Manager를 생성·초기화하고 Update를 일괄 위임한다. </summary>
    public class MainManager : MonoBehaviour
    {
        // --- Input ---
        private IngameInputProvider _input;

        // --- Content Managers ---
        private FieldManager _fieldManager;

        // --- Root Transforms ---
        private Transform _fieldRoot;

        private void Awake()
        {
            enabled = false;
            _ = AwakeAsync();
        }
        private async Awaitable AwakeAsync()
        {
            await Initialize_Table();

            // Field 루트 오브젝트 생성
            GameObject fieldRootObj = new GameObject("Field");
            fieldRootObj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _fieldRoot = fieldRootObj.transform;
            
            // 입력 Provider 생성
            _input = new IngameInputProvider();

            // 콘텐츠 Manager 일괄 생성·초기화
            _fieldManager = new FieldManager(_fieldRoot);

            await Awaitable.NextFrameAsync();
            
            // 콘텐츠 시작 (Camera.main.transform을 스트리밍 앵커로 전달)
            _fieldManager.StartFieldAsync(Camera.main.transform);

            // Update() 호출 시작;
            enabled = true;
        }

        private async Awaitable Initialize_Table()
        {
            Awaitable unit = UnitTableProvider.InitializeAsync();
            //TODO: 추후 추가 예정
            
            await unit;
        }

        private void Update()
        {
            InputState state = _input.Current;
            //_fieldManager.Update(in state);
            _input.OnEndOfFrame();
        }

        private void OnDestroy()
        {
            _fieldManager.Dispose();
        }
    }
}
