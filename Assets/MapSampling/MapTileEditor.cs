using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapTileSampler))]
public class MapTile : Editor
{
    MapTileSampler mapTileSample;

    private bool showSetData;
    private bool showDebugData;



    public override void OnInspectorGUI()
    {
        mapTileSample = (MapTileSampler)target;

        showSetData = EditorGUILayout.Foldout(showSetData, "Set Data");
        if (showSetData)
        {
            base.OnInspectorGUI();
        }

        EditorGUILayout.Space();

        showDebugData = EditorGUILayout.Foldout(showDebugData, "Debug Data");
        if (showDebugData)
        {
            DrawGridLabel();
            DrawMoveLabel();

        }
    }
    private void DrawGridLabel()
    {
        EditorGUILayout.LabelField("Grid Pivot", mapTileSample.debug_gridPivot.ToString());

        // grid 인덱스로부터 변환하여 처리해야 함
    }
    private void DrawMoveLabel()
    {
        EditorGUILayout.LabelField("Move");

        int width1 = 13;
        int length = 16;

        long collideFlag = mapTileSample.debug_data.ColliderFlag;
        int moveFlag = (int)(collideFlag & 0b_1111_1111_1111_1111);

        GUILayout.BeginHorizontal();
        
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        string ch;
        for (int i = 0; i < length; ++i)
        {
            bool isMovable = ((moveFlag >> i) & 1) != 0;
            switch (i % 4)
            {
                case 0: ch = isMovable ? "▲" : "_"; break;
                case 1: ch = isMovable ? "◀" : "_"; break;
                case 2: ch = isMovable ? "▼" : "_"; break;
                case 3: ch = isMovable ? "▶" : "_"; break;
                default: continue;
            }
            EditorGUILayout.LabelField(ch, GUILayout.MaxWidth(width1));

            if (i != 15 && i % 4 == 3)
            {
                EditorGUILayout.LabelField("||", GUILayout.MaxWidth(width1));
            }

        }
        GUILayout.EndHorizontal();

    }
}
