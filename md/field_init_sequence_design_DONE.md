# 필드 초기화 시퀀스 구현 설계

> **목적:** MainManager → FieldManager → 필드 생성(MapGridData 로드) 시퀀스의 구현 설계를 기록한다.  
> 이 문서는 코드 작성 전 설계 단계 문서다.  
> 기존 설계 문서(MainManager.md, FieldManager.md, MapGridData.md) 및  
> 직접 읽은 소스 파일(IMapQueryService.cs, FieldMapQueryService.cs, MapManager.cs)을 근거로 작성한다.  
> 신규 설계 항목은 **[설계]** 태그로 명시한다.
>
> 연관 문서: [MainManager.md](MainManager.md) · [FieldManager.md](FieldManager.md) · [MapGridData.md](MapGridData.md)

---

## 1. 구현 범위

이번 구현에서 다루는 범위는 아래와 같다.

- `MainManager.cs` 신규 생성
- `FieldManager.cs` 신규 생성
- `MapManager.cs` 수정: `DisposeAll()` 메서드 추가
- 테스트 기준 좌표 (0, 0, 0) 기반 필드(MapGridData) 로드 확인

**이번 구현에서 제외하는 항목**

- 카메라 실제 설정 (Camera.main 연동은 카메라 설정 이후에 반영)
- `FieldMapLayerService` (추후 구현)
- 유닛 생성 (FieldPlayerEntity 등)
- `BattleManager`, `IngameEventManager`
- `InputProvider` 연동

---

## 2. 확인된 네임스페이스 (소스 직접 확인)

| 파일 | 네임스페이스 | 확인 근거 |
|------|-------------|-----------|
| `MapManager.cs` | `Kompile.Map.Manager` | MapManager.cs 1행 |
| `IMapQueryService.cs` | `Kompile.Field.Data` | IMapQueryService.cs 1행 |
| `FieldMapQueryService.cs` | `Kompile.Field.Data` | FieldMapQueryService.cs 1행 |

---

## 3. 신규 생성 및 수정 파일 목록

| 파일 경로 | 작업 | 레이어 |
|-----------|------|--------|
| `Assets/Script/MainManager.cs` | **신규 생성** | MonoBehaviour 진입점 |
| `Assets/Script/Field/Manager/FieldManager.cs` | **신규 생성** | Manager |
| `Assets/Script/Map/Manager/MapManager.cs` | **수정**: `DisposeAll()` 추가 | Manager |

---

## 4. MainManager 구현 설계 [설계]

**근거:** MainManager.md §3 클래스 구조, §4 생명주기 시퀀스

### 4.1 파일 정보

```
파일 경로: Assets/Script/MainManager.cs
네임스페이스: Kompile
타입: MonoBehaviour
```

### 4.2 using 선언

```csharp
using UnityEngine;
using Kompile.Field.Manager;
```

### 4.3 필드 선언

```csharp
private FieldManager _fieldManager;
private Transform _fieldRoot;
```

### 4.4 Awake()

MainManager.md §4.1 기준:

```
MainManager.Awake()
    ↓
var fieldGo = new GameObject("Field")
fieldGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity)
_fieldRoot = fieldGo.transform
    ↓
_fieldManager = new FieldManager(_fieldRoot)
```

### 4.5 Start()

**[테스트 한정]**  
MainManager.md §4.2에서 `Camera.main.transform`을 전달하도록 설계되어 있으나,  
카메라 설정이 완료되지 않은 현재 단계에서는 씬에 배치한 테스트 카메라 오브젝트의 Transform을 사용한다.  
씬에 Camera 오브젝트를 배치하면 `Camera.main`으로 접근 가능하므로 아래와 같이 작성한다.

```
MainManager.Start()
    ↓
_fieldManager.StartFieldAsync(Camera.main.transform)
```

> 씬 구성 조건: Camera 오브젝트 position = (0, 5.27, -7.52), rotation = (35, 0, 0)  
> 카메라 설정 이후 이 코드는 그대로 유지된다 (교체 불필요).

### 4.6 Update()

MainManager.md §4.3 기준:

```
MainManager.Update()
    ↓
_fieldManager.Update()
```

### 4.7 OnDestroy()

MainManager.md §4.4 기준:

```
MainManager.OnDestroy()
    ↓
_fieldManager.Dispose()
```

---

## 5. FieldManager 구현 설계 [설계]

**근거:** FieldManager.md §4 클래스 구조, §5 초기화 시퀀스

### 5.1 파일 정보

```
파일 경로: Assets/Script/Field/Manager/FieldManager.cs
네임스페이스: Kompile.Field.Manager
타입: plain class (MonoBehaviour 아님)
```

### 5.2 using 선언

```csharp
using UnityEngine;
using Kompile.Map.Manager;
using Kompile.Field.Data;
```

### 5.3 필드 선언

실제 FieldManager.cs 확인 기준. `FieldMapLayerService`는 이번 범위 제외.

```csharp
// --- Sub-Managers ---
private MapManager _mapManager;

// --- Services ---
private FieldMapQueryService _mapQueryService;

// --- Root Transforms ---
private readonly Transform _fieldRoot;
private readonly Transform _mapRoot;

// --- State ---
private bool _isFieldActive;
```

### 5.4 생성자 (Constructor)

실제 FieldManager.cs 4~32행 기준:

```csharp
public FieldManager(Transform fieldRoot)
{
    _fieldRoot = fieldRoot;
    _mapRoot = new GameObject("Map").transform;
    _mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    _mapRoot.SetParent(fieldRoot);
    _mapManager = new MapManager(_mapRoot);
    _mapQueryService = new FieldMapQueryService(_mapManager);
    _isFieldActive = false;
}
```

> `_mapRoot.transform.SetPositionAndRotation` — `_mapRoot`가 이미 `Transform`이므로 `.transform`은 중복이지만 동작에 문제 없음. 기존 코드 유지.

### 5.5 StartFieldAsync(Transform cameraTransform)

실제 FieldManager.cs 35~39행 기준:

```csharp
public void StartFieldAsync(Transform cameraTransform)
{
    _isFieldActive = true;
    _ = _mapManager.PlayStreamingAsync(cameraTransform); // fire and forget
}
```

- `PlayStreamingAsync`: MapManager.cs 65행 `public async Awaitable PlayStreamingAsync(Transform cameraTransform)`

### 5.6 StopField()

실제 FieldManager.cs 40~44행 기준 (기존 코드 유지):

```csharp
public void StopField()
{
    _isFieldActive = false;
    _mapManager.StopStreaming();
}
```

### 5.7 Update() — 신규 추가 [설계]

현재 FieldManager.cs에 없음. MainManager.Update()에서 호출하므로 추가 필요.

```csharp
public void Update()
{
    // TODO: 하위 Manager·Service Update 호출 (향후 추가)
}
```

### 5.8 Dispose() — 수정 [설계]

실제 FieldManager.cs 45~48행 현재 상태:

```csharp
// 현재 코드
public void Dispose()
{
    _mapManager.StopStreaming();
}
```

`DisposeAll()` 추가 후 아래와 같이 수정:

```csharp
// 수정 후
public void Dispose()
{
    _mapManager.StopStreaming();
    _mapManager.DisposeAll();   // [설계] MapManager에 신규 추가 후 반영 (§6 참고)
    _isFieldActive = false;
}
```

### 5.9 UpdateMapLayerAsync() — 기존 코드 유지

실제 FieldManager.cs 51~55행. 빈 플레이스홀더. 이번 구현 범위에서 수정 없음.

```csharp
public async Awaitable UpdateMapLayerAsync()
{
    // TODO: MapTileData.LayerMask 기반 레이어 전환 (향후 구현)
}
```

### 5.10 프로퍼티

실제 FieldManager.cs 18행 기준:

```csharp
public IMapQueryService MapQueryService => _mapQueryService;
```

---

## 6. MapManager.cs 수정 설계 — DisposeAll() 추가 [설계]

**근거:** FieldManager.md §5.5, MapManager.cs 직접 확인 (현재 해당 메서드 없음)

### 6.1 추가 위치

MapManager.cs의 `// 시스템 제어 인터페이스` 섹션 (61~77행) 내,  
`StopStreaming()` 아래에 추가한다.

### 6.2 DisposeAll() 동작 설계

MapManager.cs에서 직접 확인한 내부 필드 목록을 기준으로 설계:

```
DisposeAll()
    ↓
_spawnedMapObjects 순회 (for 루프)
    각 gridKey의 List<MapChunkContext> 순회
        chunk.Obj가 null이 아니면 Object.Destroy(chunk.Obj)
    ↓
_spawnedMapObjects.Clear()
_mapGridDataDic.Clear()
_activeGrids.Clear()
_loadingGrids.Clear()
_invalidGrids.Clear()
_gridsToRemove.Clear()
_keepGrids.Clear()
_animatingChunksCache.Clear()
```

**MapManager.cs 내부 필드 목록 (직접 확인):**

| 필드 | 타입 | Clear 처리 |
|------|------|-----------|
| `_mapGridDataDic` | `Dictionary<int, MapGridData>` | Clear ✓ |
| `_spawnedMapObjects` | `Dictionary<int, List<MapChunkContext>>` | Destroy 후 Clear ✓ |
| `_activeGrids` | `HashSet<int>` | Clear ✓ |
| `_loadingGrids` | `HashSet<int>` | Clear ✓ |
| `_gridsToRemove` | `List<int>` | Clear ✓ |
| `_keepGrids` | `HashSet<int>` | Clear ✓ |
| `_invalidGrids` | `HashSet<int>` | Clear ✓ |
| `_animatingChunksCache` | `List<MapChunkContext>` | Clear ✓ |
| `_materialAddressCache` | `Dictionary<string, string>` | 유지 (주소 캐시, 재사용 가능) |
| `_gridKeyAddressCache` | `Dictionary<int, string>` | 유지 (주소 캐시, 재사용 가능) |

> `_materialAddressCache`, `_gridKeyAddressCache`는 주소 문자열 캐시이므로 해제 후 재시작 시에도 유효하다. Clear 하지 않는다.

---

## 7. 테스트 시나리오 — 카메라 (0, 5.27, -7.52) 기준 필드 로드

**근거:** FieldManager.md §5.6, MapGridData.md §3.1, MapManager.cs StartGridStreamingLoopAsync (83~165행)

### 7.1 씬 배치 조건

| 항목 | 설정값 |
|------|--------|
| `Main` 오브젝트 위치 | `(0, 0, 0)` |
| `MainManager.cs` 부착 | `Main` 오브젝트 |
| `Camera` 오브젝트 position | `(0, 5.27, -7.52)` |
| `Camera` 오브젝트 rotation | `(35, 0, 0)` |

### 7.2 GridKey 0 탐색 경로 계산

MapManager.cs `StartGridStreamingLoopAsync`는 step=5f 간격으로 카메라 주변을 탐색한다 (85~86행).

카메라 위치 `(0, 5.27, -7.52)` 기준, `x_offset=0, z_offset=10` 탐색 시:

```
world pos = (0 + 0, 5.27 + 0, -7.52 + 10) = (0, 5.27, 2.48)

gx = FloorToInt(0    / 64) = 0
gy = FloorToInt(5.27 / 64) = 0   (5.27/64 ≈ 0.082, floor = 0)
gz = FloorToInt(2.48 / 64) = 0   (2.48/64 ≈ 0.039, floor = 0)

gridKey = (0 << 16) | (0 << 8) | 0 = 0
Addressables 키: "MapNavi_0"
```

수평 거리 검사 (MapManager.cs 109행):
```
distSq = 0² + 10² = 100
preloadRadSq = 10² = 100
100 > 100 → false → 통과 ✓
```

→ `gridKey=0`이 탐색되어 `LoadGridDataAsync(0)` 호출된다.

### 7.3 실행 흐름

```
씬 시작
    ↓
MainManager.Awake()
    new GameObject("Field") → _fieldRoot @ (0,0,0)
    new FieldManager(_fieldRoot)
        └── new GameObject("Map") → _mapRoot @ (0,0,0), parent = _fieldRoot
        └── new MapManager(_mapRoot)
        └── new FieldMapQueryService(_mapManager)
        └── _isFieldActive = false
    ↓
MainManager.Start()
    _fieldManager.StartFieldAsync(Camera.main.transform)   ← Camera @ (0, 5.27, -7.52)
        └── _isFieldActive = true
        └── _ = _mapManager.PlayStreamingAsync(Camera.main.transform)
                ↓
            [약 0.5초 후 — CHECK_INTERVAL]
            StartGridStreamingLoopAsync()
                x_offset=0, z_offset=10, y_offset=0
                distSq=100 ≤ preloadRadSq=100 → 통과
                worldPos = (0, 5.27, 2.48) → gridKey=0
                _loadingGrids.Add(0) 성공
                _ = LoadGridDataAsync(0)   ← Fire and forget
                    ↓
                AssetProvider.ReadBinaryDataAsync<MapGridData>("MapNavi_0")
                → MapGridData 역직렬화 (NaviTileDict, layerMeshAssets)
                _mapGridDataDic[0] = gridData
                CreateMapChunksAsync(0, layerData) × layerMeshAssets.Count
                    └── 3개마다 Awaitable.NextFrameAsync() (Time-Slicing)
                _activeGrids.Add(0)
                _loadingGrids.Remove(0)
```

### 7.4 씬 Hierarchy 예상 결과

```
Main                          ← MainManager 부착
├── Field                     ← MainManager.Awake()에서 생성
│   └── Map                   ← FieldManager 생성자에서 생성
│       ├── [chunk obj 1]     ← CreateMapChunksAsync에서 생성
│       ├── [chunk obj 2]
│       └── ...
└── Camera                    ← 테스트용 카메라 오브젝트 (수동 배치)
```

### 7.5 확인 포인트

| 확인 항목 | 방법 |
|----------|------|
| `Field`, `Field/Map` 생성 여부 | Hierarchy에서 즉시 확인 (Awake 시점) |
| 청크 오브젝트 생성 여부 | `Map` 하위에 자식 오브젝트 생성 확인 (약 0.5초 후) |
| Addressables 로드 성공 여부 | Console 경고 없음 확인 (`[MapManager] Grid 0 로드 중 오류` 없어야 함) |
| gridKey=0 블랙리스트 등록 방지 | `MapNavi_0` 에셋이 Addressables에 등록되어 있어야 함 |

---

## 8. 구현 순서 요약

1. `MapManager.cs` 수정 → `DisposeAll()` 추가 (§6)
2. `FieldManager.cs` 신규 생성 (§5)
3. `MainManager.cs` 신규 생성 (§4)
4. 씬 구성: `Main` 오브젝트 배치 + `MainManager` 부착, `Camera` 오브젝트 배치 (§7.1)
5. Play 후 §7.5 확인 포인트 검증
