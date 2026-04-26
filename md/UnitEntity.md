# Unit 스크립트 분석 문서
> 경로: `Assets/Script/Unit/` | 분석 기준: 2026-04-26  
> 목적: 현재 코드 구조 파악 + UnitEntity 관련 요청 시 컨텍스트 제공

---

## 1. 디렉토리 구조

```
Script/Unit/
├── Data/
│   ├── Definition/
│   │   ├── UnitBrainType.cs           — enum, 유닛 행동 패턴 분류
│   │   ├── UnitType.cs                — enum, 유닛 대분류 (팩션/생명주기)
│   │   └── UnitAnimCmd.cs             — enum, Brain→AnimComponent 트리거 명령 (신규)
│   └── Context/
│       ├── UnitRuntimeContext.cs      — struct, 유닛 런타임 상태 묶음
│       ├── UnitIntent.cs              — struct, Brain 의사결정 결과값 (신규)
│       └── Brain/
│           ├── IUnitBrain.cs          — interface, Brain 전략 패턴 계약
│           ├── PlayerControlBrain.cs  — class, 플레이어 입력 → UnitIntent 반환
│           └── UnitEntityBase.cs      — abstract class, 모든 유닛 Entity 베이스
├── Entity/
│   ├── IMovable.cs                    — interface, 이동 입력 수신 (Brain 흐름에서는 미사용 — 외부 주입용 보존)
│   ├── UnitAnimComponent.cs           — ⚠️ 구버전 (Entity/ → Component/ 이동 대상)
│   └── UnitMoveComponent.cs           — ⚠️ 구버전 (Entity/ → Component/ 이동 대상)
├── Component/
│   ├── UnitAnimComponent.cs           — MonoBehaviour, 애니메이션 전담 ✅ 정식
│   └── UnitMoveComponent.cs           — MonoBehaviour, 이동·충돌·높이 샘플링 ✅ 정식
└── Manager/
    └── x_UnitManager.cs               — ⚠️ 폐기 예정
```

---

## 2. Brain → Entity → Component 전체 흐름

```
콘텐츠 매니저 (FieldManager 등)
  → entity.Update() 매 프레임 수동 호출

entity.Update()
  1. UnitIntent intent = _brain?.Update() ?? UnitIntent.Empty
  2. _moveComponent.Update_(in intent)   — 이동 처리
  3. _animComponent.Update_(in intent)   — 애니메이션 처리

Brain.Update() 내부
  → 입력 또는 AI 로직으로 intent 필드 채워 반환
      ├── intent.MoveInput    : Vector2 (XZ 방향)
      └── intent.AnimCommand  : UnitAnimCmd (None이면 무시)

AnimComponent.Update_(in intent) 내부
  → UpdateMovementAnim(MoveInput.magnitude, dir)   — 루프 상태 (Idle/Walk)
  → ApplyAnimCommand(AnimCommand)                  — 트리거 상태 (Attack/Hit/Dead)

MoveComponent.Update_(in intent) 내부
  → _moveInput = intent.MoveInput
  → CheckWalkable → SampleHeight → transform.position 갱신
```

**UI는 이 파이프라인과 분리한다.** HP, 쿨다운 등은 매 프레임 갱신이 아닌 이벤트 발행(`event Action<int> OnHPChanged` 등)으로 처리한다.

---

## 3. 파일별 상세 분석

### 3-1. Data/Definition/UnitType.cs
`enum UnitType` — `Player`, `PartyGroup`, `NPC`, `Enemy`

### 3-2. Data/Definition/UnitBrainType.cs
`enum UnitBrainType` — `PlayerControl`(구현), `Ataho`(미구현), `E_Monkey`(미구현)

### 3-3. Data/Definition/UnitAnimCmd.cs ✅ 신규
```
네임스페이스: Kompile.Unit.Data
```
`enum UnitAnimCmd` — Brain이 AnimComponent에 전달하는 트리거성 명령.

| 값 | 의미 |
|---|---|
| `None` | 명령 없음 (AnimComponent 무시) |
| `Attack` | 공격 애니메이션 트리거 |
| `Hit` | 피격 애니메이션 트리거 |
| `Dead` | 사망 애니메이션 세팅 |

루프 상태(Idle/Walk)는 `UnitIntent.MoveInput.magnitude`로 AnimComponent가 결정하므로 여기에 포함하지 않는다.

---

### 3-4. Data/Context/UnitRuntimeContext.cs
`struct UnitRuntimeContext` — `Type`, `BrainType`, `IsDead` 묶음.

### 3-5. Data/Context/UnitIntent.cs ✅ 신규
```
네임스페이스: Kompile.Unit.Data
```
`struct UnitIntent` — Brain이 한 프레임에 내리는 의사결정 결과를 담는 값 타입.

| 멤버 | 타입 | 소비처 |
|---|---|---|
| `Empty` | `static readonly UnitIntent` | Brain 없거나 비활성 시 기본값 |
| `MoveInput` | `Vector2` | MoveComponent, AnimComponent |
| `AnimCommand` | `UnitAnimCmd` | AnimComponent |

struct이므로 매 프레임 생성해도 GC 부담 없음. `in` 한정자로 복사 없이 Component에 전달.

---

### 3-6. Data/Context/Brain/IUnitBrain.cs
```
네임스페이스: Kompile.Unit.Entity
```
`interface IUnitBrain` — Brain 전략 패턴 계약.

```csharp
void       Initialize(UnitEntityBase ownerEntity);
UnitIntent Update();    // 의사결정 결과 반환 (void → UnitIntent 변경)
void       Clear();
```

---

### 3-7. Data/Context/Brain/UnitEntityBase.cs
```
네임스페이스: Kompile.Unit.Entity
상속: Entity → MonoBehaviour
```
`abstract class UnitEntityBase` — 모든 유닛 Entity의 공통 베이스.

**핵심 멤버:**

| 멤버 | 타입 | 설명 |
|---|---|---|
| `_brain` | `IUnitBrain` | 교체 가능한 행동 패턴 객체 |
| `_instanceID` | `long` | Manager Dictionary 키 |
| `_isInitialized` | `bool` | 초기화 완료 여부 |
| `_context` | `UnitRuntimeContext` | 유닛 런타임 상태 |

**핵심 메서드:**

| 메서드 | 설명 |
|---|---|
| `SetBrain(IUnitBrain)` | Brain 교체. 기존 `Clear()` → 신규 `Initialize()` |
| `SetBrain()` | `_context.BrainType` switch로 내부 Brain 자동 생성 |
| `Clear()` | 풀 반환 시 초기화 (virtual) |
| `Initialize(long, UnitRuntimeContext)` | abstract |
| `Update()` | abstract — 콘텐츠 매니저가 매 프레임 수동 호출 |

**설계 노트:** 템플릿 메서드 패턴 미적용. 콘텐츠마다 컴포넌트 구성이 다르므로 하위 클래스가 `Update()` 내부에서 자유롭게 파이프라인 구성.

---

### 3-8. Data/Context/Brain/PlayerControlBrain.cs
```
네임스페이스: Kompile.Unit.Entity
```
`class PlayerControlBrain : IUnitBrain` — 플레이어 입력을 읽어 `UnitIntent`로 반환하는 Brain.

**동작 흐름:**
```
IngameInputProvider.Current → InputState
IsPressing(방향) → MoveInput(x, z) 계산
OnEndOfFrame()
return new UnitIntent { MoveInput, AnimCommand = None }
```

**변경 이력 (2026-04-26):**
- `void Update()` → `UnitIntent Update()`
- `IMovable _movable` 제거 — Entity에 직접 Push하지 않고 Intent 반환으로 전환
- `using Kompile.Field.Entity` 없음 — Field 레이어 의존 없음

**미처리:** `using Script.Input.*` 네임스페이스 → `Kompile.Input.*` 정비 필요 (InputProvider .md 확정 후)

---

### 3-9. Component/UnitAnimComponent.cs ✅ 정식
```
네임스페이스: Kompile.Unit.Component
```

**Update_ 흐름:**
```csharp
public void Update_(in UnitIntent intent)
  → UpdateMovementAnim(MoveInput.magnitude, dir)  // Idle/Walk 루프 상태
  → ApplyAnimCommand(AnimCommand)                 // Attack/Hit/Dead 트리거
```

`ApplyAnimCommand()`는 `switch`로 `PlayAttackAnimation()` 등 기존 메서드를 호출한다.

---

### 3-10. Component/UnitMoveComponent.cs ✅ 정식
```
네임스페이스: Kompile.Unit.Component
```

**Update_ 흐름:**
```csharp
public void Update_(in UnitIntent intent)
  → _moveInput = intent.MoveInput
  → CheckWalkable → SampleHeight → transform.position 갱신
```

---

### 3-11. Entity/IMovable.cs
```
네임스페이스: Kompile.Unit.Entity
```
`interface IMovable` — `SetMoveInput(Vector2)` 계약.

Brain→Component 흐름이 `UnitIntent`로 전환되면서 Brain에서는 더 이상 사용되지 않음. 외부 시스템(컷씬, 넉백 등)이 Brain 없이 Entity에 직접 이동 입력을 밀어넣어야 하는 경우를 위해 보존.

---

## 4. 클래스 관계도

```
[ Data Layer ]
  UnitAnimCmd   ──────────────────────────────────┐
  UnitType                                        │
  UnitBrainType ──► UnitRuntimeContext            │
                         │                        ▼
[ Entity Layer ]   UnitEntityBase         UnitIntent (struct)
                         │                   ↑          ↓
                         ├── _brain : IUnitBrain     배분
                         │       └── PlayerControlBrain
                         │               └── IngameInputProvider
                         │
                         │   [Component Layer]
                         ├── UnitAnimComponent  — Update_(in UnitIntent)
                         └── UnitMoveComponent  — Update_(in UnitIntent)
```

---

## 5. 규칙 위반 / 미처리 항목

### [완료] FieldPlayerEntity — IMovable 선언 + Update() 구현 ✅
- **위치**: `Script/Field/Entity/FieldPlayerEntity.cs`
- **처리 내용**:
  - `: IMovable` 선언 — 완료
  - `Update()` 내부에서 `UnitIntent intent = _brain != null ? _brain.Update() : UnitIntent.Empty;` 수신 후 `_moveComponent.Update_(in intent)`, `_animComponent.Update_(in intent)` 배분 — 완료
  - `SetMoveInput(Vector2)` — Brain을 거치지 않는 외부 주입 경로. `UnitIntent` 생성 후 `_moveComponent.Update_(in intent)` 직접 호출로 재구현

### [완료] Entity/ 구버전 파일 삭제 ✅
- `Entity/UnitAnimComponent.cs`, `Entity/UnitMoveComponent.cs` 삭제 완료

### [미처리 3] Brain 파일 위치 이상
- `Data/Context/Brain/` → `Entity/Brain/`으로 이동 권장

### [완료] PlayerControlBrain 입력 네임스페이스 정비 ✅
- `IngameInputProvider.cs` — `namespace Script.Input.Provider` → `Kompile.Input.Provider`
- `Definition_Input.cs` — `namespace Script.Input.Data` → `Kompile.Input.Data`
- `PlayerControlBrain.cs` — using 문 양쪽 `Kompile.Input.*`로 변경

---

## 6. Update 호출 명칭 규칙

| 대상 | 메서드명 | 비고 |
|---|---|---|
| `IUnitBrain` 구현체 (순수 C#) | `Update()` → `UnitIntent` 반환 | MonoBehaviour 아님 |
| `UnitEntityBase` 하위 (MonoBehaviour) | `Update()` | abstract, 콘텐츠 매니저가 수동 호출 |
| `UnitMoveComponent` (MonoBehaviour) | `Update_(in UnitIntent)` | Unity 자동 호출 방지 |
| `UnitAnimComponent` (MonoBehaviour) | `Update_(in UnitIntent)` | Unity 자동 호출 방지 |

---

## 7. 레이어별 구현 상태

| 레이어 | 상태 |
|---|---|
| Data/Definition (UnitType, UnitBrainType, UnitAnimCmd) | ✅ 완성 |
| Data/Context (UnitRuntimeContext, UnitIntent) | ✅ 완성 |
| Entity/IMovable | ✅ 완성 (외부 주입용 보존) |
| Entity (IUnitBrain, UnitEntityBase) | ✅ 완성 |
| Entity/Brain/PlayerControlBrain | ⚠️ Input 네임스페이스 정비 필요 |
| Component/UnitAnimComponent | ✅ 완성 |
| Component/UnitMoveComponent | ✅ 완성 |
| Field/Entity/FieldPlayerEntity | ✅ 완성 |
| Manager/x_UnitManager | 🗑️ 폐기 예정 |
