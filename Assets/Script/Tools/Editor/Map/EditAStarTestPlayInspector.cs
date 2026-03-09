#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Script.Map.Instance;

[CustomEditor(typeof(EditAStarTestPlayEntity))]
public class EditAStarTestPlayInspector : Editor
{
    private EditAStarTestPlayEntity tester;

    private void Awake()
    {
        tester = target as EditAStarTestPlayEntity;
    }

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 표시
        base.OnInspectorGUI();

        if (GUILayout.Button("Test Pathfinding Play"))
        {
            if (null != tester)
            {
                tester.Play();
            }
        }
    }
}
#endif