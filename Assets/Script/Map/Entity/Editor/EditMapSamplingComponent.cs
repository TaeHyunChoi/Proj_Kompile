#if UNITY_EDITOR
namespace Script.Map.Entity
{
    using UnityEngine;

    public class EditMapSamplingComponent : MonoBehaviour
    {
        [SerializeField] private byte sceneIndex;

        public byte SceneIndex => sceneIndex;
    }
}
#endif