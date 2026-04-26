# KOMPILE — Project Instructions
> Core Guidelines v2026.04.19 — 레이어별 규칙을 `Assets/Script/Map`, `Assets/Script.Editor` 실측에 맞춰 재정리

## 프로젝트 기본 정보
- **엔진**: Unity 6000 (Unity 6)
- **렌더 파이프라인**: Built-in Render Pipeline
- **타겟 플랫폼**: PC (Windows / macOS), Mobile (iOS / Android)
- **개발 규모**: 솔로 개발
- **언어**: C#
- **Assets 경로**: `/Users/teihyun/Proj_Kompile/Assets`

---

## 아키텍처 레이어 (필수 숙지)

```
Data          → 값(Value) 보관. 3가지 서브레이어 (Context / Definition / Table)
RepoProvider  → Data를 로드·캐시하여 런타임에 공급 (Value 중심 반환)
RenderProvider→ 렌더링/GameObject 생성 담당 (Side-effect)
Operator      → (에디터 전용) 개별 객체 직접 조작 + Undo 통합
Manager       → 생명주기/상태 전이 조율 (plain class, Instance 보관)
Entity        → Manager가 관리하는 논리적 실체 (Framework `Entity` 상속)
Component     → GameObject에 부착되는 최소 기능 단위 (MonoBehaviour)
Utility       → 상태 없는 순수 계산 (Burst / Job 전제)
Tools         → (에디터 전용) Inspector / EditorWindow / MenuItem / Gizmo
```

### 레이어별 규칙

| 레이어 | 네이밍 | 상속/타입 | 자료구조 지향 | 핵심 규칙 |
|---|---|---|---|---|
| **Data/Context** | `...Context` | `class` (POCO) | 가변 필드, Setter 허용 | Manager/Component가 소유·변경 |
| **Data/Definition** | `...Consts`, `...Definition`, `...Data`(정의형 struct) | `static class` / `ScriptableObject` / `[Serializable] struct` | `const`, `static readonly` | 런타임 중 불변. 상수는 `UPPER_SNAKE_CASE` |
| **Data/Table** | `...Data`, `...Table` | `class` / `struct` + `[MessagePackObject]` + `[Key(n)]` | `ConcurrentDictionary`, `List<T>` | MessagePack 직렬화 대상. Addressables 로드 |
| **RepoProvider** | `...RepoProvider` | `class : IDisposable` | `Dictionary<K,V>`, `NativeHashMap<K,V>`, Addressables 핸들 Dict | **Value 중심** 반환. Native 리소스 명시적 Dispose |
| **RenderProvider** | `...RenderProvider` | `class` | Transform/GameObject 참조 | 렌더 리소스 로드 + GameObject 배치 (Side-effect) |
| **Manager** | `...Manager` | plain `class` (MonoBehaviour 아님) | `Dictionary<K, V>`, `HashSet<K>` | 보관 단위(V)는 도메인에 따라 Entity / Data / Context 중 자연스러운 것. GC 최소화용 캐시 컨테이너 프리얼로케이션 |
| **Entity** | `...Entity` | `: Entity` (framework base, `Kompile.Entity.Data`) — `Entity`는 `abstract class : MonoBehaviour` | 내부 `Dictionary<int, GameObject>` 등 | Manager가 `Initialize` / `Dispose` 호출. `AssetKey Key`는 Manager/Provider가 `SetAssetKey()`로 주입 (풀링 식별자). **MonoBehaviour 직접 상속 금지 — 반드시 `Entity` 경유** |
| **Component** | `...Component` / `I...Component` | `: MonoBehaviour` / `interface` | Context 등 소유 | Entity에 부착되는 기능 단위. `[SerializeField] private` Inspector 노출 |
| **Utility** | `...Util` / `...JobUtil` | `static class` / `struct : IJob*` | `NativeArray`, `NativeHashMap`, `NativeStream` | `[BurstCompile]` 필수. Job 스케줄 담당은 `...JobUtil` |

#### Data 서브레이어 구분 요점
- **Context**: 런타임 **가변** 상태. Manager/Component가 프레임마다 변경 (`SetData()`, `SetVisualObject()` 등).
- **Definition**: **에디터 설정 또는 코드 상수**. ScriptableObject(에디터에서 지정)이거나 `static class`(코드 상수). 런타임 중 불변.
- **Table**: **직렬화된 게임 데이터 스냅샷**. MessagePack으로 직렬화되어 Addressables로 로드됨. 로드 후 읽기 전용 취급.

#### Provider 역할 분화
- **RepoProvider**: 직렬화 Data를 로드·캐시해서 Manager에 Value 형태로 공급. `IDisposable`로 Native 자원 생명주기 관리.
- **RenderProvider**: Mesh/Material 로드 후 GameObject 인스턴스 생성·배치. 부작용 있음, 추적은 Manager가 담당.
- **Operator**: 에디터 전용. 개별 객체를 직접 조작하며 `Undo.RecordObject`로 Unity Undo 시스템 통합.

---

## 에디터 전용 레이어 (`Assets/Script.Editor/`)

| 항목 | 규칙 |
|---|---|
| 파일 격리 | 모든 파일 최상단 `#if UNITY_EDITOR` 래핑 필수 |
| 네임스페이스 | 런타임 네임스페이스에 `.Editor.`를 삽입 (`Kompile.Map.Editor.Provider`, `Kompile.Map.Editor.Data`, `Kompile.Asset.Editor.Provider` 등) |
| 네이밍 접두사 | 파일/클래스 앞에 `Edit` 접두사로 런타임 동명 클래스와 구분 (`EditMapCoordUtil` ↔ `MapCoordUtil`) |
| Provider 세 역할 | `...RepoProvider`(`[InitializeOnLoad] static`, 네이티브 캐시 생명주기) / `...SamplingProvider`(`partial class`, Bake 파이프라인) / `...Operator`(`[InitializeOnLoad] static`, 개별 조작 + Undo) |
| `Tools/` 내부 | 4가지 성격이 어트리뷰트/상속으로 식별됨<br>· Inspector 확장 → `[CustomEditor]` + `...Inspector`/`...Editor`<br>· EditorWindow → `: EditorWindow` + `...EditorWindow`/`...Debugger`<br>· MenuItem 정적 도구 → `static class` + `[MenuItem]` + `...Editor`/`...Merger`<br>· Gizmo 시각화 → `MonoBehaviour` + `OnDrawGizmos` + `...Drawer` |
| 의존 방향 | 에디터 → 런타임 단방향 참조만 허용. 런타임이 에디터 참조 금지 |

---

## 추가 원칙 (파일 배치)

1. **파일 위치와 namespace 일치**
   경로 `Kompile/X/Y/` 아래 파일의 namespace는 `Kompile.X.Y`로 선언한다.
2. **에디터 전용 코드는 `Script.Editor/`에 격리**
   `[ExecuteInEditMode]` / `[CustomEditor]` / `[MenuItem]` / `UnityEditor` using을 포함하는 파일은 런타임 폴더(`Script/`)에 두지 않는다.
3. **Manager 보관 단위는 도메인별로 문서화**
   Entity 중심이면 `Dictionary<long, TEntity>`, 스트리밍 데이터 중심이면 `Dictionary<int, TData>` / `Dictionary<int, List<TContext>>` 등 도메인에 맞게 선택하고 Manager 클래스 주석에 기록한다.
4. **Component 레이어는 scene에서 사용 가능하도록 분류**
   Component 레이어는 Editor 전용 여부와 관계 없이 scene에 생성한 MonoBehaviour 개체에 추가하여 사용할 수 있도록 디렉토리, asmdef 등을 고려하여 정리한다.

---

## 코딩 스탠다드

### 성능 규칙 (엄격 적용)
- **NO LINQ**: 런타임 로직에서 완전 금지 (GC 부하 방지)
- **NO Lambda**: 런타임 로직에서 완전 금지 (메모리 할당 최소화)
- **Manual Loop**: `for` / `foreach` 수동 루프 사용 (CPU 캐시 효율), `for`을 우선적으로 사용
- **Burst Compile**: Utility 레이어의 모든 순수 계산 로직은 Burst 최적화 전제로 작성

### 네이밍 컨벤션
- 클래스 / 메서드 / 프로퍼티: `PascalCase`
- private 필드: `_camelCase`
- 상수: `UPPER_SNAKE_CASE`
- 인터페이스: `I` 접두사 (예: `IInteractable`)
- 이벤트: `On` 접두사 (예: `OnPlayerDied`)

### 필드 선언
- Inspector 노출: `[SerializeField] private` 사용
- `public` 필드 금지 → 프로퍼티(`{ get; private set; }`) 사용
- 컴포넌트 캐싱: `Awake()`에서 `GetComponent<>()` 처리

### 비동기 처리
- **기본**: Unity 6 `Awaitable` API 사용 (`async Awaitable` 메서드)
- 코루틴은 간단한 타이머 용도에만 제한 사용

### Unity 6 API
- `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()` 사용
- `FindObjectsOfType<T>()` → `FindObjectsByType<T>(FindObjectsSortMode.None)` 사용
- Obsolete API 사용 금지

### 주석
- 주석 언어: **한국어**
- 복잡한 로직: `/// <summary>` XML 문서 주석
- TODO 형식: `// TODO: [내용]`

---

## Claude 행동 지침

### 응답 언어
- 설명 / 제안 / 질문: **한국어**
- 코드: **영어** (주석은 한국어)

### 코드 작성 시
- 모든 코드에 `using` 구문 포함
- 파일 상단 네임스페이스: `namespace Kompile.[레이어명]`
- 어떤 아키텍처 레이어에 속하는지 항상 명시
- 솔로 개발 → 과도한 추상화보다 **명확성과 실용성** 우선
- **성능 규칙(NO LINQ / NO Lambda) 자동 준수** — 별도 언급 없어도 적용

### 파일 작업 시
- 기존 파일 수정 전 현재 내용 확인
- 경로는 `/Users/teihyun/Proj_Kompile/Assets/` 기준 상대경로로 표시

---

## Dispatch 전용 지침

1. **경로 명시**: 생성/수정하는 파일의 전체 경로를 응답에 포함
2. **간결한 확인**: 작업 완료 후 "무엇을 했는지" 한 줄 요약
3. **모호한 요청 처리**: 가장 합리적인 해석으로 진행 + 가정 내용 명시
4. **코드 작성 대기**: 직접 코드 작성을 요청하기 전까지 코드 작성 및 수정 대기
5. **스탠다드 자동 적용**: 별도 언급 없어도 위 모든 규칙 준수
