# Kompile Framework v0 — 기술 문서

> Unity Engine 6 기반 HD-2D 스타일 RPG 프레임워크 전체 아키텍처 정리

---

## 목차

1. [디렉터리 구조](#1-디렉터리-구조)
2. [레이어 아키텍처](#2-레이어-아키텍처)
3. [시스템별 상세](#3-시스템별-상세)
   - [3.1 Main — 진입점 & 공용 컬렉션](#31-main--진입점--공용-컬렉션)
   - [3.2 Global — 입력 시스템](#32-global--입력-시스템)
   - [3.3 Battle — 턴제 전투 시스템](#33-battle--턴제-전투-시스템)
   - [3.4 Field — 필드 유닛 시스템](#34-field--필드-유닛-시스템)
   - [3.5 Map — 스트리밍 맵 & 길찾기](#35-map--스트리밍-맵--길찾기)
   - [3.6 Asset — 에셋 로딩 & 풀링](#36-asset--에셋-로딩--풀링)
4. [핵심 인터페이스 & 타입](#4-핵심-인터페이스--타입)
5. [데이터 흐름](#5-데이터-흐름)
6. [게임 루프 & 업데이트 사이클](#6-게임-루프--업데이트-사이클)
7. [코딩 표준 & 컨벤션](#7-코딩-표준--컨벤션)
8. [디자인 패턴 목록](#8-디자인-패턴-목록)
9. [성능 전략](#9-성능-전략)
10. [미완성 / 보류 항목](#10-미완성--보류-항목)

---

## 1. 디렉터리 구조

```
Assets/Script/
├── Main/              # 씬 진입점, 공용 컬렉션 (FastPool)
├── Global/            # 횡단 관심사 (Input)
├── Field/             # 필드(오버월드) 유닛·플레이어 레이어
├── Battle/            # 턴제 전투 시스템
├── Map/               # 타일 맵 스트리밍 + A* 길찾기
├── Asset/             # Addressables 기반 에셋 로딩·캐싱·풀링
└── Tools/             # 에디터 전용 유틸 (FrameChecker 등)
```

각 주요 시스템(Battle, Field, Map)은 다음 내부 구조를 공유한다:

| 하위 폴더 | 역할 |
|-----------|------|
| `Data/Definition/` | 열거형, 상수, 이뮤터블 정의 |
| `Data/Context/` | 런타임 상태 컨테이너 |
| `Data/Table/` | 직렬화 가능한 데이터 구조체 |
| `Entity/` | MonoBehaviour 컴포넌트 |
| `Manager/` | 라이프사이클·상태 관리 |
| `RepoProvider/` | 에셋 로딩·데이터 제공 |
| `Utility/` | 순수 함수 (Burst 컴파일 대상) |

---

## 2. 레이어 아키텍처

```
┌──────────────────────────────────────────┐
│            MainManager (Scene)           │  ← Unity 씬 진입점
├──────────────────────────────────────────┤
│      FieldManager  │  BattleManager      │  ← 게임플레이 레이어
├──────────────────────────────────────────┤
│   FieldUnitManager │  MapManager         │  ← 서브시스템 레이어
├──────────────────────────────────────────┤
│  Entity / Component (MonoBehaviour)      │  ← 실제 씬 오브젝트
├──────────────────────────────────────────┤
│  AssetRepoProvider (Addressables)        │  ← 에셋 공급 레이어
├──────────────────────────────────────────┤
│  Burst Utilities (Jobs / NativeArrays)   │  ← 고성능 순수 연산
└──────────────────────────────────────────┘
```

의존 방향은 **위 → 아래** 단방향이다. 하위 레이어는 상위 레이어를 직접 참조하지 않는다.

---

## 3. 시스템별 상세

### 3.1 Main — 진입점 & 공용 컬렉션

#### MainManager
- 씬의 유일한 루트 MonoBehaviour.
- 동적으로 Map, Unit 용 루트 Transform을 생성해 씬 계층을 정돈한다.
- 카메라 참조를 보유하고, FieldManager를 초기화한다.
- 에디터 모드에서 `Space` 키로 레이어 전환 테스트를 지원한다.

#### FastPool\<T\> (`Main/Manager/Collection/`)
프로젝트 전역에서 사용하는 **제로-GC 오브젝트 풀**.

| 항목 | 내용 |
|------|------|
| 제네릭 | `T : Component` |
| 내부 자료구조 | 동적 배열 (초기화 후 2배 확장) |
| Pop / Push | O(1), 힙 할당 없음 |
| 안전 해제 | `ClearAndDestroyAll()` |

---

### 3.2 Global — 입력 시스템

#### IngameInputProvider

UnityEngine.InputSystem 기반 키보드 입력을 관리한다. 세 가지 플래그 상태를 유지한다:

| 플래그 | 설명 |
|--------|------|
| `rawInputFlag` | 실시간 하드웨어 상태 |
| `latchedInputFlag` | 프레임 내 누적 입력 (소비자가 읽는 값) |
| `prevInputFlag` | 직전 프레임 상태 (IsDown / IsUp 판별용) |

입력 처리 순서:
```
하드웨어 → InputSystem → rawInput (실시간)
                              ↓ OR 누적
                         latchedInput (프레임 단위)
                              ↓ OnEndOfFrame()
                         prevInput ← latched (동기화)
```

#### IDxInput (`Definition_Input.cs`)
```csharp
[Flags]
enum IDxInput {
    Up = 1, Down = 2, Left = 4, Right = 8,   // 4방향
    Enter = 16, Cancel = 32, Action = 64,     // 버튼
    MOVE_ALL = Up | Down | Left | Right,
    SELECT_ALL = Enter | Cancel
}
```

#### InputState
```csharp
readonly struct InputState {
    bool IsDown(IDxInput key);     // 이번 프레임에 눌린
    bool IsPressing(IDxInput key); // 누르는 중
    bool IsUp(IDxInput key);       // 이번 프레임에 떼어진
}
```

---

### 3.3 Battle — 턴제 전투 시스템

#### 핵심 상수

| 상수 | 값 | 설명 |
|------|-----|------|
| `TARGET_FPS` | 24 | 전투 로직 프레임 레이트 |
| `FRAMES_PER_TICK` | 6 | 1틱당 로직 프레임 수 |
| `TARGET_DISTANCE` | 10000f | 대기 페이즈 초기 거리 |
| `ORDER_PER_TICK` | 10.0f | 속도 1 기준 틱당 이동 거리 |

#### BattleManager

유닛 라이프사이클과 전투 흐름을 총괄한다.

- **두 딕셔너리**: `_units` (엔티티 참조) + `_contexts` (런타임 상태)
- **유닛 페이즈**:
  - `Wait` 페이즈: 대기 거리가 10,000 → 0으로 속도에 비례해 감소
  - `Action` 페이즈: 대기 완료 후 ~2,000 거리 단위 동안 스킬 실행
- **인터럽트**: `TriggerInterrupt(unitID, skillID)` → 즉시 액션 페이즈 진입

#### BattleTimelineManager

**어큐뮬레이터 패턴**으로 deltaTime → 논리 프레임 변환.

| 기능 | 설명 |
|------|------|
| `_currentTotalFrames` | 전역 프레임 카운터 |
| `ApplyHitStop(duration, scale)` | 히트스톱 (scale=0.1 → 90% 슬로우) |
| `RequestFastForwardTo(frame, buffer)` | 목표 프레임까지 감속 도달 |
| 패닉 임계값 | 최대 8프레임/업데이트 (프레임 스킵 방지) |

시간 스케일 우선순위:
1. 히트스톱 (최우선)
2. 목표 프레임 감속
3. 기본 (1.0×)

#### BattleUnitAnimationComponent

Animator를 **속도 0으로 고정**하고 직접 샘플링한다.

```csharp
// 매 프레임 호출
void Sample(int currentFrame, int fpt) {
    float progress = (float)(currentFrame - startFrame) / totalFrames;
    animator.Play(stateHash, 0, progress); // 수동 샘플링
    animator.Update(0f);
}

bool CheckHitTriggered(int currentFrame) { /* 콤보 윈도우 체크 */ }
```

#### 주요 데이터 구조

```csharp
struct BattleAnimationCommand {
    int StateHash;
    int StartFrame;
    int StartupTick, ActiveTick, RecoveryTick;
    int HitFrameOffset;
    (int start, int end) ComboWindow;
}

struct BattleUnitContext {
    long EntityID;
    float CurrentSpeed;
    float RemainingDistance;
    float ActionDistance;
    BattlePhase Phase; // Wait | Action
}
```

#### BattleUtil ([BurstCompile])

| 함수 | 계산 |
|------|------|
| `CalculateProgressPerTick(speed)` | `speed × 10.0` |
| `CalculatorOrderToFrames(dist, fpt)` | 대기 거리 → 프레임 수 변환 |
| `IsInsideComboWindow(frame, cmd)` | 콤보 윈도우 내 여부 |
| `GetRequiredTimeScale(...)` | 목표 프레임 감속 스케일 계산 |

---

### 3.4 Field — 필드 유닛 시스템

#### FieldManager

MapManager와 FieldUnitManager를 순차 초기화하고, 플레이어 유닛 초기 스폰을 담당한다.

#### FieldUnitManager

| 항목 | 내용 |
|------|------|
| 유닛 추적 | `Dictionary<long, FieldUnitEntity>` (instanceID 키) |
| 풀링 | `Dictionary<string, FastPool<FieldUnitEntity>>` (에셋 주소별) |
| 풀 용량 | 최대 128 활성 유닛, 주소당 32개 |

**스폰 흐름**:
```
SpawnUnitAsync()
  1. 풀에서 사용 가능한 엔티티 확인
  2. 없으면 Addressables에서 프리팹 로드
  3. 고유 InstanceID 할당 (카운터 증가)
  4. UnitRuntimeContext 생성 (Type + BrainType)
  5. 모든 컴포넌트 Initialize() 호출
  6. Brain 컴포넌트 Attach
```

#### FieldUnitEntity

```
FieldUnitEntity
├── InstanceId (long)
├── AssetAddress (string)
├── UnitRuntimeContext
├── UnitMoveComponent   ← ManualUpdate() 순서 2
├── UnitAnimComponent   ← ManualUpdate() 순서 3
└── IUnitBrainComponent ← ManualUpdate() 순서 1 (Brain 최우선)
```

#### UnitAnimComponent

8방향 이동 애니메이션 블렌딩 지원. 해시 캐시 목록:
`Speed`, `DirX`, `DirZ`, `Hit`, `Dead`, `Attack`

#### 열거형

```csharp
enum UnitType      { Player, PartyGroup, NPC, Enemy }
enum FieldBrainType {
    PlayerControl, PartyFollower,
    NpcIdle, NpcShop, NpcInn,
    EnemyWanderEncounter, EnemyStandEncounter
}
```

---

### 3.5 Map — 스트리밍 맵 & 길찾기

#### 좌표 체계

| 단위 | 크기 | 설명 |
|------|------|------|
| Grid | 64×64 타일 | 청크 단위, 스트리밍 기준 |
| Tile | 1.0 unit | 기본 지형 단위 |
| Sub-tile | 0.125 unit | 8개 버텍스/타일 (16 서브타일/타일) |
| Voxel | 0.25 unit | 2×2×2 서브그리드 |

**64비트 타일 ID 비트 레이아웃**:
```
[상위 32비트: Grid Key (부호 있는 X,Y,Z 각 1바이트)]
[하위 32비트: Local Tile Key (X,Y,Z 각 6비트)]
```

#### MapManager

**스트리밍 상수**:

| 상수 | 값 | 역할 |
|------|----|------|
| `PRELOAD_RADIUS` | 10 | 비동기 로드 시작 반경 |
| `UNLOAD_RADIUS` | 20 | 언로드 시작 반경 (히스테리시스) |
| `CHECK_INTERVAL` | 0.5s | 스트리밍 루프 폴링 주기 |

**그리드 스트리밍 루프** (`StartGridStreamingLoopAsync`):
- 카메라 위치 기준 원통형 방사 탐색
- `_invalidGrids` 블랙리스트로 없는 데이터 반복 로드 방지
- Fire-and-forget 비동기 로드 (논블로킹)

**메시 생성** (`CreateMapChunksAsync`):
- 3개 인스턴스마다 yield → 프레임 히치 방지 (타임 슬라이싱)
- Addressables에서 Mesh + Material 로드

**레이어 가시성** (`UpdateLayerVisibilityAsync`):
- 기본 1.0초 페이드 전환
- `MaterialPropertyBlock`으로 복사 없이 색상 블렌딩
- 1D 리스트로 애니메이션 중인 청크를 캐시 (딕셔너리 순회 회피)

#### A* 길찾기

**AStarBatchJobUtil** (`[BurstCompile]`, `IJobParallelFor`):
- 여러 유닛의 A*를 병렬 실행
- Unity Native Collections 사용 (`NativeArray`, `NativeHashMap`, `NativeStream`)

**String Pulling (경로 스무딩)**:
1. 끝 → 시작 역추적 (부모 포인터)
2. 가장 먼 가시 웨이포인트를 탐욕적으로 선택
3. 결과: 웨이포인트 수를 대폭 줄인 부드러운 경로

**가시선(LOS) 검증**:
- 0.1875 unit 간격으로 샘플링
- 높이 차 > 0.5 unit 이면 경로 거부
- 2D 외적(Point-in-Triangle) + 세그먼트 거리로 충돌 검사

#### MapTileData (MessagePack 직렬화)

```csharp
struct MapTileData {
    long  NaviMask;  // 버텍스당 13비트 (높이 4비트 + 유효성)
    ushort LinkMask; // 방향당 2비트 (8방향 수직 링크)
    ushort LayerMask; // 미래 레이어 지원용 (현재 미사용)
}
```

#### MapUnitMoveComponent — 스티어링 이동

```csharp
// 매 프레임
velocity += CalculateSteering(desiredVelocity, velocity);
velocity  = Clamp(velocity, MaxSpeed);
position += velocity * deltaTime;
// 노드 도달 시 다음 웨이포인트로 진행
```

8방향 스프라이트 방향: `GetSpriteDirection8()` (속도 벡터의 각도에서 인덱스 계산)

#### Burst 유틸리티 목록

| 클래스 | 주요 함수 |
|--------|----------|
| `MapCoordUtil` | `ComputeTileIDInt`, `ComputeGridKey`, `ComputeWorldPosition` |
| `MapNaviTileUtil` | `IsSubTileValid`, `GetHeightFromNaviMask`, `IsCircleOverlappingSubTile` |
| `MapNaviSteeringUtil` | 스티어링 력 계산, 옥틸 거리 휴리스틱, 8방향 인덱스 |
| `AStarPathfinderUtil` | 배치 요청 래퍼 (`List<Vector3>` → `NativeArray` → Job) |

---

### 3.6 Asset — 에셋 로딩 & 풀링

#### AssetRepoProvider (static partial class)

**두 가지 로딩 트랙**:

| 트랙 | 주소 결정 방식 | 사용 예 |
|------|--------------|---------|
| 데이터 주도 | `AssetKey` → 문자열 주소 | 프리팹, 텍스처 |
| 타입 추론 | `TypeNameCache<T>.Name` | 제네릭 데이터 |

**인스턴스 풀링 흐름** (`GetOrNewInstanceAsync<T>`):
```
최초 로드: Addressables.LoadAssetAsync → entry 저장
이후 호출: 풀 우선 → 빈 경우 Addressables.InstantiateAsync
참조 카운트: AddReference / RemoveReference
카운트 ≤ 0 && 풀 비어있음 → 자동 Release
```

**바이너리 데이터 로딩** (`LoadBinaryDataAsync<T>`):
- TextAsset 로드 → MessagePackSerializer 역직렬화 → 즉시 Release

**존재 여부 사전 확인**:
- `ReadBinaryDataAsync()`: 없는 에셋 로드 시 예외 방지
- 없으면 default 반환 → 호출자가 블랙리스트에 등록

#### InstanceEntry (내부 private 클래스)

```csharp
class InstanceEntry {
    AsyncOperationHandle handle;       // 원본 에셋 핸들
    ConcurrentQueue<T> pool;          // 풀링된 인스턴스
    int refCount;                     // Interlocked (thread-safe)
    bool ShouldRelease();             // refCount≤0 && pool empty
}
```

#### AssetKey

```csharp
readonly struct AssetKey : IEquatable<AssetKey> {
    string _address;
    int    _hashCode; // 생성 시 캐시
    bool   IsValid;   // null/empty 검사
    // string → AssetKey, AssetKey → string 암묵적 변환
}
```

#### MessagePack 설정

- `ContractlessStandardResolver` → 어트리뷰트 없이 직렬화 가능
- `SerializeUtil` 헬퍼: null/빈 배열 안전 처리

---

## 4. 핵심 인터페이스 & 타입

| 이름 | 종류 | 위치 | 역할 |
|------|------|------|------|
| `Entity` | Abstract Class | `Global/Entity.cs` | 풀링 가능한 모든 오브젝트의 기반; AssetKey 보유 |
| `ITimelineHandler` | Interface | `Battle/Data/Definition/` | 타임라인 목표 프레임 도달 시 콜백 |
| `IUnitBrainComponent` | Interface | `Field/Entity/` | AI/플레이어/NPC 행동 전략 교체 |
| `IInitializable` | Interface | `Asset/Data/Definition/` | Initialize() 호출이 필요한 타입 표시 |
| `IDxInput` | [Flags] Enum | `Global/Input/Data/` | 4방향 + 3버튼 비트플래그 |
| `BattlePhase` | Enum | `Battle/Data/` | Wait / Action |
| `UnitType` | Enum | `Field/Data/` | Player, PartyGroup, NPC, Enemy |
| `FieldBrainType` | Enum | `Field/Data/` | 7가지 행동 유형 |
| `AssetKey` | Struct (IEquatable) | `Asset/Data/` | 해시 캐시 된 에셋 주소 래퍼 |
| `InputState` | Readonly Struct | `Global/Input/Data/` | IsDown/IsPressing/IsUp 2프레임 쿼리 |

---

## 5. 데이터 흐름

```
MainManager
  │
  ├─ FieldManager.InitializeAsync()
  │    ├─ MapManager.InitializeAsync()
  │    │    └─ StartGridStreamingLoopAsync()  ← 카메라 위치 기준 주기적 스트리밍
  │    │         ├─ AssetRepoProvider.LoadBinaryDataAsync<MapGridData>()
  │    │         └─ CreateMapChunksAsync()    ← 타임슬라이싱 메시 생성
  │    │
  │    └─ FieldUnitManager.SpawnUnitAsync()
  │         └─ AssetRepoProvider.GetOrNewInstanceAsync<FieldUnitEntity>()
  │
  └─ Update() 매 프레임
       │
       ├─ IngameInputProvider.OnEndOfFrame()   ← 입력 상태 동기화
       │
       ├─ FieldUnitManager.ManualUpdate()
       │    └─ foreach unit:
       │         ├─ BrainComponent.ManualUpdate()  ← 행동 결정
       │         ├─ MoveComponent.ManualUpdate()   ← 스티어링 이동
       │         └─ AnimComponent.ManualUpdate()   ← 애니메이터 동기화
       │
       └─ BattleManager.Update()
            ├─ BattleTimelineManager.OnUpdateTick(deltaTime)
            │    └─ 누적 시간 → 논리 프레임 (24 FPS)
            ├─ UpdateVisuals(frame)              ← 매 프레임 수동 샘플링
            └─ (frame % 6 == 0) ProcessBattleLogic()  ← 틱마다 거리·페이즈 갱신
```

**비동기 체인** (메인 스레드 비블로킹):
- 그리드 스트리밍: 0.5초 폴링 루프
- 에셋 로딩: Fire-and-forget Addressables
- A* 길찾기: Job System 병렬 실행
- 레이어 페이드: 취소 토큰 기반 yield 루프

---

## 6. 게임 루프 & 업데이트 사이클

### 프레임 레이트 전략

| 레이어 | 주기 | 설명 |
|--------|------|------|
| 렌더링 | 60 FPS | Unity 기본 |
| 전투 로직 | 24 FPS | 어큐뮬레이터로 변환 |
| 전투 틱 | 4 FPS (6프레임/틱) | 거리·페이즈 갱신 |

### 입력 처리 사이클

```
[하드웨어]
    → rawInputFlag (실시간)
    → latchedInputFlag (프레임 내 OR 누적)
    → OnEndOfFrame(): prev ← latched, latched ← raw
    → 다음 프레임 소비자 읽기 (IsDown/IsPressing/IsUp)
```

### 전투 업데이트 상세

```
BattleManager.Update()
  BattleTimelineManager.OnUpdateTick(deltaTime)
    └─ 논리 프레임 누적 (최대 8프레임/업데이트, 패닉 임계값)
    └─ 히트스톱/목표프레임/기본 스케일 우선순위 적용
  UpdateVisuals(currentFrame)
    └─ BattleUnitAnimationComponent.Sample(frame, fpt)
  (frame % FRAMES_PER_TICK == 0)
    └─ 각 유닛: RemainingDistance -= CalculateProgressPerTick(speed)
    └─ 0 이하 → Wait → Action 페이즈 전환
```

---

## 7. 코딩 표준 & 컨벤션

### 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase | `BattleManager`, `FieldUnitEntity` |
| 메서드 | PascalCase | `UpdateLayerVisibilityAsync` |
| private 필드 | `_camelCase` | `_animator`, `_unitIDCounter` |
| 상수 | `UPPER_SNAKE_CASE` | `TARGET_FPS`, `GRID_SIZE` |
| enum 값 | PascalCase | `Wait`, `Action`, `PlayerControl` |
| 지역 변수 | camelCase | `currentFrame`, `targetPos` |

### 클래스 접미사 규칙

| 접미사 | 의미 |
|--------|------|
| `Manager` | 라이프사이클 총괄 (BattleManager) |
| `Component` | MonoBehaviour 컴포넌트 (UnitMoveComponent) |
| `Entity` | 컴포넌트 집합 오브젝트 (FieldUnitEntity) |
| `Data` | 직렬화 가능한 데이터 구조체 (BattleSkillData) |
| `Context` | 런타임 상태 컨테이너 (BattleUnitContext) |
| `Util` | 순수 함수 집합 (MapCoordUtil) |
| `RepoProvider` | 에셋·데이터 공급자 (AssetRepoProvider) |

### 코드 스타일

- XML doc 주석 (`///`) — 모든 public 멤버
- 한국어 인라인 주석 + 영어 요약 혼용
- Coroutine 대신 `Awaitable` / `async-await` 사용
- Burst 함수: `[ReadOnly]` 어트리뷰트 명시
- GC 최소화: 문자열 해시 정적 캐시, ConcurrentQueue for pooling
- 가드 클로즈(early return)로 중첩 최소화
- 데이터 홀더: 가능하면 `struct` 사용 (BattleAnimationCommand, InputState, AssetKey)
- LINQ, 람다 사용 금지 (Burst 호환성 + 성능)

---

## 8. 디자인 패턴 목록

| 패턴 | 구현 위치 | 효과 |
|------|-----------|------|
| Manager Pattern | BattleManager, FieldManager, MapManager | 중앙화된 라이프사이클 |
| Component Pattern | Entity + MoveComponent, AnimComponent, BrainComponent | 유연한 조합 |
| Provider / Repository | AssetRepoProvider, BattleSkillRepoProvider | 에셋·데이터 소스 분리 |
| Object Pool | FastPool<T> + 주소별 풀 | 런타임 GC 제로 |
| Strategy | IUnitBrainComponent | 행동 교체 |
| Accumulator | BattleTimelineManager | 고정 타임스텝 시뮬레이션 |
| Factory (implicit) | FieldUnitManager.SpawnUnitAsync | 풀링 통합 생성 |
| Data-Driven Design | AssetKey + MapGridData 직렬화 | 코드 변경 없는 설정 |
| Fire-and-Forget Async | MapManager 그리드 로드 | 논블로킹 백그라운드 작업 |
| Blacklist | MapManager._invalidGrids | 없는 에셋 반복 요청 방지 |

---

## 9. 성능 전략

### 제로-GC 설계

| 기법 | 위치 | 효과 |
|------|------|------|
| FastPool<T> | Main/Manager/Collection | 힙 할당 없는 컴포넌트 재사용 |
| AssetKey 해시 캐시 | Asset/Data/Definition | Dictionary 조회 O(1), 박싱 없음 |
| 애니메이터 파라미터 해시 | BattleUnitAnimationComponent, UnitAnimComponent | 문자열 조회 제거 |
| Animator.speed = 0 + Sample | BattleUnitAnimationComponent | GC 없는 프레임 고정 샘플링 |
| MaterialPropertyBlock | MapManager | Material 복사 없는 GPU 파라미터 전달 |
| 1D 애니메이션 청크 캐시 | MapManager | 중첩 딕셔너리 순회 회피 |

### Burst 컴파일 대상

| 유틸리티 | 연산 |
|----------|------|
| AStarBatchJobUtil | 병렬 A* (IJobParallelFor) |
| MapCoordUtil | 비트 연산 좌표 변환 |
| MapNaviTileUtil | 네비게이션 마스크 쿼리, 충돌 검사 |
| MapNaviSteeringUtil | 스티어링 력, 옥틸 휴리스틱 |
| BattleUtil | 전투 거리·프레임 계산 |

### 타임 슬라이싱

- 메시 생성: 3개 인스턴스마다 `yield` (CreateMapChunksAsync)
- 전투 업데이트: 패닉 임계값으로 최대 8프레임/업데이트 제한

### 비트 패킹

- 64비트 타일 ID에 Grid + Local 좌표 압축
- NaviMask: 버텍스당 13비트 (높이 4비트 + 유효 1비트 × 8버텍스 + 여분)
- LinkMask: 방향당 2비트 (8방향 수직 링크)

---

## 10. 미완성 / 보류 항목

| 위치 | 내용 | 상태 |
|------|------|------|
| `Map/Entity/MapGridEntity.cs:50` | Material 할당 로직 플레이스홀더 | TODO |
| `Map/Entity/MapUnitMoveComponent.cs:78` | 이동 방향별 스프라이트 애니메이션 | TODO |
| `Map/Data/Table/MapTileData.cs:12` | LayerMask 필드 (예약만, 미사용) | TODO |
| `Tools/Editor/GUIDevFrameChecker.cs` | FrameRate 설정 → Config 클래스로 분리 필요 | TODO |
| `Field/Manager/FieldUnitManager.cs:97-130` | Brain 팩토리 패턴 스캐폴딩 (주석 처리됨) | 보류 |
| 전반적 | ScriptableObject 기반 런타임 설정 없음 (인라인 상수만) | 미구현 |
| 전반적 | NPC/Enemy Brain 구현 없음 (인터페이스만 존재) | 미구현 |

---

*작성일: 2026-04-08 | 대상 브랜치: Claude/Field | Unity Engine 6*
