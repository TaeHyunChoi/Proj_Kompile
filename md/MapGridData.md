# MapGridData

> **목적:** 이 문서는 `MapGridData.cs`의 개념, 데이터 구조, 키 체계, 아키텍처 위치, 연관 시스템 전체를 기록한다.
> 이후 필드 콘텐츠(월드 탐험) 및 Navigation 시스템 설계의 기반 문서로 사용된다.
> 연관 문서: [MapTileData.md](MapTileData.md)

---

## 1. 개념 요약

`MapGridData`는 **한 개의 그리드(64×64×64 타일 묶음)에 대한 내비게이션 데이터와 렌더링 에셋 정보**를 담는 직렬화 가능한 클래스다.

런타임에서 두 가지 용도로 사용된다.

- **내비게이션**: `NaviTileDict`에서 tileKey로 `MapTileData`를 조회하여 이동 판정·높이 샘플링·경로 탐색에 활용한다.
- **렌더링**: `layerMeshAssets`에서 레이어별 메시 에셋 주소를 로드하여 씬에 그리드 시각물을 생성한다.

```
파일 경로: Assets/Script/Map/Data/Table/MapGridData.cs
네임스페이스: Kompile.Map.Data
아키텍처 레이어: Data
직렬화 방식: MessagePack (바이너리)
Addressables 키: "MapNavi_{gridKey}"
```

---

## 2. 클래스 정의

```csharp
[MessagePackObject]
public class MapGridData
{
    [Key(0)] public int Key;
    [Key(1)] public ConcurrentDictionary<int, MapTileData> NaviTileDict;
    [Key(2)] public List<MapGridLayerData> layerMeshAssets;

    [SerializationConstructor]
    public MapGridData() { }

    public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
    {
        return NaviTileDict.TryGetValue(tileIntKey, out tileData);
    }
}
```

- `struct`가 아닌 **`class`** (참조 타입)다. `MapTileData`(struct)와 대비된다.
- `[SerializationConstructor]`가 붙은 기본 생성자는 **MessagePack 역직렬화 전용**이다.
- `ConcurrentDictionary`를 사용하는 이유는 에디터 베이크 시 **병렬 Job에서 동시 Write**가 발생하기 때문이다.

---

## 3. 필드 상세

### 3.1 Key (int) — gridKey

그리드의 고유 식별자. `MapCoordUtil.ComputeGridKey(float3 worldPos)`로 계산된다.

**구조** (`MapCoordUtil.ComputeGridKey` 기준):

```csharp
int gx = Mathf.FloorToInt(worldPos.x * GRID_SIZE_RECIP);  // GRID_SIZE = 64
int gy = Mathf.FloorToInt(worldPos.y * GRID_SIZE_RECIP);
int gz = Mathf.FloorToInt(worldPos.z * GRID_SIZE_RECIP);

byte bX = (byte)(sbyte)gx;
byte bY = (byte)(sbyte)gy;
byte bZ = (byte)(sbyte)gz;

return (bX << 16) | (bY << 8) | bZ;
```

| 비트 위치 | 의미 |
|-----------|------|
| [23:16] | 그리드 X 좌표 (`sbyte`로 캐스팅 → -128~127) |
| [15:8]  | 그리드 Y 좌표 |
| [7:0]   | 그리드 Z 좌표 |

- 각 축의 그리드 좌표는 `sbyte` 범위(-128~127) → 월드 커버리지: 축당 256 그리드 × 64 = **16,384 유닛**
- `MapGridData.Key`는 에디터 베이크 시 이 값으로 채워지며, Addressables 키 문자열 `$"MapNavi_{Key}"`와 1:1 대응한다.

### 3.2 NaviTileDict (ConcurrentDictionary\<int, MapTileData\>)

그리드 내 타일들의 내비게이션 데이터 컬렉션. key는 **tileKey(int)**, value는 `MapTileData`.

**tileKey 구조** (`MapCoordUtil.ComputeKey` 기준):

```csharp
outTKey = ((tX & TILE_MASK) << (TILE_BITS * 2))   // TILE_BITS = 6, TILE_MASK = 63
        | ((tY & TILE_MASK) << (TILE_BITS * 1))
        | ((tZ & TILE_MASK) << (TILE_BITS * 0));
```

| 비트 위치 | 의미 |
|-----------|------|
| [17:12] | 그리드 내 로컬 타일 X (0~63) |
| [11:6]  | 그리드 내 로컬 타일 Y (0~63) |
| [5:0]   | 그리드 내 로컬 타일 Z (0~63) |

- 타일 조회: `TryGetTileData(int tileIntKey, out MapTileData tileData)` — NaviTileDict.TryGetValue 래퍼
- 타일 개수는 그리드마다 다르다. 비어 있는(지형이 없는) 타일은 Dict에 존재하지 않는다.
- `MapTileData`의 상세 구조는 [MapTileData.md](MapTileData.md) 참조.

### 3.3 layerMeshAssets (List\<MapGridLayerData\>)

그리드의 렌더링 메시 에셋 정보. 레이어 인덱스별로 그룹화되어 있다.

```csharp
[MessagePackObject]
public class MapGridLayerData
{
    [Key(0), ReadOnly] public int layer;          // 레이어 인덱스
    [Key(1), ReadOnly] public List<string> assets; // Addressables 에셋 주소 목록
}
```

- `layer`: 레이어 인덱스(정수). `MapGridEntity.UpdateLayerVisibility`에서 `(targetLayerMask & (1 << layer)) != 0` 비트 검사로 사용된다.
- `assets`: 해당 레이어를 구성하는 메시 에셋들의 Addressables 주소 문자열 목록.
- 하나의 그리드는 여러 레이어(지면, 고원, 지하 등)를 가질 수 있으며, 각 레이어는 여러 메시 에셋으로 구성될 수 있다.

---

## 4. long ID — 타일의 전역 식별자

`MapRepoProvider`는 내비게이션 캐싱 시 `MapGridData.Key`(gKey)와 tileKey를 조합하여 **long ID**를 생성한다.

```csharp
// MapCoordUtil.ComputeID
outID = ((long)gKey << 32) | (uint)tKey;
```

| 비트 위치 | 의미 |
|-----------|------|
| [63:32] | gKey (그리드 식별자, 24비트 유효) |
| [31:0]  | tKey (타일 로컬 키, 18비트 유효) |

- `long ID`는 월드 전체에서 타일을 유일하게 식별하는 키다.
- `MapRepoProvider._tileDict[id]`, `MapRepoProvider._nativeMap[id]`의 키로 사용된다.
- `MapCoordUtil.ComputeWorldPositionInt(long id, out int3 outWorldPosInt)`로 역산하여 타일의 월드 좌표를 구할 수 있다.

---

## 5. 아키텍처 레이어 위치

```
Data Layer      MapGridData          ← 여기
                └── NaviTileDict: ConcurrentDictionary<int, MapTileData>
                └── layerMeshAssets: List<MapGridLayerData>
                         ↑ 로드 (각각 독립)
Provider Layer  MapRepoProvider      NaviTileDict → _tileDict, _nativeMap 캐싱
Manager Layer   MapManager           layerMeshAssets → 렌더링 메시 생성
                                     NaviTileDict → TryGetTileData() 제공
```

`MapGridData`는 내비게이션(NaviTileDict)과 렌더링(layerMeshAssets) 데이터를 **하나의 직렬화 단위**로 묶는다. 런타임에서 두 데이터를 소비하는 시스템(`MapRepoProvider`, `MapManager`)은 서로 독립적이다.

---

## 6. 런타임 컨텍스트 클래스

`MapGridData`(Data 레이어)와 구분되는 **런타임 상태 래퍼** 클래스들.

### 6.1 MapGridContext

```csharp
public class MapGridContext
{
    private const float GRID_SIZE = 64f;

    public int GridKey { get; private set; }
    public Vector3Int GridIndex { get; private set; }
    public MapGridData Data { get; private set; }
    public MapGridEntity VisualObject { get; private set; }
    public Bounds WorldBounds { get; private set; }
}
```

- 생성자 `MapGridContext(int gridKey, Vector3Int gridPivot)` — `CalculateBounds()` 호출
- `WorldBounds`: center = `GridIndex * 64 + 32`, size = `64 * Vector3.one`
- `SetData(MapGridData)`, `SetVisualObject(MapGridEntity)` — 로드 완료 후 주입
- `UpdateVisibility(bool isFrustumVisible, int targetLayerMask)` — 프러스텀 컬링 + 레이어 필터 동시 처리
- `Dispose()` — Data, VisualObject를 null로 해제

### 6.2 MapChunkContext

```csharp
public class MapChunkContext
{
    public int Layer;
    public GameObject Obj;
    public MeshRenderer Renderer;
    public Color StartColor;
    public Color TargetColor;
    public Color CurrentColor = Color.white;
}
```

- `MapManager.UpdateLayerVisibilityAsync`에서 레이어 페이드 애니메이션 상태를 보관하는 데 사용된다.
- `MapGridData.layerMeshAssets`의 에셋 하나하나가 런타임에서 `MapChunkContext` 인스턴스 하나에 대응한다.

### 6.3 MapGridEntity

```csharp
public class MapGridEntity : Entity
{
    private Dictionary<int, GameObject> _layerRoots;

    public void Initialize(Dictionary<int, List<Mesh>> layerGroups) { ... }
    public void UpdateLayerVisibility(int targetLayerMask) { ... }
    public void Dispose() { ... }
}
```

- `Initialize`: 레이어별 루트 GameObject 생성 → 메시 파츠를 자식으로 부착
- `UpdateLayerVisibility`: `(targetLayerMask & (1 << layerIndex)) != 0` 비트 검사로 레이어 루트 활성/비활성

---

## 7. 연관 파일 목록

| 파일 | 레이어 | 역할 |
|------|--------|------|
| `MapGridData.cs` | Data | **이 문서의 대상** |
| `MapGridLayerData.cs` | Data | 레이어별 메시 에셋 주소 목록 |
| `MapTileData.cs` | Data | 타일 1개의 내비게이션 데이터 (NaviTileDict의 Value) |
| `MapGridContext.cs` | Data/Context | 런타임 그리드 상태 래퍼 (WorldBounds, VisualObject 포함) |
| `MapChunkContext.cs` | Data/Context | 렌더링 청크 단위 상태 (Layer, Renderer, Color) |
| `MovementContext.cs` | Data/Context | 경로 추종 이동 상태 (Path, Velocity, MaxSpeed) |
| `MapCoordUtil.cs` | Utility | gKey, tKey, long ID 계산 (Burst) |
| `MapRepoProvider.cs` | Provider | NaviTileDict → `_tileDict` / `_nativeMap` 캐싱 |
| `MapManager.cs` | Manager | layerMeshAssets → 렌더링, NaviTileDict → TryGetTileData |
| `MapGridEntity.cs` | Entity | 레이어별 메시 GameObject 계층 관리 |
| **에디터** | | |
| `EditMapGridData.cs` | Editor/Data | 에디터용 그리드 (`ParseData()`로 런타임 타입 변환) |
| `EditMapTileData.cs` | Editor/Data | 에디터용 타일 (EditMapGridData.Data의 Value) |

---

## 8. 런타임 데이터 흐름

### 8.1 MapManager — 렌더링 파이프라인

```
MapManager.LoadGridDataAsync(int gridKey)
    - Addressables key: $"MapNavi_{gridKey}"
    - AssetProvider.ReadBinaryDataAsync<MapGridData>(addressKey)
    - _mapGridDataDic[gridKey] = gridData
    ↓
gridData.layerMeshAssets 순회
    - CreateMapChunksAsync(gridKey, layerData)
        - layerData.assets 순회 → Mesh, Material 로드
        - GameObject 조립 → MapChunkContext 생성
        - 3개마다 Awaitable.NextFrameAsync() (Time-Slicing)
    ↓
_activeGrids.Add(gridKey)
```

**타일 조회** (`MapManager.TryGetTileData`):
```csharp
MapCoordUtil.ComputeKey(worldPos, out int gKey, out int tKey);
_mapGridDataDic.TryGetValue(gKey, out MapGridData gridData);
gridData.TryGetTileData(tKey, out tileData);   // NaviTileDict.TryGetValue(tKey)
```

**그리드 스트리밍 조건** (`MapManager` 상수):
```
PRELOAD_RADIUS  = 10f   (로드 시작 수평 반경)
UNLOAD_RADIUS   = 20f   (언로드 시작 수평 반경)
CHECK_INTERVAL  = 0.5f  (스트리밍 검사 주기, 초)
Y 탐색 범위     = ±64f  (yRadius), 32f 간격 (yStep)
```

### 8.2 MapRepoProvider — 내비게이션 파이프라인

```
MapRepoProvider.LoadGridDataAsync(int gridKey)
    - Addressables key: $"MapNavi_{gridKey}"
    - TextAsset 로드 → SerializeUtil.Deserialize<MapGridData>()
    ↓
Initialize(MapGridData grid)
    foreach tile in grid.NaviTileDict:
        MapCoordUtil.ComputeID(gKey, tKey, out long id)
        _tileDict[id]   = tile
        _nativeMap[id]  = (tile.NaviMask, tile.LinkMask)
        MapCoordUtil.ComputeWorldPositionInt(id, out int3 absPivot)
        _posToID[absPivot] = id
```

- `_nativeMap`: `NativeHashMap<long, (long, long)>` — `AStarPathfinderUtil`이 Burst Job에 직접 전달하는 네이티브 내비게이션 맵

---

## 9. 에디터 파이프라인 (MapGridData 생성 과정)

```
EditMapGridData(int targetGridKey)  ← 에디터 툴에서 생성
    Data = new ConcurrentDictionary<int, EditMapTileData>()
    assetFiles = new List<string>()
    ↓
EditMapTileJobUtil / EditMapLinkJobUtil 결과를 TryAdd(key, EditMapTileData)로 적재
    ↓
AddMeshAsset(int layer, string fileName)
    - 동일 layer의 MapGridLayerData가 있으면 assets.Add(fileName)
    - 없으면 new MapGridLayerData(layer, fileName) 추가 → LayerMeshAssets
    ↓
EditMapGridData.ParseData()
    foreach kvp in Data:
        new MapTileData(kvp.Value.NaviMask, kvp.Value.LinkMask)
    → ConcurrentDictionary<int, MapTileData>
    ↓
new MapGridData
    {
        Key            = gridKey,
        NaviTileDict   = ParseData() 결과,
        layerMeshAssets = LayerMeshAssets
    }
    ↓
MessagePack 직렬화
    ↓
Addressables 에셋 "MapNavi_{gridKey}" 저장
```

**EditMapGridData vs MapGridData 대응:**

| EditMapGridData 필드 | MapGridData 필드 | 변환 |
|----------------------|-----------------|------|
| `gridKey (int)` | `Key (int)` | 그대로 |
| `Data: ConcurrentDictionary<int, EditMapTileData>` | `NaviTileDict: ConcurrentDictionary<int, MapTileData>` | `ParseData()` |
| `LayerMeshAssets: List<MapGridLayerData>` | `layerMeshAssets: List<MapGridLayerData>` | 그대로 |
| `assetFiles: List<string>` | (없음) | 에디터 전용, 런타임 미포함 |
