namespace GameManager
{
    using UnityEngine;
    using Script.Index;
    using Script.ContentTask;
    using System.Linq;
    using System.Collections.Generic;

    public class IngameManager : MonoBehaviour
    {
        private static IngameManager instance;

        private static List<ContentTask> tasks;
        private IDxInput.EInputFlag inputFlag;

        private void Awake()
        {
            // like singleton
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            tasks = new List<ContentTask>();
        }

        private void Start()
        {
            // 최초 task 호출
        }

        private void Update()
        {
            if (false == IDxInput.TryGetInput(out inputFlag))
            {
                return;
            }
        }

        // 지난번처럼 update, fixedUpdate 분류하여 task, fixedTask 분류하는게 좋을 수도?
        private void FixedUpdate()
        {
            for (int i = 0; i < tasks.Count(); ++i)
            {
                ContentTaskState state = tasks[i].Run(inputFlag);

                switch (state)
                {
                    case ContentTaskState.SUCCESS:
                        // 지난번에 썼던 것처럼 RoutineMgr 만들어서 빈칸 채워넣기 하는게 좋겠구나?
                        // 배열 정렬도 이참에 추가..? >> 배열 인덱스까지 직접 조정하면 어때?
                        break;
                    case ContentTaskState.FAILURE:
                        // log 등 오류 발생 시 처리 필요
                        UnityEngine.Assertions.Assert.IsTrue(state == ContentTaskState.FAILURE);
                        break;
                }
            }
        }
    }
}

