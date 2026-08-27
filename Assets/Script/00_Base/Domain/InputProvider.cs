namespace Kompile.Domain
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Data;

    public class InputProvider
    {
        // 입력 액션 정의
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;
        private readonly InputAction cancelInput;

        // 상태 변수 (빠른 입력 처리를 위해 분리)
        private IDxInput rawInputFlag;      // 실시간 물리적 입력 상태 (하드웨어 동기화)
        private IDxInput latchedInputFlag;  // 이번 프레임에 발생한 모든 입력 누적 (매니저 전달용)
        private IDxInput prevInputFlag;     // 이전 프레임의 최종 상태 (IsDown/IsUp 판별용)

        // 외부에서는 이 속성을 통해 안전하게 처리된 상태를 가져갑니다.
        public InputState Current => new InputState(latchedInputFlag, prevInputFlag);

        public InputProvider()
        {
            // 초기화
            rawInputFlag = IDxInput.NONE;
            latchedInputFlag = IDxInput.NONE;
            prevInputFlag = IDxInput.NONE;

            moveInput = new InputAction("Move", InputActionType.Value);
            enterInput = new InputAction("Enter", InputActionType.Button);
            cancelInput = new InputAction("Cancel", InputActionType.Button);
            actionInput = new InputAction("Action", InputActionType.Button);

            AddBinding_Keyboard();
            AddBinding_JoyPad();

            moveInput.performed += OnMovePerformed;
            moveInput.canceled += OnMoveCanceled;

            enterInput.started += _ =>
            {
                rawInputFlag |= IDxInput.ENTER;
                latchedInputFlag |= IDxInput.ENTER; // 누르는 순간 즉시 기록
            };
            enterInput.canceled += _ =>
            {
                rawInputFlag &= ~IDxInput.ENTER;
                // 주의: 뗄 때는 raw만 끕니다. latched는 프레임 끝까지 유지합니다.
            };

            cancelInput.started += _ =>
            {
                rawInputFlag |= IDxInput.CANCEL;
                latchedInputFlag |= IDxInput.CANCEL;
            };
            cancelInput.canceled += _ =>
            {
                rawInputFlag &= ~IDxInput.CANCEL;
            };

            actionInput.started += _ =>
            {
                rawInputFlag |= IDxInput.ACTION;
                latchedInputFlag |= IDxInput.ACTION;
            };
            actionInput.canceled += _ =>
            {
                rawInputFlag &= ~IDxInput.ACTION;
            };

            // 모든 입력 액션 활성화
            moveInput.Enable();
            enterInput.Enable();
            actionInput.Enable();
            cancelInput.Enable();
        }


        // --- Binding ---
        private void AddBinding_Keyboard()
        {
            moveInput.AddCompositeBinding("2DVector")
                     .With("Up", "<Keyboard>/upArrow")
                     .With("Down", "<Keyboard>/downArrow")
                     .With("Left", "<Keyboard>/leftArrow")
                     .With("Right", "<Keyboard>/rightArrow");

            enterInput.AddBinding("<Keyboard>/z");
            cancelInput.AddBinding("<Keyboard>/x");
            actionInput.AddBinding("<Keyboard>/space");
        }
        private void AddBinding_JoyPad()
        {
            // [게임패드] D-Pad (십자키)
            //moveInput.AddCompositeBinding("2DVector")
            //         .With("Up", "<Gamepad>/dpad/up")
            //         .With("Down", "<Gamepad>/dpad/down")
            //         .With("Left", "<Gamepad>/dpad/left")
            //         .With("Right", "<Gamepad>/dpad/right");

            // [게임패드] 왼쪽 아날로그 스틱
            moveInput.AddCompositeBinding("2DVector")
                     .With("Up", "<Gamepad>/leftStick/up")
                     .With("Down", "<Gamepad>/leftStick/down")
                     .With("Left", "<Gamepad>/leftStick/left")
                     .With("Right", "<Gamepad>/leftStick/right");

            enterInput.AddBinding("<Gamepad>/buttonSouth");
            cancelInput.AddBinding("<Gamepad>/buttonEast");
            actionInput.AddBinding("<Gamepad>/buttonWest"); // Xbox: X버튼, 듀얼쇼크: 네모버튼
        }


        // --- On Event ---
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();

            // raw 상태: 기존 이동 값 지우고 현재 값으로 갱신
            rawInputFlag &= ~IDxInput.MOVE_ALL;

            IDxInput tempMove = IDxInput.NONE;
            if (direction.x >  0.1f) { tempMove |= IDxInput.RIGHT; }
            if (direction.x < -0.1f) { tempMove |= IDxInput.LEFT;  }
            if (direction.y >  0.1f) { tempMove |= IDxInput.UP;    }
            if (direction.y < -0.1f) { tempMove |= IDxInput.DOWN;  }

            rawInputFlag |= tempMove;

            // latched 상태: 이번 프레임에 있었던 이동 입력을 누적 (OR 연산)
            latchedInputFlag |= tempMove;
        }
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            // 이동 멈춤: 물리 상태(raw)는 즉시 해제
            rawInputFlag &= ~IDxInput.MOVE_ALL;
        }
        // MainBehaviour의 Update 마지막에 반드시 호출해야 함
        public void OnEndOfFrame()
        {
            // 1. 현재 프레임의 처리 결과(latched)를 '과거'로 저장
            prevInputFlag = latchedInputFlag;

            // 2. 다음 프레임을 위해 latched 초기화
            // 0으로 초기화하는 것이 아니라, '현재 누르고 있는 키(raw)' 상태로 동기화
            // -> 키를 꾹 누르고 있을 때(Hold) 다음 프레임에도 입력이 이어진다.
            latchedInputFlag = rawInputFlag;
        }
    }
}
