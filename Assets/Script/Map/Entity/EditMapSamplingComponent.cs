namespace Kompile.Map.Entity
{
    using UnityEngine;

    public class EditMapSamplingComponent : MonoBehaviour
    {
        [SerializeField] private byte sceneIndex;

        public byte SceneIndex => sceneIndex;
    }
}