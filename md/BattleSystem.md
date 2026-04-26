# Battle System 코드 분석

> 분석 기준일: 2026-04-26  
> 최종 수정일: 2026-04-26  
> 대상 경로: `Assets/Script/Battle`  
> 목적: 현재 구현 상태 파악 + 향후 작업 요청 기반 문서

---

## 1. 파일 구조

```
Battle/
├── Data/
│   ├── Context/
│   │   └── BattleUnitContext.cs       ← 런타임 유닛 상태 객체
│   ├── Definition/
│   │   ├── BattleAnimationCommand.cs  ← 애니메이션 실행 명령 struct
│   │   └── ITimelineHandler.cs        ← 타임라인 이벤트 콜백 인터페이스
│   └── Table/
│       └── BattleSkillData.cs         ← 스킬 테이블 데이터 struct
├── Entity/
│   └── BattleUnitAnimationComponent.cs ← Animator 수동 샘플링 컴포넌트
├── Manager/
│   ├── BattleManager.cs               ← 전투 전체 제어 (순수 C# 클래스)
│   └── BattleTimelineManager.cs       ← 논리 프레임 구동 엔진 (순수 C# 클래스)
├── RepoProvider/
│   └── BattleSkillRepoProvider.cs     ← 스킬 데이터 조회 및 Command 변환
└── Utility/
    └── BattleUtil.cs                  ← 수학/판정 연산 (BurstCompile)
```

---

## 2. 레이어별 역할 요약

### Data

| 파일 | 타입 | 역할 |
|---|---|---|
| `BattlePhase` (enum) | enum | `Wait` / `Action` 두 페이즈 정의 |
| `BattleUnitContext` | class | 유닛의 런타임 상태 (거리, 페이즈, 속도) 추적 |
| `BattleAnimationCommand` | struct | 애니메이터에 넘길 실행 명령 (StateHash, 프레임 정보) |
| `ITimelineHandler` | interface | `OnTargetFrameReached()` 콜백 계약 |
| `BattleSkillData` | struct | 스킬 테이블 원본 데이터 (ID, 애니메이션명, 틱 수치) |

> **BattleUnitContext가 class인 이유:** 인게임 실시간 상태 변경이 빈번하고 여러 곳에서 참조·수정되므로 struct의 복사 비용과 Dictionary 재삽입 문제를 피하기 위해 class를 유지한다. Data 레이어 Value 중심 원칙의 의도적 예외.

### RepoProvider

| 파일 | 역할 |
|---|---|
| `BattleSkillRepoProvider` | `List<BattleSkillData>` 보관. `GetSkillCommand(skillID, currentTotalFrames)` 호출 시 `BattleSkillData` → `BattleAnimationCommand`로 변환하여 반환 |

### Manager

| 파일 | 역할 |
|---|---|
| `BattleManager` | 순수 C# 클래스. MainManager가 생성·소유하고 매 프레임 `Update(float deltaTime)` 호출. 유닛 컨텍스트(`_contexts`) 및 엔티티(`_units`) 관리. 매 틱 `ProcessBattleLogic()` 실행. 인터럽트·페이즈 전환 제어 |
| `BattleTimelineManager` | 순수 C# 클래스. deltaTime 누산(Accumulator 패턴)으로 논리 프레임 생성. HitStop, FastForward, 감속 연출 담당 |

### Entity / Component

| 파일 | 역할 |
|---|---|
| `BattleUnitAnimationComponent` | MonoBehaviour. `_animator.speed = 0`으로 유지 후 `Sample(currentFrame, fpt)`로 수동 재생. 히트 판정 여부를 폴링 방식(`CheckHitTriggered`)으로 반환 |

### Utility

| 파일 | 역할 |
|---|---|
| `BattleUtil` | 정적 클래스. BurstCompile 대상. 진행량 계산(`CalculateProgressPerTick`), 콤보 윈도우 판정(`IsInsideComboWindow`), 잔여 틱 예측(`PredictRemainingTicks`) 등 |

---

## 3. 핵심 메커니즘

### 3-1. CTB(Charge Turn Battle) 방식 턴 진행

유닛은 두 페이즈를 순환한다.

```
[Wait Phase]
  RemainingDistance (10000f → 0f)
  매 틱: RemainingDistance -= currentSpeed * ORDER_PER_TICK (10.0f)
  → 0 도달 시 Action Phase로 전환, ActionDistance = 2000f 설정

[Action Phase]
  ActionDistance (2000f → 0f)
  매 틱: ActionDistance -= currentSpeed * ORDER_PER_TICK
  → 0 도달 시 Wait Phase로 복귀, RemainingDistance = 10000f 리셋
```

속도 예시 (ORDER_PER_TICK = 10):

| speed | Wait 완료까지 틱 수 | 24fps/6fpTick 기준 실시간 |
|---|---|---|
| 10 | 100틱 | 25초 |
| 50 | 20틱 | 5초 |
| 100 | 10틱 | 2.5초 |

### 3-2. 인터럽트 (Interrupt)

`TriggerInterrupt(unitID, skillID)` 호출 시:
1. `_isInterrupting = true` → `ProcessBattleLogic()` 전체 정지
2. 대상 유닛의 `RemainingDistance = 0`, `Phase = Action`, `ActionDistance = 1500f`로 강제 전환
3. 해당 유닛의 Action Phase 종료 시 `_isInterrupting = false`로 시간 재개
- 스킬 애니메이션 연결 코드는 현재 **주석 처리** 상태

### 3-3. 논리 프레임 엔진 (BattleTimelineManager)

```
상수: TARGET_FPS = 24, FRAMES_PER_TICK = 6
→ 논리 틱 = 매 6프레임 = 약 0.25초

Accumulator 패턴:
  _accumulatedTime += deltaTime * timeScale
  while (_accumulatedTime >= _timePerFrame):
      _accumulatedTime -= _timePerFrame
      _currentTotalFrames++

과부하 방지: 한 Update에서 FRAMES_PER_TICK + 2 이상 처리 시 저금통 초기화
```

시간 배율 우선순위:

| 우선순위 | 조건 | 배율 |
|---|---|---|
| 1 | HitStop 진행 중 | `_hitStopScale` (기본 0.1) |
| 2 | TargetFrame 설정 중 | `BattleUtil.GetRequiredTimeScale()` (감속) |
| 3 | 평상시 | 1.0f |

### 3-4. 애니메이션 수동 샘플링

```
BattleUnitAnimationComponent.Sample(currentFrame, fpt):
  elapsed = currentFrame - cmd.StartFrame
  total   = cmd.TotalTicks * fpt
  animator.Play(stateHash, 0, elapsed / total)  ← normalized time으로 직접 지정
  elapsed >= total 이면 _isActive = false
```

Unity 이벤트/콜백 없이 매 프레임 `Sample()`을 호출해 애니메이터를 직접 구동한다.  
히트 판정도 마찬가지로 `CheckHitTriggered(currentFrame)` 폴링으로 처리한다.

### 3-5. 스킬 데이터 흐름

```
BattleSkillData (Table)
    ↓ BattleSkillRepoProvider.GetSkillCommand(skillID, currentFrame)
BattleAnimationCommand (Runtime)
    ↓ BattleUnitAnimationComponent.Play(cmd)
Animator (Visual)
```

`BattleSkillData.AnimationStateName` → `Animator.StringToHash()` → `BattleAnimationCommand.StateHash`

---

## 4. 클래스 의존 관계

```
MainManager
  └── owns & calls Update(deltaTime) ──► BattleManager
                                              │
                                              ├── uses ──► BattleTimelineManager
                                              │                 └── uses ──► BattleUtil (GetRequiredTimeScale)
                                              │
                                              ├── uses ──► BattleSkillRepoProvider
                                              │                 └── uses ──► BattleSkillData (Table)
                                              │                              → produces ──► BattleAnimationCommand
                                              │
                                              ├── holds ──► BattleUnitContext (Dict<long, BattleUnitContext>)
                                              ├── holds ──► UnitEntityBase   (Dict<long, UnitEntityBase>)  ※ 현재 미연결
                                              │
                                              └── implements ──► ITimelineHandler

BattleUnitAnimationComponent
     └── uses ──► BattleAnimationCommand
     └── wraps ──► Animator (Unity)

BattleUtil
     └── uses ──► BattleAnimationCommand (IsInsideComboWindow)
```

---

## 5. 미완성 / 주석 처리된 코드

### 5-1. RegisterUnit (BattleManager)

```csharp
// public void RegisterUnit(BattleUnitEntity unit, int baseSpeed)
// {
//     long newID = ++_unitIDCounter;
//     unit.EntityID = newID;
//     _units.Add(newID, unit);
//     _contexts.Add(newID, new BattleUnitContext(newID, baseSpeed));
//     unit.Animation.Init();
// }
```
- `BattleUnitEntity` 타입 자체가 미정의 상태. 유닛 등록 불가.
- `_units` Dictionary가 비어있어 실질적으로 아무 유닛도 전투에 참여하지 않음.

### 5-2. UpdateVisuals (BattleManager)

```csharp
private void UpdateVisuals(int currentFrame)
{
    // foreach (var unit in _units.Values)
    // {
    //     unit.Animation.Sample(currentFrame, FRAMES_PER_TICK);
    // }
}
```
- `BattleUnitAnimationComponent`와 `BattleManager`의 연결이 끊긴 상태.

### 5-3. TriggerInterrupt 내 스킬 애니메이션

```csharp
// var cmd = _skillProvider.GetSkillCommand(interruptSkillID, _timelineManager.TotalFrames);
// unit.Animation.Play(cmd);
```
- 인터럽트 스킬의 애니메이션 재생이 미연결 상태.

### 5-4. _isWaitingForCombo (BattleManager)

```csharp
private bool _isWaitingForCombo = false;
```
- 선언만 존재하고 사용처 없음. 콤보 입력 대기 로직 미구현.

---

## 6. 규칙 적합성 현황

### ✅ 수정 완료 항목

| 항목 | 내용 |
|---|---|
| 네임스페이스 | `Script.Battle.*` → `Kompile.Battle.*` 전체 변경 완료 |
| Lambda 제거 | `BattleSkillRepoProvider.GetSkillCommand()`: `FindIndex(Lambda)` → manual `for` loop |
| 오타 수정 | `BattleUtil.CalculatorOrderToFrames` → `CalculateOrderToFrames` |
| BattleManager 구조 | MonoBehaviour 제거 → 순수 C# 클래스. `Awake()` → 생성자, `Update()` → `Update(float deltaTime)` |

### ✅ 의도적 예외 항목

| 항목 | 판단 |
|---|---|
| `BattleUnitContext`가 class | 인게임 실시간 변경이 많고 다수 참조 구조상 struct 부적합. class 유지 확정 |

---

## 7. 미결정 / 설계 공백

| 항목 | 현재 상태 | 필요한 결정 |
|---|---|---|
| BattleManager 생명주기 | MainManager가 생성·Update 호출 예정 | MainManager 연결 구현 필요 |
| IngameEventManager | 미존재 | 전투 이벤트(스킬 발동, 데미지, 사망 등) 처리 주체 |
| BattleUnitEntity 정의 | 미정의 | UnitEntityBase 상속 구조, Animation 컴포넌트 연결 방식 |
| 콤보 시스템 | `_isWaitingForCombo` 선언만 존재 | 콤보 입력 타이밍, 커맨드 구조 |
| 인터럽트 스킬 애니메이션 연결 | 주석 처리 | RegisterUnit 구현 이후 연결 가능 |

---

## 8. 작동 흐름 요약 (현재 구현 기준)

```
MainManager (외부)
  └── new BattleManager()
        └── 생성자 내부:
              BattleTimelineManager 생성 (handler=this, fps=24, fpt=6)
              BattleSkillRepoProvider 생성
              _timelineManager.Play()

MainManager.Update(deltaTime)
  └── battleManager.Update(deltaTime)
        └── OnUpdateTick(deltaTime) → true이면 (논리 프레임 진행 시)
              ├── UpdateVisuals(currentFrame)      ← 현재 주석 처리 (미동작)
              └── if frame % 6 == 0:
                    ProcessBattleLogic()
                      └── _contexts 순회
                            ├── Wait: RemainingDistance -= progress → 0이면 Action으로
                            └── Action: ActionDistance -= progress → 0이면 Wait으로

TriggerInterrupt(unitID, skillID)  ← 외부 호출
  └── _isInterrupting = true → ProcessBattleLogic 전체 정지
  └── 대상 유닛 강제 Action Phase 진입 (ActionDistance=1500f)
```

현재 `_units`와 `_contexts`가 모두 비어있어, `ProcessBattleLogic()`은 실행되지만 아무 유닛도 처리하지 않는 상태.

---

## 9. 향후 작업 시 참고 사항

### MainManager 연결

```csharp
// MainManager 내부 예시
private BattleManager _battleManager;

// 전투 시작 시
_battleManager = new BattleManager();

// Update() 내부
_battleManager.Update(Time.deltaTime);
```

### 유닛 등록 연결을 위해 필요한 것

1. `BattleUnitEntity` 타입 정의 (UnitEntityBase 상속, Animation 컴포넌트 포함)
2. `RegisterUnit()` 주석 해제 및 타입 수정
3. `UpdateVisuals()` 주석 해제
