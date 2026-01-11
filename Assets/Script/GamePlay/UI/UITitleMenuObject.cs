namespace Script.GamePlay
{
    using Script.Data;
    using UnityEngine;
    using UnityEngine.UI;

    public class UITitleMenuObject : MonoBehaviour, IGameUpdater
    {
        [SerializeField] private Transform menuParent;
        [SerializeField] private Image selectSlotImage;

        private readonly float minAlpha = 0.3f;
        private readonly float maxAlpha = 0.7f;
        private readonly float alphaDelta = 0.5f;
        private readonly float waitTime = 0.125f;

        private Vector2[] anchoredPositions;
        private float alpha;
        private float sign;

        private float lastInputTime;
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

            alpha = minAlpha;
            sign = 1f;
            selectedColor = new Color(0.2232704f, 0.5052339f, 1f, 1f);

            index = 0;
            lastInputTime = 0;
        }

        public bool OnUpdate()
        {
            // 수학 연산 최적화: Mathf.PingPong을 사용하여 if문 제거 및 부드러운 왕복 구현
            alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PingPong(Time.time * alphaDelta, 1f));
            
            selectedColor.a = alpha;
            selectSlotImage.color = selectedColor;

            return true;
        }

        public bool Select(DataType.InputState inputState)
        {
            if(true == inputState.IsDown(DataType.IDxInput.SELECT_ALL))
            {
                selectedColor.a = alpha;
                selectSlotImage.color = selectedColor;
                Debug.Log($"Select! input.curr:{inputState.Curr}, input.prev:{inputState.Prev}");
                // MessageManager.Publish(new OnSelect_UITitleMenu(index)); //이벤트 방식으로 처리했음
                return true;
            }

            if (Time.time < lastInputTime + waitTime)
            {
                return false;
            }
            lastInputTime = Time.time;

            if(true == inputState.IsDown(DataType.IDxInput.UP))
            {
                index = ((index - 1) + 4) % 4;
                selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
                return true;
            }
            else if(true == inputState.IsDown(DataType.IDxInput.DOWN))
            {
                index = ((index + 1) + 4) % 4;
                selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
                return true;
            }

            return false;
        }
    }
}