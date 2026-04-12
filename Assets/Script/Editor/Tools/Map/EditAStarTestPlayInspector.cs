using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EditAStarTestPlayComponent))]
public class EditAStarTestPlayInspector : Editor
{
    private EditAStarTestPlayComponent tester;

    private void Awake()
    {
        tester = target as EditAStarTestPlayComponent;
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