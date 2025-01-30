using UnityEngine;
using UnityEngine.UI;
using static Script.Index.IDxInput;

public class UI_TitleMenuObject : MonoBehaviour
{
    [SerializeField] private RectTransform[] rects;
    [SerializeField] private Image selectSlotImage;

    private readonly float minAlpha   = 0.3f;
    private readonly float maxAlpha   = 0.7f;
    private readonly float alphaDelta = 0.5f;

    private float alpha;
    private float sign;

    public bool TryGetInput(EInputFlag inputMask)
    {
        if (true == inputMask.Contains(EInputFlag.UP))
        {

            return true;
        }
        if (true == inputMask.Contains(EInputFlag.UP_HOLD))
        {
            // 시간 체크
            return true;
        }

        if (true == inputMask.Contains(EInputFlag.DOWN))
        {

            return true;
        }
        if (true == inputMask.Contains(EInputFlag.DOWN_HOLD))
        {
            // 시간 체크
            return true;
        }

        if (true == inputMask.Contains(EInputFlag.ACTION))
        {

            return true;
        }

        return false;
    }

    private void Awake()
    {
        alpha = minAlpha;
        sign = 1f;
    }
    private void Update()
    {
        alpha += sign * Time.deltaTime * alphaDelta;

        if (alpha >= maxAlpha)
        {
            alpha = maxAlpha;
            sign  = -1f;
        }
        else if (alpha <= minAlpha)
        {
            alpha = minAlpha;
            sign  = 1f;
        }
    }
    private void LateUpdate()
    {
        selectSlotImage.color = new Color(0.2232704f, 0.5052339f, 1f, alpha);
    }
}
