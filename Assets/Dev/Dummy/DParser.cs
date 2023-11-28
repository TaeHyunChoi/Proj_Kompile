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
        if (GUILayout.Button("CSV to BIN"))
        {
            string csvFolderPath = Application.dataPath + "/Dev/Dummy/DCSV/";
            string[] csvPaths = Directory.GetFiles(csvFolderPath, "*.csv");

            string filePath = csvFolderPath + "test.csv";
            //foreach (string filePath in csvPaths)
            {
                FileStream fStream = new FileStream(filePath, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fStream);

                //형식에 맞춰서 저장하는거구나.
                //이걸 어떻게 해야 하는거지? 알아서 읽고 다음으로 넘기는건가?
                //아주 모르는 영역이라 엄청 헤매는 중; 넘긴 넘어야 함 ㅅㄱ;

                bw.Close();
                fStream.Close();

                //Debug.Log($"{filePath.Replace(csvFolderPath, "")}");

                //StreamReader reader = new StreamReader(filePath);
                //while (!reader.EndOfStream)
                //{
                //    string line = reader.ReadLine();
                //    Debug.Log(line);
                //}
                //reader.Close();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}
