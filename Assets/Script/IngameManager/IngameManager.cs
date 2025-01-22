namespace GameManager
{
    using UnityEngine;
    using Script.Index;

    public class IngameManager : MonoBehaviour
    {
        private IngameManager instance;

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
        }
        private void Update()
        {
            if (false == IDxInput.TryGetInput(out IDxInput.EInputFlag inputFlag))
            {
                return;
            }

            // 루틴 업데이트 리스트를 가지고 있는게 좋겠는데?

        }
    }
}

