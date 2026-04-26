# FieldManager 프로그래밍 설계 문서

> **목적:** FieldManager 및 MapManager 연동을 통한 필드 맵 생성·스트리밍 시스템의 프로그래밍 설계를 기록한다.
> 신규 설계 항목은 **[설계]** 태그, 구현 완료 항목은 **[구현완료]** 태그로 명시한다.
>
> **구현 상태 (2026-04-26):** FieldManager.cs 구현 완료 (맵 스트리밍까지). FieldMapLayerService는 추후 구현.
>
> 연관 문서: [MapGridData.md](MapGridData.md) · [MapTileData.md](MapTileData.md)

---

## 1. 개요

**FieldManager**는 게임 필드 내의 모든 콘텐츠·기능을 일괄 조율하는 Manager 레이어 최상위 클래스다.

```
파일 경로: Assets/Script/Field/Manager/FieldManager.cs   [구현완료]
네임스페이스: Kompile.Field.Manager                      [구현완료]
아키텍처 레이어: Manager
타입: plain class (MonoBehaviour 아님)                   ← project-instructions.md Manager 레이어 규칙
```

**현재 설계 범위**: MapManager를 이용한 필드 맵 생성·스트리밍  
**향후 확장**: 인게임 이벤트 매니저, 기후 매니저 등  
**UnitEntity 생성**: 별도 UnitManager 없이 FieldManager가 `AssetProvider`를 통해 직접 생성·관리한다. (`AssetProvider`의 `Task` 반환 메서드는 향후 `Awaitable`로 교체 예정)

---

## 2. 아키텍처 위치

```
MonoBehaviour   MainManager                     (씬 진입점, Awake/Start 초기화, Update 일괄 호출) [구현완료]
                    ↓ 소유·초기화
Manager Layer   FieldManager                    ← 이 문서의 대상 [구현완료]
                ├── MapManager                  (Kompile.Map.Manager)
                ├── FieldMapQueryService        (Kompile.Field.Data) ← _mapManager 주입
                ├── (미구현) FieldMapLayerService
                └── (미설계) EventManager / WeatherManager ...
                         ↓ IMapQueryService 주입
Service Layer   IMapQueryService                (TryGetTileData 인터페이스)
                FieldMapQueryService            (MapManager 래핑 어댑터, Kompile.Field.Data)
                         ↓ 주입 체인 (FieldMapQueryService.cs XML 주석 기준)
                FieldManager → FieldPlayerEntity → UnitMoveComponent
                // UnitManager 없음. FieldManager가 AssetProvider로 직접 Entity 생성·소유
Component Layer UnitMoveComponent              (실시간 이동 판정·높이 샘플링)
```

**MainManager 역할 개요 [구현완료]**  
- MonoBehaviour를 상속받는 씬 진입점 클래스 (`Assets/Script/MainManager.cs`).  
- `Awake()`에서 FieldManager 등 주요 콘텐츠 Manager를 생성·초기화한다.  
- 각 콘텐츠 Manager는 MonoBehaviour를 상속받지 않으며, 개별 `Update()`를 갖지 않는다.  
  MainManager의 `Update()` 한 곳에서 모든 하위 Manager의 `Update()`를 **순차적으로** 호출한다.

---

## 3. MapManager 기존 API (MapManager.cs 기준)

FieldManager가 직접 사용하는 MapManager 퍼블릭 API.

### 3.1 생성자

```csharp
public MapManager(Transform root)
```

- `root`: MapManager가 내부에서 생성하는 모든 청크 `GameObject`의 부모 Transform.
  (`CreateMapChunksAsync` 내부: `chunkObj.transform.SetParent(_rootTransform)`)
- 내부 컨테이너 전체를 생성자에서 초기화한다. (`Dictionary`, `HashSet`, `List`, `MaterialPropertyBlock` 등)

### 3.2 스트리밍 제어

```csharp
public async Awaitable PlayStreamingAsync(Transform cameraTransform)
public void StopStreaming()
```

- `PlayStreamingAsync`: `_isStreamingActive = true` 설정 후 `StartGridStreamingLoopAsync` 실행.
  내부 루프는 `_isStreamingActive == false`가 될 때까지 `CHECK_INTERVAL(0.5초)` 마다 반복한다.
- `StopStreaming`: `_isStreamingActive = false` 설정. 루프는 다음 `await` 재개 시 종료된다.

### 3.3 레이어 시각 제어

```csharp
public async Awaitable UpdateLayerVisibilityAsync(int currentLayer, bool hideInsteadOfDim = false, float duration = 1.0f)
```

- 로드된 모든 그리드의 청크(`MapChunkContext`)를 대상으로 레이어 페이드 애니메이션을 실행한다.
- 내부 `_layerTransitionToken` 증가로 중첩 전환 자동 취소된다.
- 대상 레이어(`currentLayer`)의 청크: `Color.white`로 복원.
- 그 외 청크: `hideInsteadOfDim == false`이면 `Color(0.1, 0.1, 0.1, 1)` (Dim), `true`이면 `Color(0, 0, 0, 0)` 후 `Renderer.enabled = false`.

### 3.4 타일 데이터 접근

```csharp
public bool TryGetTileData(in float3 worldPos, out MapTileData tileData)
```

- 내부 동작: `MapCoordUtil.ComputeKey(worldPos, out int gKey, out int tKey)` → `_mapGridDataDic.TryGetValue(gKey)` → `gridData.TryGetTileData(tKey)`.
- 그리드가 로드되지 않았거나 타일이 없으면 `false` 반환.

### 3.5 스트리밍 상수 (MapManager.cs)

| 상수 | 값 | 설명 |
|------|----|------|
| `PRELOAD_RADIUS` | `10f` | 그리드 로드 시작 수평 반경 |
| `UNLOAD_RADIUS` | `20f` | 그리드 언로드 시작 수평 반경 (히스테리시스) |
| `CHECK_INTERVAL` | `0.5f` | 스트리밍 검사 주기 (초) |
| `GRID_SIZE` (local const) | `64f` | 그리드 한 변의 크기. 그리드 셀 열거 범위 계산에 사용 |
| `Y_RADIUS` (local const) | `64f` | Y축 탐색 범위 (±64) |

> `yStep`(32f)은 구버전 월드좌표 샘플링 방식에서 사용되었으나, 그리드 셀 직접 열거 방식으로 변경 후 사용하지 않음.

---

## 4. FieldManager 클래스 구조 [구현완료]

```csharp
using Kompile.Field.Data;
using Kompile.Map.Manager;

namespace Kompile.Field.Manager
{
    using UnityEngine;

    public class FieldManager
    {
        // --- Sub-Managers ---
        private MapManager _mapManager;
        // (미설계) private EventManager _eventManager;

        // --- Unit Entities (UnitManager 없음. FieldManager가 직접 소유·생명주기 관리) ---
        // (미설계) private FieldPlayerEntity _playerEntity;

        // --- Services ---
        private FieldMapQueryService _mapQueryService;  // IMapQueryService 주입용
        // (미구현) private FieldMapLayerService _mapLayerService;  ← 추후 구현

        // --- Root Transforms ---
        private readonly Transform _fieldRoot;  // 필드 씬 루트 (외부 주입)
        private readonly Transform _mapRoot;    // MapManager 전용 루트 (_fieldRoot 하위)
        // (미설계) private Transform _unitRoot;

        // --- State ---
        private bool _isFieldActive;

        // --- Constructor ---
        public FieldManager(Transform fieldRoot) { ... }

        // --- Life Cycle ---
        public void StartFieldAsync(Transform cameraTransform) { ... }  // void, Fire and forget
        public void StopField() { ... }
        public void Update() { ... }    // MainManager.Update()에서 호출. 현재 빈 메서드
        public void Dispose() { ... }   // StopStreaming → DisposeAll → _isFieldActive=false

        // --- Layer Control ---
        public async Awaitable UpdateMapLayerAsync() { ... }  // 빈 플레이스홀더, 추후 구현

        // --- Service Access (하위 시스템 주입용) ---
        public IMapQueryService MapQueryService => _mapQueryService;
    }
}
```

---

## 5. 초기화 시퀀스 [구현완료]

### 5.1 생성 (Constructor)

```
[MainManager.cs — Awake()]
new FieldManager(fieldRoot)
    ↓
FieldManager 생성자
    _fieldRoot = fieldRoot
    _mapRoot   = new GameObject("Map").transform
    _mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity)
    _mapRoot.SetParent(fieldRoot)
    _mapManager      = new MapManager(_mapRoot)
    _mapQueryService = new FieldMapQueryService(_mapManager)
    // FieldMapLayerService: 미구현 (추후 추가)
    _isFieldActive   = false
```

**`_mapRoot`**: `MapManager.CreateMapChunksAsync` 내부에서 `chunkObj.transform.SetParent(_rootTransform)`가 호출될 때 사용하는 부모 노드.  
씬 하이어라키에서 맵 청크 오브젝트를 `Map/` 하위로 격리하기 위해 별도 루트를 생성한다.

**`_fieldRoot`**: MainManager가 보유하는 Transform을 주입받는다. 씬 내 어느 계층에 배치할지는 MainManager에서 결정한다.

### 5.2 필드 시작 (StartFieldAsync)

```
FieldManager.StartFieldAsync(cameraTransform)   ← void 반환 (실제 코드 기준)
    ↓
_isFieldActive = true
    ↓
_ = _mapManager.PlayStreamingAsync(cameraTransform)   ← Fire and forget
    ↓ (비동기 루프 시작, CHECK_INTERVAL=0.5초 주기)
MapManager 내부: StartGridStreamingLoopAsync 실행
```

`PlayStreamingAsync`(MapManager.cs)는 내부에서 무한 루프(`while(_isStreamingActive && _cameraTransform)`)를 실행한다.
`StartFieldAsync`는 이를 `void` 메서드에서 `_ = ...` Fire and forget으로 호출하여 즉시 반환한다.

### 5.3 Update 흐름

```
MainManager.Update()
    ↓
FieldManager.Update()
    // 현재 빈 메서드. 향후 하위 Manager·Service Update 순차 호출 추가 예정
    // (미구현) _mapLayerService.CheckAndUpdateLayer(worldPos)  ← InputProvider 설계 시 확정
    // (미설계) _playerEntity?.Update()
    // (미설계) _eventManager.Update()
```

- 각 하위 Manager·Service는 MonoBehaviour를 상속받지 않으며 `Update()`를 Unity 엔진에 직접 등록하지 않는다.  
- 호출 순서는 FieldManager.Update() 내부에서 고정한다.

---

### 5.4 필드 종료 (StopField)

```
FieldManager.StopField()
    ↓
_isFieldActive = false
_mapManager.StopStreaming()   ← _isStreamingActive = false
    (루프는 다음 Awaitable.WaitForSecondsAsync 재개 시점에 종료)
```

### 5.5 Dispose() 흐름 [구현완료]

게임 종료 또는 재시작 시 호출한다. MapManager가 들고 있는 모든 에셋·GameObject를 즉시 해제하고 상태를 초기화한다.

```
FieldManager.Dispose()
    ↓
_mapManager.StopStreaming()    ← _isStreamingActive = false
    ↓
_mapManager.DisposeAll()       ← 청크 오브젝트 Object.Destroy + 내부 컨테이너 전체 Clear
    ↓
_isFieldActive = false
```

> `DisposeAll()`은 MapManager.cs에 구현 완료. `_spawnedMapObjects` 청크 전체 Destroy 후 8개 컨테이너 Clear.

---

### 5.6 원점(0, 0, 0) 포함 그리드의 초기 로드

스트리밍 루프는 카메라가 속한 그리드와 이웃 그리드를 직접 열거하고, 각 그리드 AABB ↔ 카메라 수평 최단 거리로 PRELOAD_RADIUS 판정한다.

카메라가 `(0, 5.27, -7.52)` 기준, gridKey=0 탐지 경로:
```
camGx = floor(0/64) = 0,  camGz = floor(-7.52/64) = -1

dx=0, dz=1 → gx=0, gz=0 (gridKey=0)
nearX = clamp(0, 0, 64)  = 0  → ddx = 0
nearZ = clamp(-7.52, 0, 64) = 0  → ddz = -7.52
distSq = 0 + 56.55 = 56.55  ≤  preloadRadSq(100)  → 로드 ✓
```
Addressables 키: `"MapNavi_0"`

---

## 6. 스트리밍 동작 (MapManager 위임, MapManager.cs 기준)

FieldManager는 스트리밍 로직을 직접 구현하지 않고 MapManager에 위임한다.
아래는 MapManager.cs에서 확인된 동작이다.

### 6.1 그리드 로드 조건

`StartGridStreamingLoopAsync` 내 탐색 로직 (MapManager.cs 직접 확인 기준):

1. 카메라 그리드 좌표 계산: `camGx = FloorToInt(camX / 64)`, `camGy`, `camGz` 동일
2. `keepRange = CeilToInt(UNLOAD_RADIUS / 64) + 1`, `yRange = CeilToInt(64 / 64) = 1` 산출
3. `dy(-1~+1)`, `dx(-keepRange~+keepRange)`, `dz(-keepRange~+keepRange)` 이웃 그리드 열거
4. 각 그리드 AABB ↔ 카메라 수평 최단 거리(`distSq`) 계산
5. `distSq > unloadRadSq` → 완전 무시
6. `distSq ≤ unloadRadSq` → `_keepGrids.Add()` (히스테리시스 유지)
7. `distSq > preloadRadSq` → 유지만, 신규 로드 안 함
8. `distSq ≤ preloadRadSq` → `LoadGridDataAsync()` Fire and forget

로드 완료 후: `_mapGridDataDic[gridKey] = gridData` → 청크 생성 → `_activeGrids.Add(gridKey)`

### 6.2 그리드 언로드 조건 (풀링 기반, [설계 변경])

- `_activeGrids`에 있으나 현재 루프의 `_keepGrids`에 없는 GridKey → `UnloadGridData` 호출

`UnloadGridData` 동작 (MapManager.cs — 변경 예정):
```
_mapGridDataDic.Remove(gridKey)
_activeGrids.Remove(gridKey)
    ↓
풀 잔여 수 < MAX_POOL_SIZE(10) ?
    ↓ Yes — 풀에 반환
    _spawnedMapObjects[gridKey]의 모든 청크: chunk.Obj.SetActive(false)
    chunk를 _chunkPool에 추가 (등록 순서 기록)
    ↓ No — 풀 초과 → 즉시 해제
    _spawnedMapObjects[gridKey]의 모든 청크: Object.Destroy(chunk.Obj)

_spawnedMapObjects.Remove(gridKey)
```

**풀 재사용 (그리드 재로드 시)**:
```
LoadGridDataAsync → 풀에 동일 구조의 청크가 있으면 꺼내서 SetActive(true) → 초기화 후 재배치
```

**풀 크기 정책**:
| 항목 | 값 |
|------|-----|
| `MAX_POOL_SIZE` | 10 (확정) |
| 풀링 대상 | 가장 최근에 반환된 청크 순으로 유지 |
| 초과분 | `Object.Destroy`로 즉시 해제 |

> **변경 이유**: 동일 그리드를 짧은 시간 내 재방문 시 `Instantiate` 비용 절감.  
> 단, 무한정 보유는 메모리 낭비이므로 최근 10–20개로 상한을 둔다.

### 6.3 로드 실패 처리 (블랙리스트)

`LoadGridDataAsync` 내부:
- Addressables에서 `null` 반환 시 → `_invalidGrids.Add(gridKey)` 영구 등록
- 이후 스트리밍 루프에서 해당 GridKey는 `_invalidGrids.Contains()` 검사로 즉시 스킵

### 6.4 Time-Slicing (메인 스레드 보호)

`CreateMapChunksAsync` 내부: 청크 3개 생성마다 `await Awaitable.NextFrameAsync()` 호출.
대규모 그리드 로드 시에도 메인 스레드가 단일 프레임에 과도하게 점유되지 않는다.

---

## 7. 레이어 시각 제어 — FieldMapLayerService [설계]

레이어 감지 및 전환 호출은 `FieldManager`가 직접 담당하지 않고 `FieldMapLayerService`에 위임한다.

### 7.1 설계 의도

`MapTileData.LayerMask`(ushort) 값이 이전 값과 달라지는 시점을 감지하여 MapManager의 레이어 페이드를 트리거한다.

사용 시나리오: 플레이어가 건물 밖(LayerMask = A)에서 건물 안(LayerMask = B)으로 이동  
→ LayerMask 변화 감지 → `MapManager.UpdateLayerVisibilityAsync()` 호출 → 페이드 인/아웃 실행

> **주의**: `MapTileData.LayerMask`(ushort)는 현재 항상 `0`으로 초기화된 미구현 필드다 (MapTileData.md 기준).  
> `LayerMask` 값과 `MapManager.UpdateLayerVisibilityAsync(int currentLayer)`의 `int currentLayer` 간 변환 방식은 미결정이다. (→ 항목 10 참고)

### 7.2 FieldMapLayerService 구조 [설계]

```
파일 경로: Assets/Script/Field/Data/FieldMapLayerService.cs   [설계]
네임스페이스: Kompile.Field.Data                              [설계]
아키텍처 레이어: Field/Data (Service)
```

```csharp
public class FieldMapLayerService
{
    private readonly MapManager      _mapManager;       // UpdateLayerVisibilityAsync 호출
    private readonly IMapQueryService _mapQueryService;  // MapTileData.LayerMask 조회

    private ushort _previousLayerMask;

    public FieldMapLayerService(MapManager mapManager, IMapQueryService mapQueryService)
    {
        _mapManager      = mapManager;
        _mapQueryService = mapQueryService;
        _previousLayerMask = 0;
    }

    // 위치 변화가 감지될 때에만 호출. 호출자는 InputProvider 또는 이동 처리 시스템 — 입력 설계 시 확정   [설계]
    public void CheckAndUpdateLayer(in float3 worldPos) { ... }
}
```

### 7.3 레이어 감지 흐름 [설계]

```
CheckAndUpdateLayer(worldPos)
    ↓
_mapQueryService.TryGetTileData(worldPos, out MapTileData tile)
    ↓ (성공 시)
currentLayerMask = tile.LayerMask
    ↓
currentLayerMask != _previousLayerMask ?
    ↓ Yes
_previousLayerMask = currentLayerMask
_ = _mapManager.UpdateLayerVisibilityAsync(currentLayer, ...)  ← Fire and forget
```

### 7.4 MapManager.UpdateLayerVisibilityAsync 동작 (MapManager.cs 기준)

`FieldMapLayerService`가 최종적으로 호출하는 MapManager API:

```csharp
public async Awaitable UpdateLayerVisibilityAsync(int currentLayer, bool hideInsteadOfDim = false, float duration = 1.0f)
```

- 내부 `_layerTransitionToken` 증가 → 이전 전환 자동 취소
- 평탄화된 `_animatingChunksCache` 리스트 순회로 매 프레임 Dictionary 순회 비용 제거
- `MaterialPropertyBlock`(`_Color`)으로 색상 보간
- 대상 레이어(`currentLayer`) 청크: `Color.white` 복원
- 그 외 청크: `hideInsteadOfDim == false` → Dim(`Color(0.1, 0.1, 0.1, 1)`), `true` → `Renderer.enabled = false`

---

## 8. 타일 데이터 접근 (IMapQueryService 연동)

### 8.1 FieldMapQueryService 구조 (FieldMapQueryService.cs 기준)

```
파일 경로: Assets/Script/Field/Data/FieldMapQueryService.cs
네임스페이스: Kompile.Field.Data
구현 인터페이스: IMapQueryService
```

```csharp
public class FieldMapQueryService : IMapQueryService
{
    private readonly MapManager _mapManager;

    public FieldMapQueryService(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    public bool TryGetTileData(in float3 worldPos, out MapTileData tileData)
    {
        return _mapManager.TryGetTileData(worldPos, out tileData);
    }
}
```

### 8.2 호출 경로

```
IMapQueryService.TryGetTileData(worldPos)          ← UnitMoveComponent 호출
    ↓ (FieldMapQueryService 구현)
_mapManager.TryGetTileData(worldPos, out tileData)  ← MapManager 위임
    ↓ (MapManager.cs 내부)
MapCoordUtil.ComputeKey → _mapGridDataDic.TryGetValue → NaviTileDict.TryGetValue
```

### 8.3 주입 체인 (FieldMapQueryService.cs XML 주석 기준)

```
FieldManager._mapQueryService (FieldMapQueryService)
    ↓ 주입 (FieldUnitManager 제거 — FieldManager가 직접 Entity에 주입)
FieldPlayerEntity
    ↓ 주입
UnitMoveComponent
```

FieldManager가 `AssetProvider`로 `FieldPlayerEntity`를 생성한 뒤 `_mapQueryService`를 직접 주입한다.
`IMapQueryService MapQueryService` 프로퍼티는 향후 다른 시스템에서 조회가 필요할 경우를 위해 유지한다. [설계]

---

## 9. 향후 확장 구조 [설계]

```
FieldManager
├── MapManager                        (현재 설계 범위)
├── (미설계) FieldPlayerEntity        ← AssetProvider로 직접 생성·소유 (UnitManager 없음)
├── (미설계) EventManager
└── (미설계) WeatherManager
```

UnitEntity는 별도 UnitManager 없이 FieldManager가 `AssetProvider.GetOrNewInstanceAsync<T>()` 를 통해
직접 생성하고 생명주기를 소유한다. Dispose 시 `AssetProvider.ReleaseInstance<T>()` 로 해제한다.

각 하위 Manager·Service는 FieldManager 생성자 또는 별도 `InitializeXxx` 메서드에서 생성·초기화한다.
구체적인 구조는 각 시스템 설계 시 결정한다.

---

## 10. 미결정 사항

| 항목 | 내용 |
|------|------|
| LayerMask → currentLayer 변환 | `MapTileData.LayerMask`(ushort, 현재 항상 0)에서 `UpdateLayerVisibilityAsync(int currentLayer)`의 `int`로 변환하는 방식 미결정. `LayerMask` 구현 시 함께 정의 필요 |
| CheckAndUpdateLayer 호출자 | 위치 변화 감지 시에만 호출하는 것으로 확정. **호출자(InputProvider 또는 이동 처리 시스템)는 InputProvider 설계 시 확정 필요** |

---

## 11. 연관 파일 목록

| 파일 | 레이어 | 역할 |
|------|--------|------|
| `MainManager.cs` | MonoBehaviour | 씬 진입점. FieldManager 등 주요 Manager 소유·초기화, 일괄 Update 호출 [설계] |
| `FieldManager.cs` | Manager | **이 문서의 설계 대상 (신규)** |
| `MapManager.cs` | Manager | 그리드 스트리밍·렌더링·레이어 제어, `TryGetTileData` 제공 |
| `MapGridData.cs` | Data/Table | 64×64×64 타일 묶음, `NaviTileDict` + `layerMeshAssets` |
| `MapTileData.cs` | Data/Table | 타일 1개의 내비게이션 데이터 (`NaviMask`, `LinkMask`) |
| `MapChunkContext.cs` | Data/Context | 렌더링 청크 상태 (`Layer`, `Renderer`, `CurrentColor` 등) |
| `MapCoordUtil.cs` | Utility | `gridKey`, `tileKey` 좌표 계산 (Burst) |
| `IMapQueryService.cs` | Field/Data | `TryGetTileData(worldPos)` 인터페이스 |
| `FieldMapQueryService.cs` | Field/Data (`Kompile.Field.Data`) | `IMapQueryService` 구현체. 생성자에서 `MapManager`를 직접 주입받아 `TryGetTileData` 위임 |
| `FieldMapLayerService.cs` | Field/Data (`Kompile.Field.Data`) | **신규 [설계]** `MapTileData.LayerMask` 변화 감지 → `MapManager.UpdateLayerVisibilityAsync` 호출 |
| `AssetProvider.cs` | Asset/Provider (`Kompile.Asset.Provider`) | GameObject 인스턴스 생성·풀링·해제. `GetOrNewInstanceAsync<T>()` 로 UnitEntity 생성 (`Task` 반환 → 향후 `Awaitable` 교체 예정) |
