using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class DCustomWindow : EditorWindow
{
    [MenuItem("Custom/DataParser")]
    private static void Init()
    {
        EditorWindow wnd = GetWindow<DCustomWindow>();
        wnd.titleContent = new GUIContent("DataParser");
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("[.csv] → [.bin]"))
        {
            DDataTable.LoadCSVTable();         //CSV 파일 읽어서
            DDataTable.WriteBinaryFiles();     //Binary 파일로 저장
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
