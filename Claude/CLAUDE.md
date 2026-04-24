# 작업 규칙

## 코드 작성
- 사용자가 명시적으로 요청하기 전까지 코드 작성 업무를 하지 않는다.

## .md 문서 작성
- **추론 금지**: 문서의 모든 내용은 직접 읽은 소스 코드 또는 파일에 근거해야 한다.
- 추론이 필요한 상황이 생기면, 작업 전에 어떤 파일을 추가로 읽어야 하는지 명시하고 사용자에게 확인한다.
- 확인되지 않은 내용은 문서에 포함하지 않는다. 누락이 추론보다 낫다.

---

# 프로젝트 구조 (Proj_Kompile)

## 폴더 / 네임스페이스 구조

```
Assets/Script/
├── Asset/          → Script.Global.Asset.*       에셋 로딩, 테이블 데이터
├── Battle/         → Script.Battle.*             전투 씬 전용 시스템
├── Field/          → Script.Field.*              필드(인게임 월드) 씬 전용 시스템
│   ├── Entity/     → FieldPlayerEntity
│   └── Manager/    → FieldUnitManager (파일명: FieldManager.cs)
├── Global/         → Script.Global.*             씬 공통 범용 시스템
│   ├── Entity.cs   → Script.Global.Entity.Data.Entity  (MonoBehaviour base)
│   ├── Input/      → Script.Global.Input.*       입력 시스템
│   └── Unit/       → Script.Global.Unit.*        유닛 Entity/Component/Brain
├── Map/            → Script.Map.*                맵 데이터·렌더링·네비게이션
│   ├── Data/       → 맵 데이터 구조체 및 컨텍스트
│   ├── Entity/     → MapGridEntity, MapUnitMoveComponent
│   ├── Manager/    → MapManager
│   └── Utility/    → MapCoordUtil, MapNaviTileUtil, MapNaviSteeringUtil, AStarPathfinderUtil
└── Main/           → Script.Main.*               앱 진입점, MainManager
```

---

## 아키텍처: Entity / Component / Brain 패턴

### 계층 구조
```
Entity (MonoBehaviour)
 └── UnitEntityBase (MonoBehaviour)  ← 유닛의 기반 클래스
      └── FieldPlayerEntity           ← 필드 전용 플레이어 유닛
           ├── UnitMoveComponent      ← 이동 연산 전담 (MonoBehaviour)
           ├── UnitAnimComponent      ← 애니메이션 전담 (MonoBehaviour)
           └── _brain: IUnitBrain     ← 행동 로직 전담 (순수 C# 객체)
                └── PlayerControlBrain
```

### 생명주기
- **Manager가 생성/삭제를 전담** (Spawn/Despawn) — 유닛이 스스로 생성/삭제하지 않는다.
- **ManualUpdate 패턴** — `MonoBehaviour.Update()` 사용 금지. Manager가 루프를 돌며 `entity.ManualUpdate()`를 호출한다.
- **풀링(FastPool<T>)** — 유닛 재사용 시 `SetActive(false/true)` + `Initialize/Clear`.

### 주요 클래스 요약

| 클래스 | 역할 |
|--------|------|
| `UnitEntityBase` | InstanceID, AssetAddress, UnitRuntimeContext, IUnitBrain 보유. `Initialize/Clear/ManualUpdate` abstract |
| `FieldPlayerEntity` | `UnitMoveComponent` + `UnitAnimComponent` 초기화, ManualUpdate 위임 |
| `IUnitBrain` | `Initialize(UnitEntityBase)`, `ManualUpdate()`, `Clear()` |
| `PlayerControlBrain` | 플레이어 입력을 받아 유닛을 제어하는 Brain (현재 미구현) |
| `UnitMoveComponent` | `SetDestination(Vector3)`, `ManualUpdate()` — 이동 연산 전담 |
| `UnitAnimComponent` | Animator Hash 캐싱, `UpdateMovementAnim(float speed, Vector3 dir)` |
| `FieldUnitManager` | `SpawnUnitByIdAsync`, `DespawnUnit`, `UpdateAllUnitsLogic`, `ClearAll` |

---

## 입력 시스템

### Definition_Input.cs
```
IDxInput (Flags enum):
  LEFT / RIGHT / UP / DOWN   ← 이동 방향 (조합하면 8방향)
  ENTER / CANCEL / ACTION    ← 액션 버튼
  MOVE_ALL / SELECT_ALL      ← 복합 마스크

InputState (readonly struct):
  IsDown(input)     ← 이번 프레임에 처음 눌림
  IsPressing(input) ← 현재 누르고 있음
  IsUp(input)       ← 이번 프레임에 뗌
```

### IngameInputProvider
- `InputSystem` 기반 (Keyboard Arrow + Z/X/Space)
- **`OnEndOfFrame()`** 을 매 프레임 끝에 반드시 호출해야 함 (latched 상태 갱신)
- `Current` 프로퍼티 → `InputState` 반환

---

## 맵 시스템

### 핵심 데이터 구조

| 클래스/구조체 | 설명 |
|---|---|
| `MapGridData` | 64×64 타일 그리드 1개. `NaviTileDict` (ConcurrentDictionary<int, MapTileData>) 보유 |
| `MapTileData` | 타일 1개. `NaviMask` (long) + `LinkMask` (ushort) |
| `MapGridContext` | 런타임 그리드 컨텍스트 (GridKey, GridIndex, Data, VisualObject, Bounds) |
| `MapChunkContext` | 렌더링용 메쉬 청크 단위 (Layer, Obj, Renderer, Color) |
| `MovementContext` | 이동 중인 유닛의 경로/속도 상태 (Path, Velocity, MaxSpeed 등) |

### NaviMask (long, 64비트)
- **하위 52비트**: 13개 정점 × 4비트 = 높이 데이터
  - 4비트 값 0~14 → 유효한 높이 인덱스
  - 4비트 값 15 (`0b1111`) → 해당 정점 없음(이동 불가)
- **상위 비트**: 레이어 인덱스 (`TOTAL_BITS * BITS_PER_CELL = 52`번째 비트 이상)
- 13개 정점 위치: `MapConsts.VertexPositions` (타일 내 [0,1] 정규화 좌표)
- 16개 서브타일(삼각형): `MapConsts.SubTileVertexMap` (정점 3개 인덱스 조합)

### LinkMask (ushort, 16비트)
- 8방향 × 2비트 = 16비트
- 각 방향의 이동 가능 여부와 높이 차이 인코딩:
  - `0b00` → LINK_NONE (이동 불가)
  - `0b01` → LINK_ZERO (같은 Y)
  - `0b10` → LINK_UP (+1 타일 Y)
  - `0b11` → LINK_DOWN (-1 타일 Y)
- 방향 인덱스 (반시계 방향, `GetLinkMaskShift` 기준):
  - 0: LEFT+DOWN, 1: DOWN, 2: RIGHT+DOWN, 3: RIGHT
  - 4: RIGHT+UP, 5: UP, 6: LEFT+UP, 7: LEFT

### 핵심 유틸리티

| 유틸 | 주요 메서드 |
|---|---|
| `MapCoordUtil` | `ComputeTileID(float3)` → `long` tileID, `ComputeKey(float3, out gKey, out tKey)`, `ComputeGridKey(float3)` |
| `MapNaviTileUtil` | `IsSubTileValid(naviMask, sIndex)`, `GetHeightFromNaviMask(naviMask, vIndex)`, `TryGetYInt(linkMask, dirIndex, out yInt)`, `IsCircleOverlappingSquare(pos, center, radius)`, `IsCircleOverlappingSubTile(sIndex, center, radiusSq)` |
| `MapNaviSteeringUtil` | `CalculateSteering(...)`, `GetSpriteDirection8(velocity)` |

### MapManager
- **그리드 스트리밍** — 카메라 주변 PRELOAD_RADIUS(10f) 안으로 그리드 자동 로드, UNLOAD_RADIUS(20f) 밖이면 언로드
- 로드된 그리드: `_mapGridDataDic[gridKey]` → `MapGridData`
- 타일 조회: `mapGridData.TryGetTileData(tileIntKey, out MapTileData)`
- 그리드 키 계산: `MapCoordUtil.ComputeGridKey(worldPos)`
- 타일 int 키: `MapCoordUtil.ComputeKey(worldPos, out gKey, out tKey)` 에서 `tKey`

---

## 코딩 컨벤션

- **네임스페이스**: 폴더 구조와 1:1 대응 (`Script.Field.Entity`, `Script.Global.Unit.Entity` 등)
- **private 필드**: `_camelCase`
- **MonoBehaviour 생명주기**: `Awake`/`Start` 대신 `Initialize(...)` 명시적 호출 패턴 사용
- **ManualUpdate 패턴**: `Update()` 사용 금지, Manager가 일괄 호출
- **비동기**: Unity `Awaitable` 사용 (`async Awaitable`)
- **GC 최소화**: 문자열 캐싱, `MaterialPropertyBlock` 재사용, `FastPool<T>` 풀링
- **Burst 컴파일 대상**: `MapCoordUtil`, `MapNaviTileUtil` 등 수학 유틸은 `[BurstCompile]` 적용
- **에디터 전용 코드**: `#if UNITY_EDITOR` 블록으로 격리
- **`false == expr` 패턴**: null/false 체크 시 `if (false == obj)` 형태 사용
