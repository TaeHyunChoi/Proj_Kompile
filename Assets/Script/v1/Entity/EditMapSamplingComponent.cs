namespace Kompile.Entity
{
    using UnityEngine;
    using System;
    
    [Serializable]
    public class EditMapSamplingComponent : MonoBehaviour
    {
        [SerializeField] private byte sceneIndex;

        public byte SceneIndex => sceneIndex;
    }
}