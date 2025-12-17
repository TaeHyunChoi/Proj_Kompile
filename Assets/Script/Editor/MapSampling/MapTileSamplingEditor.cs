#if UNITY_EDITOR
namespace  MapSampling
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(EditMapTileSampling))]
    public class MapTileSamplingEditor : Editor
    {
        private EditMapTileSampling _sampler;
        
        private void Awake()
        {
            _sampler = target as EditMapTileSampling;
        }
        
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 표시
            base.OnInspectorGUI();
        
            if (GUILayout.Button("Save"))
            {
                if (null != _sampler)
                {
                    _sampler.Save();
                    //_sampler.StartCoroutine(_sampler.Save());
                }
            }
            if (GUILayout.Button("Load"))
            {
                if (null != _sampler)
                {
                    _sampler.Load();
                }
            }
        }
    }  
}
#endif