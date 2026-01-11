namespace Script.GameSystem
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Script.Data;
    using static Script.Data.DataType;

    public class GameplayInputSystem : ISystem
    {
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;
        private readonly InputAction cancelInput;

        private IDxInput inputFlag;
        private IDxInput prevInputFlag; // 이전 프레임 입력값

        public InputState Current => new InputState(inputFlag, prevInputFlag);

        public GameplayInputSystem()
        {
            inputFlag = IDxInput.NONE;

            moveInput = new InputAction("Move", InputActionType.Value);
            moveInput.AddCompositeBinding("2DVector")
                     .With("Up", "<Keyboard>/upArrow")
                     .With("Down", "<Keyboard>/downArrow")
                     .With("Left", "<Keyboard>/leftArrow")
                     .With("Right", "<Keyboard>/rightArrow");
            moveInput.performed += OnMovePerformed;
            moveInput.canceled += OnMoveCanceled;

            enterInput = new InputAction("Enter", InputActionType.Button);
            enterInput.AddBinding("<Keyboard>/z");
            enterInput.started += (context) =>
            {
                inputFlag |= IDxInput.ENTER;
            };
            enterInput.canceled += (context) =>
            {
                inputFlag &= ~IDxInput.ENTER;
            };

            cancelInput = new InputAction("Enter", InputActionType.Button);
            cancelInput.AddBinding("<Keyboard>/x");
            cancelInput.started += (context) =>
            {
                inputFlag |= IDxInput.CANCEL;
            };
            cancelInput.canceled += (context) =>
            {
                inputFlag &= ~IDxInput.CANCEL;
            };

            actionInput = new InputAction("Action", InputActionType.Button);
            actionInput.AddBinding("<Keyboard>/space");
            actionInput.started += (context) =>
            {
                inputFlag |= IDxInput.ACTION;
            };
            actionInput.performed += (context) =>
            {
                inputFlag |= IDxInput.ACTION;
            };
            actionInput.canceled += (context) =>
            {
                inputFlag &= ~IDxInput.ACTION;
            };

            moveInput.Enable();
            enterInput.Enable();
            actionInput.Enable();
            cancelInput.Enable();

            //cts = new CancellationTokenSource();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();

            inputFlag &= ~IDxInput.MOVE_ALL;
            if (direction.x > 0.1f) { inputFlag |= IDxInput.RIGHT; }
            if (direction.x < -0.1f) { inputFlag |= IDxInput.LEFT; }
            if (direction.y > 0.1f) { inputFlag |= IDxInput.UP; }
            if (direction.y < -0.1f) { inputFlag |= IDxInput.DOWN; }
        }
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            inputFlag &= ~IDxInput.MOVE_ALL;
        }

        // [신규] 프레임 끝에서 호출: 현재 입력을 과거로 저장
        public void OnEndOfFrame()
        {
            prevInputFlag = inputFlag;
        }

        // 현재 구조에선 입력 취소 토큰을 사용할 이유가 없음;
        //private CancellationTokenSource cts;
        //public CancellationToken Token => cts.Token;
        //public void Reset()
        //{
        //    cts.Cancel();
        //    cts.Dispose();
        //    cts = new CancellationTokenSource();

        //    inputFlag = IDxInput.NONE;
        //}
    }
}