namespace Kompile
{
    using System;
    using System.Threading;
    using Kompile.Asset.Provider;
    using Kompile.Field.Manager;
    using Kompile.Input.Provider;
    using UnityEngine;
    using static Kompile.Input.Data.Definition;

    /// <summary> 런타임 씬의 진입점. 콘텐츠 Manager를 생성·초기화하고 Update를 일괄 위임한다. </summary>
    public sealed class MainManager : MonoBehaviour
    {
        // --- Input ---
        private IngameInputProvider _input;

        // --- Content Managers ---
        private FieldManager _fieldManager;

        // --- Root Transforms ---
        private Transform _fieldRoot;

        private void Awake()
        {
            // 초기화 완료 전까지 Update 실행 방지
            enabled = false;

            // Unity 2022.2 / 6000 표준 destroyCancellationToken 활용
            RunInitialization(this.destroyCancellationToken);
        }

        private async void RunInitialization(CancellationToken cancellationToken)
        {
            try
            {
                await AwakeAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[MainManager] 씬 전환 또는 오브젝트 파괴로 인해 비동기 초기화가 안전하게 취소되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainManager][Fatal Error] 초기화 중 예외 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Awaitable AwakeAsync(CancellationToken cancellationToken)
        {
            // 1. 데이터 테이블 초기화 (정상적인 병렬 구동 방식)
            await InitializeTablesAsync(cancellationToken);

            // 2. 에셋 세션 시작
            AssetProvider.BeginSession();

            // 3. Field 루트 오브젝트 생성
            GameObject fieldRootObj = new GameObject("Field");
            fieldRootObj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _fieldRoot = fieldRootObj.transform;

            // 4. 입력 Provider 생성 (Value-Centric)
            _input = new IngameInputProvider();

            // 5. 콘텐츠 Manager 생성 및 비동기 초기화 (Instance-Centric)
            _fieldManager = new FieldManager(_fieldRoot);
            await _fieldManager.AwakeAsync();
            cancellationToken.ThrowIfCancellationRequested();

            // 6. 프레임 동기화를 위해 한 프레임 대기 (매개변수 없는 순수 API)
            await Awaitable.NextFrameAsync();
            cancellationToken.ThrowIfCancellationRequested();

            // 7. 콘텐츠 콘텐츠 시작
            if (Camera.main != null)
            {
                await _fieldManager.StartAsync(Camera.main.transform);
            }
            else
            {
                Debug.LogWarning("[MainManager] Main Camera를 찾을 수 없습니다. _fieldRoot를 대체 앵커로 전달합니다.");
                await _fieldManager.StartAsync(_fieldRoot);
            }
            cancellationToken.ThrowIfCancellationRequested();

            // 8. 모든 준비가 끝나면 비로소 유니티 고유의 Update() 루프 개방
            enabled = true;
        }

        /// <summary> 복수의 Table 데이터 공급(Provider)을 6000 환경에 맞춰 병렬로 구동합니다. </summary>
        private async Awaitable InitializeTablesAsync(CancellationToken cancellationToken)
        {
            // 이 시점에 두 초기화 비동기 함수가 동시에 구동을 시작합니다.
            Awaitable fieldUnitTask = FieldUnitTableProvider.InitializeAsync();
            // Awaitable unitTask = UnitTableProvider.InitializeAsync(); // 추후 확장용

            // 순차적으로 await를 만나면서 두 작업이 모두 끝날 때까지 대기합니다 (할당 없는 병렬 대기).
            await fieldUnitTask;
            cancellationToken.ThrowIfCancellationRequested();

            // await unitTask;
            // cancellationToken.ThrowIfCancellationRequested();
        }

        private void Update()
        {
            // 구조적 일관성: 매 프레임 입력 데이터를 받아 Manager 인스턴스 레이어로 전파
            InputState state = _input.Current;

            _fieldManager.Update(in state);

            _input.OnEndOfFrame();
        }

        private void OnDestroy()
        {
            // 안전한 메모리 자원 해제
            // _fieldManager?.Dispose();
            AssetProvider.EndAndReleaseSession();
        }
    }
}