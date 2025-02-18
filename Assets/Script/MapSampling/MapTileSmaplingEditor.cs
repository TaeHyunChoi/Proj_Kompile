#if UNITY_EDITOR
namespace  MapSampling
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(MapTileSampling))]
    public class MyScriptEditor : Editor
    {
        private MapTileSampling _sampler;
        
        private void Awake()
        {
            _sampler = target as MapTileSampling;
        }
        
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 표시
            base.OnInspectorGUI();
        
            if (GUILayout.Button("Save"))
            {
                _sampler?.Save();
            }
            //if (GUILayout.Button("Load"))
            //{
            //    _sampler?.Load();
            //}
        }
    }  
}
#endif