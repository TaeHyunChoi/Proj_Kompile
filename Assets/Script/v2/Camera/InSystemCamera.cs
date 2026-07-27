namespace Kompile
{
    using Kompile.Manager;
    using UnityEngine;

    public class InSystemCamera : MonoBehaviour
    {
        private static InSystemCamera _instance;
        private Camera _main;

        public static InSystemCamera Instance => _instance;
        public static Camera Main => _instance._main;

        public void OnAwake()
        {
            if (_instance)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _main = transform.Find("MainCam").GetComponent<Camera>();
            // 이후로는 그냥 오브젝트 생성해서 추가하자...
            // Main.scene을 바꾸는 것보단 나은 듯;
            // 아니면 프리팹으로 처리;
        }
        public void OnLateUpdate()
        {
            var actorMgr = InGame.Get<ActorMgr>();

        }
    }
}
