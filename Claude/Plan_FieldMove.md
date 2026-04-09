# FieldPlayerEntity 8방향 이동 구현 — 완료 보고서

> **상태**: 구현 완료  
> **구현 기준 커밋**: Claude 작업 세션 (2026-04-10)

---

## 목표 요약

`FieldPlayerEntity`가 `MapGridData` 위에서 8방향 이동.

- **Height 반영**: `NaviMask` 서브타일 바리센트릭 보간 → `transform.position.y` 계산  
- **이동 불가 판정**: 반경 `0.35f` 내 서브타일 유효성(`IsSubTileValid`) 검사로 벽·구멍 차단

---

## 파일 변경 목록

### 신규 생성

| 경로 | 역할 |
|------|------|
| `Script/Field/Data/IMapQueryService.cs` | 맵 타일 조회 인터페이스 |
| `Script/Field/Data/FieldMapQueryService.cs` | `MapManager`를 감싸는 어댑터 구현체 |

### 수정

| 경로 | 변경 내용 |
|------|-----------|
| `Script/Map/Manager/MapManager.cs` | `TryGetTileData(float3, out MapTileData)` 공개 메서드 추가 |
| `Script/Field/Manager/FieldManager.cs` | 생성자에 `IMapQueryService` 파라미터 추가, 스폰 시 `FieldPlayerEntity`에 주입 |
| `Script/Field/Entity/FieldPlayerEntity.cs` | `SetMapQuery()`, `SetMoveInput()` 래퍼 추가 |
| `Script/Global/Unit/Entity/UnitMoveComponent.cs` | `Initialize(owner, mapQuery)` 시그니처 변경, `CheckWalkable` + `SampleHeight` 구현 |
| `Script/Global/Unit/Entity/Brain/PlayerControlBrain.cs` | `IngameInputProvider` 내부 생성, 8방향 입력 처리 구현 |
| `Script/Global/Unit/Entity/Brain/UnitEntityBase.cs` | `SetBrain`/`Clear`의 `_brain.Clear()` → `_brain?.Clear()` null 안전 패치 |

---

## 실행 흐름 (매 프레임)

```
FieldUnitManager.UpdateAllUnitsLogic()
  └─ FieldPlayerEntity.ManualUpdate()
       ├─ PlayerControlBrain.ManualUpdate()
       │    ├─ IngameInputProvider.Current → InputState
       │    ├─ LEFT/RIGHT/UP/DOWN 플래그 → Vector2(x, z)
       │    ├─ FieldPlayerEntity.SetMoveInput(Vector2)
       │    └─ IngameInputProvider.OnEndOfFrame()
       ├─ UnitMoveComponent.ManualUpdate()
       │    ├─ 대각선 정규화: magnitude > 1 → normalized
       │    ├─ candidatePos = currentPos + dir * MOVE_SPEED * deltaTime
       │    ├─ CheckWalkable(candidatePos) → false면 이동 중단
       │    ├─ SampleHeight(candidatePos) → newY
       │    └─ transform.position = (candidateX, newY, candidateZ)
       └─ UnitAnimComponent.ManualUpdate()
```

---

## 인터페이스 / 핵심 API

### IMapQueryService

```csharp
namespace Script.Field.Data
{
    using Script.Map.Data;
    using Unity.Mathematics;

    public interface IMapQueryService
    {
        bool TryGetTileData(in float3 worldPos, out MapTileData tileData);
    }
}
```

### FieldMapQueryService

```csharp
namespace Script.Field.Data
{
    using Script.Map.Manager;
    using Script.Map.Data;
    using Unity.Mathematics;

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
}
```

### MapManager — 추가 메서드

```csharp
// Script/Map/Manager/MapManager.cs 내부
public bool TryGetTileData(in Unity.Mathematics.float3 worldPos, out MapTileData tileData)
{
    MapCoordUtil.ComputeKey(worldPos, out int gKey, out int tKey);
    if (_mapGridDataDic.TryGetValue(gKey, out MapGridData gridData))
    {
        return gridData.TryGetTileData(tKey, out tileData);
    }
    tileData = default;
    return false;
}
```

### FieldUnitManager 생성자 변경

```csharp
// 외부(MainManager 등)에서 호출 시:
var mapQueryService = new FieldMapQueryService(mapManager);
var fieldUnitManager = new FieldUnitManager(unitRoot, mapQueryService);
```

```csharp
// FieldUnitManager.SpawnUnitInternalAsync 내부:
entity.Initialize(newId, newContext);

if (entity is FieldPlayerEntity fieldPlayer)
{
    fieldPlayer.SetMapQuery(_mapQueryService);   // ← 4. IMapQueryService 주입
}

AttachSpecificBrain(entity, brainType);          // ← 5. Brain 조립
```

### FieldPlayerEntity 추가 메서드

```csharp
/// FieldUnitManager에서 스폰 직후 호출
public void SetMapQuery(IMapQueryService mapQuery)
{
    _moveComponent.Initialize(this, mapQuery);
}

/// PlayerControlBrain에서 호출
public void SetMoveInput(Vector2 input)
{
    _moveComponent.SetMoveInput(input);
}
```

### UnitMoveComponent 핵심 상수 및 메서드

```csharp
private const float MOVE_SPEED      = 4f;
private const float WALKABLE_RADIUS = 0.35f;

/// HEIGHT_STEP 확정값:
/// AStarBatchJobUtil.PATH_SEARCH_UNIT = 0.125f 와 동일.
/// groundY = tileBaseY + heightValue * HEIGHT_STEP 공식 일치 확인.
private const float HEIGHT_STEP     = 0.125f;
```

**CheckWalkable** — IsSubTileValid 기반 서브타일 충돌 판정

```csharp
private bool CheckWalkable(Vector3 pos)
{
    if (_mapQuery == null) return true;

    float2 playerXZ = new float2(pos.x, pos.z);
    float radiusSq  = WALKABLE_RADIUS * WALKABLE_RADIUS;

    int tileX = Mathf.FloorToInt(pos.x);
    int tileZ = Mathf.FloorToInt(pos.z);

    for (int dx = -1; dx <= 1; dx++)
    for (int dz = -1; dz <= 1; dz++)
    {
        float3 queryPos = new float3(tileX + dx + 0.5f, pos.y, tileZ + dz + 0.5f);
        if (!_mapQuery.TryGetTileData(queryPos, out MapTileData tile)) continue;

        float2 localCenter = playerXZ - new float2(tileX + dx, tileZ + dz);

        for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
        {
            if (!MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s) &&
                 MapNaviTileUtil.IsCircleOverlappingSubTile(s, localCenter, radiusSq))
                return false;
        }
    }
    return true;
}
```

**SampleHeight** — 바리센트릭 보간

```csharp
private float SampleHeight(Vector3 pos)
{
    if (_mapQuery == null) return pos.y;

    float3 queryPos = new float3(pos.x, pos.y, pos.z);
    if (!_mapQuery.TryGetTileData(queryPos, out MapTileData tile)) return pos.y;

    float tileBaseY = Mathf.Floor(pos.y);
    float2 localPos = new float2(pos.x - Mathf.Floor(pos.x),
                                 pos.z - Mathf.Floor(pos.z));

    for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
    {
        if (!MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s)) continue;

        int v0 = MapConsts.SubTileVertexMap[s * 3];
        int v1 = MapConsts.SubTileVertexMap[s * 3 + 1];
        int v2 = MapConsts.SubTileVertexMap[s * 3 + 2];

        float2 p0 = MapConsts.VertexPositions[v0];
        float2 p1 = MapConsts.VertexPositions[v1];
        float2 p2 = MapConsts.VertexPositions[v2];

        if (!IsPointInTriangle(localPos, p0, p1, p2)) continue;

        int h0 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v0);
        int h1 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v1);
        int h2 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v2);

        float3 bary          = BarycentricCoords(localPos, p0, p1, p2);
        float  sampledHeight = (bary.x * h0 + bary.y * h1 + bary.z * h2) * HEIGHT_STEP;

        return tileBaseY + sampledHeight;
    }
    return pos.y;
}
```

### PlayerControlBrain 요점

```csharp
public void Initialize(UnitEntityBase entity)
{
    _playerEntity = entity as FieldPlayerEntity;
    _input        = new IngameInputProvider();   // 내부 생성
}

public void ManualUpdate()
{
    InputState state = _input.Current;

    float x = 0f, z = 0f;
    if (state.IsPressing(IDxInput.RIGHT)) x += 1f;
    if (state.IsPressing(IDxInput.LEFT))  x -= 1f;
    if (state.IsPressing(IDxInput.UP))    z += 1f;
    if (state.IsPressing(IDxInput.DOWN))  z -= 1f;

    _playerEntity.SetMoveInput(new Vector2(x, z));
    _input.OnEndOfFrame();
}
```

---

## 원래 계획과 달라진 점

| 항목 | 원래 계획 | 실제 구현 |
|------|-----------|-----------|
| `IMapQueryService` 메서드명 | `TryGetTile` | `TryGetTileData` (`MapGridData.TryGetTileData`와 명칭 통일) |
| `FieldMapQueryService` 의존 | `Dictionary<int,MapGridData>` 직접 참조 | `MapManager` 참조 (공개 메서드 추가 방식) |
| `SetMoveInput` 파라미터 | `Vector3` | `Vector2` (XZ 평면, Y는 SampleHeight가 결정) |
| `CheckWalkable` 판정 기준 | `LinkMask + TryGetYInt` | `IsSubTileValid + IsCircleOverlappingSubTile` (서브타일 레벨 직접 판정) |
| `HEIGHT_STEP` | 추정 0.125f | **확정 0.125f** (AStarBatchJobUtil.PATH_SEARCH_UNIT 코드에서 직접 확인) |
| `UnitEntityBase` | 변경 없음 | `_brain?.Clear()` null 안전 패치 추가 (스폰 첫 호출 NRE 방지) |

---

## 다음 작업에서 고려할 사항

- **슬라이딩(미끄럼 이동)**: 현재 대각선 이동 중 한 축이 막히면 전체 차단. 향후 X축·Z축 각각 독립 CheckWalkable로 슬라이딩 처리 고려.
- **경사 경계(Y 타일 전환)**: 계단식 높이 변화 시 `tileBaseY`가 갑자기 바뀌어 Y 튐 현상 가능. 현재 Y와 sampledY의 보간(lerp) 또는 스무딩 필요 여부 검토.
- **그리드 미로드 구간**: `TryGetTileData` 실패 시 현재는 이동 허용(true 반환). 경계 안전성에 따라 차단으로 전환 고려.
- **AnimComponent 연동**: `UnitAnimComponent.UpdateMovementAnim`에 실제 speed와 방향을 전달하는 로직 미연결. `UnitMoveComponent.ManualUpdate` 끝에서 호출 추가 필요.
- **IngameInputProvider 공유**: 현재 Brain마다 개별 생성. 멀티플레이어 등 확장 시 공유 인스턴스로 교체 필요.
