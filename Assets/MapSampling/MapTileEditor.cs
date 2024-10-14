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

        base.OnInspectorGUI();

        //showSetData = EditorGUILayout.Foldout(showSetData, "Set Data");
        //if (showSetData)
        //{
        //    base.OnInspectorGUI();
        //}

        //EditorGUILayout.Space();

        //showDebugData = EditorGUILayout.Foldout(showDebugData, "Debug Data");
        //if (showDebugData)
        //{
        //    DrawGridLabel();
        //    DrawMoveLabel();
        //    DrawHeightLabel();
        //}
    }
    //private void DrawGridLabel()
    //{
    //    EditorGUILayout.LabelField("Grid Pivot", mapTileSample.debug_gridPivot.ToString());

    //    // grid 인덱스로부터 변환하여 처리해야 함
    //}
    //private void DrawMoveLabel()
    //{
    //    EditorGUILayout.LabelField("Move");

    //    int width = 13;
    //    int length = 16;

    //    long moveFlag = mapTileSample.debug_data.MoveFlag;

    //    GUILayout.BeginHorizontal();
        
    //    GUIStyle style = new GUIStyle(EditorStyles.label);
    //    style.fontSize = 12;
    //    style.alignment = TextAnchor.MiddleCenter;

    //    string ch;
    //    for (int i = 0; i < length; ++i)
    //    {
    //        bool isMovable = ((moveFlag >> i) & 1) != 0;
    //        switch (i % 4)
    //        {
    //            case 0: ch = isMovable ? "▲" : "_"; break;
    //            case 1: ch = isMovable ? "◀" : "_"; break;
    //            case 2: ch = isMovable ? "▼" : "_"; break;
    //            case 3: ch = isMovable ? "▶" : "_"; break;
    //            default: continue;
    //        }
    //        EditorGUILayout.LabelField(ch, GUILayout.MaxWidth(width));

    //        if (i != 15 && i % 4 == 3)
    //        {
    //            EditorGUILayout.LabelField("||", GUILayout.MaxWidth(width));
    //        }

    //    }
    //    GUILayout.EndHorizontal();

    //}
    //private void DrawHeightLabel()
    //{
    //    EditorGUILayout.LabelField("Height");

    //    int width = 100;

    //    long heightFlag = mapTileSample.debug_data.HeightFlag;
    //    float[] h = new float[13];
    //    for (int i = 0; i < 13; ++i)
    //    {
    //        h[i] = ((heightFlag >> (i * 3)) & 0b_0111) * (mapTileSample.IsHalf ? 0.125f : 0.25f);
    //    }

    //    GUILayout.BeginHorizontal();
    //    EditorGUILayout.LabelField(string.Format("{0:F2}, {1:F2}, {2:F2}", h[6], h[7], h[8]), GUILayout.MaxWidth(width));
    //    GUILayout.EndHorizontal();

    //    GUILayout.BeginHorizontal();
    //    EditorGUILayout.LabelField(string.Format("   {0:F2},  {1:F2}", h[11], h[12]), GUILayout.MaxWidth(width));
    //    GUILayout.EndHorizontal();

    //    GUILayout.BeginHorizontal();
    //    EditorGUILayout.LabelField(string.Format("{0:F2}, {1:F2}, {2:F2}", h[3], h[4], h[5]), GUILayout.MaxWidth(width));
    //    GUILayout.EndHorizontal();

    //    GUILayout.BeginHorizontal();
    //    EditorGUILayout.LabelField(string.Format("   {0:F2},  {1:F2}", h[9], h[10]), GUILayout.MaxWidth(width));
    //    GUILayout.EndHorizontal();

    //    GUILayout.BeginHorizontal();
    //    EditorGUILayout.LabelField(string.Format("{0:F2}, {1:F2}, {2:F2}", h[0], h[1], h[2]), GUILayout.MaxWidth(width));
    //    GUILayout.EndHorizontal();
    //}
}
