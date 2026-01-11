namespace Script.GamePlay
{
    using Script.Data;
    using UnityEngine;
    using UnityEngine.UI;

    public class UITitleMenuObject : MonoBehaviour, IGameUpdater
    {
        [SerializeField] private Transform menuParent;
        [SerializeField] private Image selectSlotImage;

        private readonly float MIN_ALPHA = 0.3f;
        private readonly float MAX_ALPHA = 0.7f;
        private readonly float ALPHA_DELTA = 0.5f;
        private readonly float FREEZE_INPUT_TIME = 0.125f;


        private Vector2[] anchoredPositions;
        private float alpha;
        private float waitTime;
        private int index;

        private Color selectedColor;
        
        private void Awake()
        {
            anchoredPositions = new Vector2[menuParent.childCount];
            for (int i = 0; i < anchoredPositions.Length; ++i)
            {
                anchoredPositions[i] = menuParent.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            }
            menuParent = null; // 사용을 마침

            alpha = MIN_ALPHA;
            waitTime = 0f;
            selectedColor = new Color(0.2232704f, 0.5052339f, 1f, 1f);

            index = 0;
        }

        public bool OnUpdate()
        {
            // 수학 연산 최적화: Mathf.PingPong을 사용하여 if문 제거 및 부드러운 왕복 구현
            alpha = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, Mathf.PingPong(Time.time * ALPHA_DELTA, 1f));
            
            selectedColor.a = alpha;
            selectSlotImage.color = selectedColor;

            return true;
        }

        public bool Select(DataType.InputState inputState)
        {
            if (true == inputState.IsDown(DataType.IDxInput.ENTER | DataType.IDxInput.ACTION))
            {
                selectedColor.a = alpha;
                selectSlotImage.color = selectedColor;
                Debug.Log($"Select! input.curr:{inputState.Curr}, input.prev:{inputState.Prev}");
                return true;
            }
            
            waitTime += Time.deltaTime;
            if(FREEZE_INPUT_TIME >= waitTime)
            {
                return false;
            }


            if (true == inputState.IsDown(DataType.IDxInput.UP))
            {
                index = ((index - 1) + 4) % 4;
                selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
                waitTime = 0f;
                return true;
            }
            else if (true == inputState.IsDown(DataType.IDxInput.DOWN))
            {
                index = ((index + 1) + 4) % 4;
                selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
                waitTime = 0f;
                return true;
            }

            return false;
        }
    }
}