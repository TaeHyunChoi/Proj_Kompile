// namespace Script.GamePlay
// {
//     using Script.Asset;
//     using static Script.Input.Data.Definition;
//     using UnityEngine;
//     using UnityEngine.UI;
//
//     public class UITitleMenuObject : IngameMonoBehaviourBase, IGameUpdater
//     {
//         [SerializeField] private Transform menuParent;
//         [SerializeField] private Image selectSlotImage;
//
//         private readonly float MIN_ALPHA = 0.3f;
//         private readonly float MAX_ALPHA = 0.7f;
//         private readonly float ALPHA_DELTA = 0.5f;
//         private readonly float FREEZE_INPUT_TIME = 0.125f;
//
//
//         private Vector2[] anchoredPositions;
//         private float alpha;
//         private float waitTime;
//         private int index;
//
//         private Color selectedColor;
//
//         public override PrefabID PrefabID => PrefabID.UI_TitleMenuObject;
//
//         private void Awake()
//         {
//             anchoredPositions = new Vector2[menuParent.childCount];
//             for (int i = 0; i < anchoredPositions.Length; ++i)
//             {
//                 anchoredPositions[i] = menuParent.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
//             }
//             menuParent = null; // 사용을 마침
//
//             alpha = MIN_ALPHA;
//             waitTime = 0f;
//             selectedColor = new Color(0.2232704f, 0.5052339f, 1f, 1f);
//
//             index = 0;
//         }
//
//         public bool OnUpdate()
//         {
//             // 수학 연산 최적화: Mathf.PingPong을 사용하여 if문 제거 및 부드러운 왕복 구현
//             alpha = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, Mathf.PingPong(Time.time * ALPHA_DELTA, 1f));
//             
//             selectedColor.a = alpha;
//             selectSlotImage.color = selectedColor;
//
//             return true;
//         }
//
//         /// <summary>
//         /// 타이틀 메뉴에서 항목을 선택한다. <br/> OpeningTitleManager에게 선택한 인덱스를 반환하겠다.
//         /// </summary>
//         /// <param name="inputState"></param>
//         /// <returns>선택한 메뉴 인덱스, 메뉴 선택이 불가했다면 -1을 반환</returns>
//         public int Select(InputState inputState)
//         {
//             if (true == inputState.IsDown(IDxInput.ENTER | IDxInput.ACTION))
//             {
//                 selectedColor.a = alpha;
//                 selectSlotImage.color = selectedColor;
//                 return index;
//             }
//             
//             waitTime += Time.deltaTime;
//             if(FREEZE_INPUT_TIME >= waitTime)
//             {
//                 return -1;
//             }
//
//
//             if (true == inputState.IsDown(IDxInput.UP))
//             {
//                 index = ((index - 1) + 4) % 4;
//                 selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
//                 waitTime = 0f;
//             }
//             else if (true == inputState.IsDown(IDxInput.DOWN))
//             {
//                 index = ((index + 1) + 4) % 4;
//                 selectSlotImage.rectTransform.anchoredPosition = anchoredPositions[index];
//                 waitTime = 0f;
//             }
//
//             return -1;
//         }
//     }
// }