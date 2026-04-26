# MainManager 프로그래밍 설계 문서

> **목적:** MainManager.cs의 역할·구조·생명주기를 기록한다.
> 신규 설계 항목은 **[설계]** 태그, 구현 완료 항목은 **[구현완료]** 태그로 명시한다.
>
> **구현 상태 (2026-04-26):** MainManager.cs 구현 완료. FieldManager 초기화 및 필드 생성 시퀀스 동작 확인.
>
> 연관 문서: [FieldManager.md](FieldManager.md)

---

## 1. 개요

**MainManager**는 런타임 씬의 C++ `main()` 에 해당하는 진입점 클래스다.

```
파일 경로: Assets/Script/MainManager.cs   [구현완료]
네임스페이스: Kompile                     [구현완료]
아키텍처 레이어: (MonoBehaviour 진입점 — Manager 레이어 위에 위치하는 예외 계층)
타입: MonoBehaviour
```

**역할 요약**

- 씬 시작 시 주요 콘텐츠 Manager(FieldManager, BattleManager, IngameEventManager 등)를 생성·초기화한다.
- 매 프레임 `Update()`에서 각 콘텐츠 Manager의 `Update()`를 **순차적으로** 호출한다.
  (각 Manager는 MonoBehaviour가 아니므로 Unity 엔진에 Update를 직접 등록하지 않는다.)

---

## 2. 아키텍처 위치

```
MonoBehaviour   MainManager                 ← 이 문서의 대상 [구현완료]
(씬 진입점)         ↓ 소유·초기화
                ├── FieldManager            (Kompile.Field.Manager) [구현완료]
                ├── (미설계) BattleManager
                └── (미설계) IngameEventManager
```

**레이어 예외 사항**  
project-instructions.md의 Manager 레이어 규칙("plain class, MonoBehaviour 아님")은 FieldManager 등 콘텐츠 Manager에 해당한다.
MainManager는 그 위의 진입점으로, MonoBehaviour를 직접 상속하는 **유일한** Manager급 클래스다.

---

## 3. 클래스 구조 [구현완료]

```csharp
using Kompile.Field.Manager;
using UnityEngine;

namespace Kompile
{
    public class MainManager : MonoBehaviour
    {
        // --- Content Managers ---
        private FieldManager _fieldManager;
        // (미설계) private BattleManager _battleManager;
        // (미설계) private IngameEventManager _ingameEventManager;

        // --- Root Transforms ---
        private Transform _fieldRoot;   // Awake에서 new GameObject("Field")로 생성

        // --- Life Cycle (Unity) ---
        private void Awake() { ... }
        private void Start() { ... }
        private void Update() { ... }
        private void OnDestroy() { ... }
    }
}
```

---

## 4. 생명주기 시퀀스 [구현완료]

### 4.1 Awake — Manager 생성·초기화 (모든 Manager를 Awake에서 일괄 생성)

```
MainManager.Awake()
    ↓
_fieldRoot 준비
    var fieldGo = new GameObject("Field")
    fieldGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity)
    _fieldRoot = fieldGo.transform
    ↓
_fieldManager = new FieldManager(_fieldRoot)
    ↓ (FieldManager 생성자 내부)
    _mapRoot 생성 → new MapManager(_mapRoot)
    → new FieldMapQueryService(_mapManager)
    // FieldMapLayerService: 미구현 (추후 추가)
    ↓
// (미설계) _battleManager      = new BattleManager(...)
// (미설계) _ingameEventManager = new IngameEventManager(...)
```

### 4.2 Start — 콘텐츠 시작

```
MainManager.Start()
    ↓
_fieldManager.StartFieldAsync(Camera.main.transform)   ← Fire and forget
    ↓ (MapManager 스트리밍 루프 시작, CHECK_INTERVAL=0.5초 주기)
// (미설계) _battleManager.Start(...)
// (미설계) _ingameEventManager.Start(...)
```

> 모든 Manager는 `Awake`에서 일괄 생성한다. 초기화 순서 의존성이 있을 경우 `Awake` 내부에서
> `Awaitable`을 사용한 비동기 생성을 고려할 수 있다.
> `Start`는 Manager 생성 이후 콘텐츠 시작 메서드 호출 전용으로 사용한다 (Unity 관례).

### 4.3 Update — 순차 Update 위임

```
MainManager.Update()
    ↓ 순서 고정 (아래 순서대로)
_fieldManager.Update()
    // (미설계) _battleManager.Update()
    // (미설계) _ingameEventManager.Update()
```

- 각 Manager는 MonoBehaviour를 상속받지 않으므로 Unity `Update` 루프에 직접 등록되지 않는다.
- 호출 순서는 MainManager.Update() 내부에서 **명시적으로** 고정한다.
- 신규 Manager 추가 시 이 순서에 명시적으로 삽입한다.

### 4.4 OnDestroy — Dispose

```
MainManager.OnDestroy()
    ↓
_fieldManager.Dispose()       ← MapManager 에셋 전체 해제
// (미설계) _battleManager.Dispose()
// (미설계) _ingameEventManager.Dispose()
```

> 게임 재시작은 씬 재로드 없이 동일 씬에서 각 Manager의 `Dispose()` 호출 후 재초기화하는 방식으로 구현한다.

---

## 5. 관리 대상 콘텐츠 [설계]

| Manager | 역할 | 상태 |
|---------|------|------|
| `FieldManager` | 필드 맵 스트리밍, 유닛, 인게임 이벤트 등 필드 내 콘텐츠 일괄 조율 | **구현완료** (맵 스트리밍까지. [FieldManager.md](FieldManager.md) 참고) |
| `BattleManager` | 전투 콘텐츠 | 미설계 |
| `IngameEventManager` | 인게임 이벤트 콘텐츠 | 미설계 |

---

## 6. 미결정 사항

| 항목 | 내용 |
|------|------|
| BattleManager 설계 | 구조·책임 범위 미결정 |
| IngameEventManager 설계 | 구조·책임 범위 미결정 |

---

## 7. 연관 파일 목록

| 파일 | 레이어 | 역할 | 상태 |
|------|--------|------|------|
| `MainManager.cs` | MonoBehaviour | 이 문서의 대상. 씬 진입점 | **구현완료** |
| `FieldManager.cs` | Manager | 필드 콘텐츠 일괄 조율. MainManager가 소유·초기화 | **구현완료** (맵 스트리밍까지) |
| `MapManager.cs` | Manager | 그리드 스트리밍·렌더링. FieldManager가 소유 | 기존 코드 + `DisposeAll()` 추가 + 스트리밍 버그 수정 |
