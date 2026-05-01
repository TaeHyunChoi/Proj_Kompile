# MapTileData

> **목적:** 이 문서는 `MapTileData.cs`의 개념, 데이터 구조, 비트 레이아웃, 아키텍처 위치, 연관 시스템 전체를 기록한다.
> 이후 필드 콘텐츠(월드 탐험) 및 Navigation 시스템 설계의 기반 문서로 사용된다.
> 연관 문서: [MapGridData.md](MapGridData.md) *(작성 예정)*

---

## 1. 개념 요약

`MapTileData`는 게임 월드를 구성하는 **한 개의 타일(1×1×1m 단위)에 대한 내비게이션 정보**를 담는 **불변 값 구조체**다.

런타임에서 타일은 다음 두 가지 용도로 사용된다.

- **이동 판정 / 높이 샘플링**: 캐릭터가 타일 위를 이동할 때 지형 높이를 계산하고, 서브타일 단위 충돌을 판정한다.
- **경로 탐색 (A\*)**: 인접 타일과의 연결 가능 여부와 높이 단차를 결정한다.
- **레이어 시각 제어**: `LayerMask` 값을 기반으로 동일 레이어 타일만 강조하고 나머지를 어둡게(dim) 처리한다.

```
파일 경로: Assets/Script/Map/Data/Table/MapTileData.cs
네임스페이스: Kompile.Map.Data
아키텍처 레이어: Data
직렬화 방식: MessagePack (바이너리)
```

---

## 2. 구조체 정의

```csharp
[MessagePackObject]
public struct MapTileData
{
    [ReadOnly, Key(0)] public long   NaviMask;   // 타일 내 13개 정점의 높이값 (4비트 × 13)
    [ReadOnly, Key(1)] public ushort LinkMask;   // 인접 8방향 높이 단차 (2비트 × 8)
    [ReadOnly, Key(2)] public ushort LayerMask;  // 렌더링 레이어 (에디터 RenderLayer와 1:1 대응)
}
```

- 모든 필드가 `[ReadOnly]`이며, 런타임에서 **값 변경이 불가능한 불변 구조체**다.
- 에디터 전용 생성자(`#if UNITY_EDITOR`, 파라미터: `naviMask`, `linkMask`, `layerMask`)를 통해서만 생성된다.
- 총 크기: `8 + 2 + 2 = 12바이트`

---

## 3. NaviMask (long, 64비트)

### 3.1 역할

타일 내부 **13개 서브 정점의 높이값**을 각 4비트에 패킹하여 저장한다.

- 유효 높이값: `0 ~ 14`
- 값 `15 (0b1111)`는 **무효(벽·통과불가)** 상태를 나타낸다.
- 총 사용 비트: `13 × 4 = 52비트` (bit[51:0])
- 에디터 베이크 시 레이어 값이 상위 비트(bit[63:52])에 추가로 패킹된다. 단, 이는 `LayerMask` 필드와 무관하다.

**출처:** `MapConsts.TOTAL_BITS = 13`, `MapConsts.BITS_PER_CELL = 4`, `MapNaviTileUtil.GetHeightFromNaviMask`, `EditMapTileJobUtil`

### 3.2 비트 레이아웃

`GetHeightFromNaviMask`의 추출 공식: `(naviMask >> (vIndex * 4)) & 0b1111`

```
비트 위치    정점 인덱스
[63:52]  →  에디터 layer 패킹 영역 (런타임 LayerMask 필드와 별개)
[51:48]  →  v12
[47:44]  →  v11
[43:40]  →  v10
[39:36]  →  v09
[35:32]  →  v08
[31:28]  →  v07
[27:24]  →  v06  (타일 중심)
[23:20]  →  v05
[19:16]  →  v04
[15:12]  →  v03
[11:8]   →  v02
[7:4]    →  v01
[3:0]    →  v00  (타일 내 (0,0) 정점)
```

### 3.3 정점 배치 (타일 내부 2D 그리드)

`MapConsts.VertexPositions` 기준. 좌표는 타일 내 0~1 정규화 (X, Z).

```
v10(0, 1) ─────── v11(0.5, 1) ─────── v12(1, 1)
    │                                      │
    │    v08(0.25, 0.75)  v09(0.75, 0.75) │
    │                                      │
v05(0, 0.5) ───── v06(0.5, 0.5) ───── v07(1, 0.5)
    │                                      │
    │    v03(0.25, 0.25)  v04(0.75, 0.25) │
    │                                      │
v00(0, 0) ──────── v01(0.5, 0) ─────── v02(1, 0)
```

| 정점 그룹 | 인덱스 | 설명 |
|-----------|--------|------|
| 테두리 모서리 | v00, v02, v10, v12 | 타일의 네 꼭짓점 |
| 테두리 엣지 중점 | v01, v05, v07, v11 | 각 변의 중점 |
| 쿼드 내부 중심 | v03, v04, v08, v09 | 4개 사분면의 중심 |
| 타일 중심 | v06 | 타일 전체의 중심 |

### 3.4 높이 계산 공식

**단일 정점 높이** (`UnitMoveComponent.HEIGHT_STEP = 0.125f`, `AStarBatchJobUtil.PATH_SEARCH_UNIT = 0.125f`):
```
정점 월드 Y = tileBaseY + heightValue × 0.125f
```

**캐릭터 실제 높이** (`UnitMoveComponent.SampleHeight` 기준):
```
1. tileBaseY = Mathf.Floor(pos.y)
2. localPos = (pos.x - Floor(pos.x), pos.z - Floor(pos.z))
3. 해당 localPos가 속하는 서브타일(삼각형) 탐색
4. 삼각형 3정점(v0, v1, v2)의 높이값 h0, h1, h2 추출
5. barycentric 보간: sampledHeight = (bary.x * h0 + bary.y * h1 + bary.z * h2) × 0.125f
6. return tileBaseY + sampledHeight
```

---

## 4. LinkMask (ushort, 16비트)

### 4.1 역할

현재 타일에서 **인접 8방향 타일로 이동할 때의 높이 단차(Y축 변화)**를 인코딩한다.

### 4.2 비트 레이아웃

`EditMapNaviTileUtil.GetLinkMaskShift` 및 `MapNaviTileUtil.TryGetYInt` 기준.
각 방향당 2비트, 총 8방향 × 2비트 = 16비트:

```
방향 인덱스    비트 위치    인코딩 값    의미
0 (SW, 좌하단)  [1:0]       0b01        평지 (동일 높이, yInt = 0)
                             0b10        위로 (yInt = +1)
                             0b11        아래로 (yInt = -1)
                             0b00        이동 불가
1 (S,  하)      [3:2]
2 (SE, 우하단)  [5:4]
3 (E,  우)      [7:6]
4 (NE, 우상단)  [9:8]
5 (N,  상)      [11:10]
6 (NW, 좌상단)  [13:12]
7 (W,  좌)      [15:14]
```

### 4.3 방향 인덱스 시각화

```
6(NW) ─ 5(N) ─ 4(NE)
  │      │      │
7(W)  ─ [현재] ─ 3(E)
  │      │      │
0(SW) ─ 1(S) ─ 2(SE)
```

---

## 5. LayerMask (ushort, 16비트)

에디터에서 타일에 지정한 **렌더링 레이어 번호**를 저장한다.

- 베이크 시 `EditMapTileComponent.RenderLayer`(ushort) 값을 그대로 복사한다.
- 런타임에서 `MapManager.UpdateLayerFromTileAsync(float3 playerWorldPos)`가 이 값을 읽어, 직전 값과 달라지면 `UpdateLayerVisibilityAsync(currentLayer)`를 호출한다.
  - 동일 `LayerMask` 값의 레이어: 정상 표시 (`_Color = white`)
  - 다른 `LayerMask` 값의 레이어: dim 처리 (`_Color = (0.1, 0.1, 0.1, 1)`)
  - dim 효과는 `WorldSpaceAtlasShader`의 `_Color` 프로퍼티를 `MaterialPropertyBlock`으로 제어한다.
- `NaviMask` 상위 비트([63:52])에 패킹되는 에디터 레이어 값(`EditMapTileJobUtil`)과는 별개의 독립 필드다.

---

## 6. 아키텍처 레이어 위치

```
Data Layer          MapTileData          ← 여기
                    MapGridData
                         ↑ 로드
Provider Layer      MapRepoProvider      (내비게이션용 NativeHashMap 캐싱)
Manager Layer       MapManager           (렌더링·스트리밍, TryGetTileData 제공)
                         ↑ 의존성 역전
Service Layer       IMapQueryService     (인터페이스)
                    FieldMapQueryService (MapManager 래핑 어댑터)
                         ↑ 주입
Component Layer     UnitMoveComponent    (실시간 이동·충돌·높이 샘플링)
                    MapUnitMoveComponent (경로 추종 이동)
                         ↑ 사용
Utility Layer       MapNaviTileUtil      (NaviMask 비트 연산, Burst)
                    MapCoordUtil         (좌표 ↔ ID 변환, Burst)
                    AStarBatchJobUtil    (A* 경로 탐색 Burst Job)
                    AStarPathfinderUtil  (Job 디스패처)
                    MapNaviSteeringUtil  (경로 추종 스티어링)
```

**MapManager와 MapRepoProvider는 독립적인 두 시스템이다.**
- `MapManager`: 그리드 스트리밍 및 렌더링 메시 생성. `Dictionary<int, MapGridData>`를 직접 보유하며 `TryGetTileData()`를 제공한다.
- `MapRepoProvider`: 내비게이션 전용 캐싱. `Dictionary<long, MapTileData>`와 `NativeHashMap<long, (long, long)>`을 보유하며, A* Job에 `NativeMap`을 제공한다.

---

## 7. 연관 파일 목록

| 파일 | 레이어 | 역할 |
|------|--------|------|
| `MapTileData.cs` | Data | 타일 1개의 내비게이션 데이터 |
| `MapGridData.cs` | Data | 64×64 타일 그룹 컨테이너 (`ConcurrentDictionary<int, MapTileData>`) |
| `MapGridLayerData.cs` | Data | 그리드의 렌더링 메시 에셋 경로 목록 |
| `MapConsts.cs` | Data/Definition | `TOTAL_BITS`, `BITS_PER_CELL`, `VertexPositions`, `SubTileVertexMap` |
| `MapRepoProvider.cs` | Provider | 내비게이션용 `_tileDict`, `_nativeMap` 캐싱, Addressables 로드 |
| `MapManager.cs` | Manager | 그리드 스트리밍·렌더링, `TryGetTileData()` 제공 |
| `IMapQueryService.cs` | Field/Data | `TryGetTileData(worldPos)` 인터페이스 |
| `FieldMapQueryService.cs` | Field/Data | `IMapQueryService` 구현체, `MapManager` 위임 |
| `UnitMoveComponent.cs` | Unit/Component | 실시간 이동 판정(`CheckWalkable`), 높이 샘플링(`SampleHeight`) |
| `MapUnitMoveComponent.cs` | Map/Entity | 사전 계산된 경로(`float3[]`) 추종 이동 |
| `MapNaviTileUtil.cs` | Utility | `IsSubTileValid`, `GetHeightFromNaviMask`, `IsCircleOverlappingSubTile` (Burst) |
| `MapCoordUtil.cs` | Utility | 월드 좌표 ↔ `gKey/tKey/long ID` 변환 (Burst) |
| `AStarBatchJobUtil.cs` | Utility | A* 경로 탐색 Burst Job (String Pulling 포함) |
| `AStarPathfinderUtil.cs` | Utility | `AStarBatchJobUtil` 디스패처 (`RequestPathsBatch`) |
| `MapNaviSteeringUtil.cs` | Utility | `CalculateSteering`, `GetSpriteDirection8` |
| **에디터** | | |
| `EditMapTileData.cs` | Editor/Data | 에디터용 타일 데이터 (`ID`, `NaviMask`, `LinkMask`, `RenderIndex`, `LayerMask`) |
| `EditMapGridData.cs` | Editor/Data | 에디터용 그리드 (`ParseData()`로 런타임 타입 변환) |
| `EditMapRepoProvider.cs` | Editor/Provider | 에디터 내 A* 테스트용 `NativeHashMap` 캐시 |
| `EditMapTileJobUtil.cs` | Editor/Utility | 높이값 4비트 패킹 → `NaviMask` 생성 Job |
| `EditMapNaviTileUtil.cs` | Editor/Utility | 에디터용 NaviMask 연산, 방향-비트 매핑 |
| `EditMapLinkJobUtil.cs` | Editor/Utility | `LinkMask` 계산 Job |
| `EditMapCoordUtil.cs` | Editor/Utility | 에디터용 좌표 계산 |

---

## 8. 런타임 데이터 흐름

### 8.1 맵 로딩 — MapManager (렌더링)

```
MapManager.PlayStreamingAsync(cameraTransform)
    ↓
StartGridStreamingLoopAsync()
    - 0.5초(CHECK_INTERVAL)마다 카메라 기준 수평 10f(PRELOAD_RADIUS) 내 gridKey 탐색
    - MapCoordUtil.ComputeGridKey(worldPos) → int gridKey
    ↓
LoadGridDataAsync(int gridKey)
    - Addressables key: $"MapNavi_{gridKey}"
    - AssetProvider.ReadBinaryDataAsync<MapGridData>(addressKey)
    - → MapGridData (MessagePack 역직렬화)
    - _mapGridDataDic[gridKey] = gridData
    ↓
CreateMapChunksAsync(gridKey, layerData)
    - gridData.layerMeshAssets 순회
    - Mesh, Material 로드 후 GameObject 조립
    - 3개마다 NextFrameAsync() (Time-Slicing)
```

### 8.2 맵 로딩 — MapRepoProvider (내비게이션)

```
MapRepoProvider.LoadGridDataAsync(int gridKey)
    - Addressables key: $"MapNavi_{gridKey}"
    - TextAsset 로드 → SerializeUtil.Deserialize<MapGridData>()
    ↓
Initialize(MapGridData grid)
    - foreach tile in grid.NaviTileDict:
        MapCoordUtil.ComputeID(gKey, tKey, out long id)
        _tileDict[id] = tile
        _nativeMap.TryAdd(id, (tile.NaviMask, tile.LinkMask))
```

### 8.3 캐릭터 실시간 이동 (UnitMoveComponent)

```
UnitMoveComponent.ManualUpdate()
    ↓
CheckWalkable(newPos)
    - 3×3 타일 범위(WALKABLE_RADIUS = 0.35f 기준) 순회
    - IMapQueryService.TryGetTileData(queryPos) → MapTileData
    - 16개 서브타일 순회:
        IsSubTileValid(tile.NaviMask, s) == false
        && IsCircleOverlappingSubTile(s, localCenter, radiusSq) == true
        → return false (이동 불가)
    ↓ (walkable인 경우)
SampleHeight(newPos)
    - IMapQueryService.TryGetTileData(queryPos) → MapTileData
    - tileBaseY = Mathf.Floor(pos.y)
    - localPos = (pos.x % 1, pos.z % 1)
    - SubTileVertexMap으로 해당 서브타일(삼각형) 탐색
    - 3정점 높이 추출 → 바리센트릭 보간
    - return tileBaseY + sampledHeight
    ↓
transform.position 업데이트
```

### 8.4 경로 탐색 (A*)

```
AStarPathfinderUtil.RequestPathsBatch(starts, ends, nativeMap)
    - nativeMap: MapRepoProvider.NativeMap (NativeHashMap<long, (NaviMask, LinkMask)>)
    ↓
AStarBatchJobUtil.Execute() [Burst, IJobParallelFor]
    - 8방향 NEIGHBOR_OFFSETS_INT 탐색
    - 타일 경계 이동 시: LinkMask에서 방향 2비트 추출
        0b01 → yVal = 0 (평지)
        0b10 → yVal = +1 (오름)
        0b11 → yVal = -1 (내림)
        0b00 → 이동 불가, skip
    - GetHeightFromNaviMask(naviMask, vIndex) → heightY
    - heightY == 0b1111이면 skip
    - 이동 비용: 직선 0.5f, 대각선 0.3535f
    - 휴리스틱: GetOctileDistance
    ↓
ApplyStringPulling() — 경로 평탄화
    - 가시성(Line-of-Sight) 기반 지름길 추출
    ↓
List<float3[]> 반환 → MapUnitMoveComponent.Initialize(smoothedPath)
```

---

## 9. 에디터 파이프라인 (MapTileData 생성 과정)

에디터에서 `MapTileData`를 굽는(bake) 과정.
`EditMapTileJobUtil`은 높이값 배킹을 직접 수행하지 않으며, 외부에서 계산된 `Height` 배열을 받아 NaviMask로 패킹하는 역할만 한다.

```
[외부] 씬 샘플링 → Height(ulong) 배열 준비
        ↓
EditMapTileJobUtil (IJobParallelFor)
    - 입력: SceneIndex, RenderLayer, Position, Height
    - layerMask = (ulong)layer << (TOTAL_BITS * BITS_PER_CELL)  // bit[63:52]
    - naviMask  = (long)(layerMask | heightMask)
    - → EditMapTileData { ID, NaviMask, LinkMask=default, RenderIndex }
        ↓
EditMapSamplingProvider.Bake() — EditMapTileData 조립
    - EditMapTileData.LayerMask = nativeRenderLayer[i]  ← RenderLayer를 직접 저장
        ↓
EditMapLinkJobUtil (IJobParallelFor)
    - 인접 타일 간 높이 단차 계산
    - → LinkMask 2비트 × 8방향 패킹
        ↓
EditMapGridData.ParseData()
    - foreach EditMapTileData:
        new MapTileData(NaviMask, LinkMask, LayerMask)
    - → ConcurrentDictionary<int, MapTileData>
        ↓
MapGridData { Key, NaviTileDict, layerMeshAssets }
        ↓
MessagePack 직렬화
        ↓
Addressables 에셋 "MapNavi_{gridKey}" 저장
```

---

## 10. 핵심 설계 원칙

### 10.1 비트 패킹 최소화

12바이트 구조체로 한 타일의 내비게이션 정보 전체를 표현한다.
대규모 맵에서 수만 개의 타일을 메모리에 올려야 하므로, 단일 타일의 크기를 최소화하는 것이 중요하다.

### 10.2 불변성 (Immutability)

런타임에서 타일 데이터는 절대 수정되지 않는다.
모든 변경은 에디터 베이크 과정에서 이루어지며, 런타임은 읽기 전용으로만 사용한다.

### 10.3 Burst 호환성

`MapTileData`는 `struct`이며, `long`과 `ushort`만을 포함한다.
Burst Compiler가 요구하는 Blittable 타입 조건을 완전히 충족하여, 모든 연산 유틸리티를 Burst Job으로 최적화할 수 있다.

### 10.4 의존성 역전 (DIP)

`UnitMoveComponent`(Component 레이어)는 `MapManager`(Manager 레이어)를 직접 참조하지 않는다.
`IMapQueryService` 인터페이스를 통해 의존성을 역전시킨다.
주입 체인: `FieldUnitManager → FieldPlayerEntity → UnitMoveComponent` (`FieldMapQueryService.cs` 주석 기준).

### 10.5 에디터/런타임 분리

- `EditMapTileData`: 에디터 전용. `ID`, `RenderIndex`, `LayerMask`를 포함하며, 베이크 과정의 중간 데이터로 사용된다.
- `MapTileData`: 런타임 전용. `ID` 없이 `NaviMask`/`LinkMask`/`LayerMask`를 포함한다.
- 변환은 `EditMapGridData.ParseData()`에서 명시적으로 이루어진다. (`LayerMask` 포함)

---

## 11. Field 콘텐츠 / Navigation 설계 시 고려사항

**높이 해상도**: 타일 내 높이 표현 단위는 `0.125m` (`HEIGHT_STEP`, `PATH_SEARCH_UNIT`). 유효 최대 heightValue = 14이므로, 한 타일 내 최대 높이 오프셋은 `14 × 0.125 = 1.75m`다.

**이동 불가 판정**: `NaviMask`의 4비트 값이 `15 (0b1111)`이면 해당 서브 정점은 통과 불가다. `UnitMoveComponent`는 서브타일 단위(16개 삼각형)로 충돌을 판정하며, 플레이어 반경 `WALKABLE_RADIUS = 0.35f`를 사용한다.

**A\* 이동 비용**: 직선 방향 `0.5f`, 대각선 방향 `0.3535f` (`AStarBatchJobUtil.GetMoveCost` 기준). 대각선이 직선보다 저렴하다.

**이동 컴포넌트 구분**: 실시간 플레이어 이동은 `UnitMoveComponent`(IMapQueryService 사용), 경로 추종 이동은 `MapUnitMoveComponent`(사전 계산된 `float3[]` 경로 사용)로 분리되어 있다.

**그리드 경계**: `GRID_SIZE = 64`이므로 한 `MapGridData`는 64×64×64 타일을 담는다. `MapCoordUtil`은 월드 좌표를 `gKey(int)`와 `tKey(int)`로 분리하여 그리드 경계를 투명하게 처리한다.

**동적 장애물**: `MapTileData`는 정적 지형 데이터만 담는다. 동적 장애물 처리는 현재 코드에 구현되어 있지 않다.
