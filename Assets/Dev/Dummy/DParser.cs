using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class DParser : EditorWindow
{
    [MenuItem("Custom/DataParser")]
    private static void Init()
    {
        EditorWindow wnd = GetWindow<DParser>();
        wnd.titleContent = new GUIContent("DataParser");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("[.csv] → [.bin]"))
        {
            DataMgr.LoadCSVTable();         //CSV 파일 읽어서
            DataMgr.WriteBinaryFiles();     //Binary 파일로 저장
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("[Test] Read .bin FIle"))
        {
            DataMgr.ReadBinary<SkillData>("SkillData.bin");
        }
        EditorGUILayout.EndHorizontal();
    }
}
