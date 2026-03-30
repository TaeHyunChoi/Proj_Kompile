#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Script.Map.Data;

    [CustomEditor(typeof(TileSetDefinition))]
    [CanEditMultipleObjects]
    public class TileSetDefinitionEditor : UnityEditor.Editor
    {
        private Texture2D _atlasTexture;

        private void OnEnable()
        {
            string path = EditorPrefs.GetString("KompileMap_AtlasPreview", "");
            if (!string.IsNullOrEmpty(path))
            {
                _atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🎨 아틀라스 미리보기 (Preview)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();
            _atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Atlas Texture", _atlasTexture, typeof(Texture2D), false);

            if (EditorGUI.EndChangeCheck() && true == _atlasTexture)
            {
                EditorPrefs.SetString("KompileMap_AtlasPreview", AssetDatabase.GetAssetPath(_atlasTexture));
            }

            if (true == _atlasTexture)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                SerializedProperty topTexProp = serializedObject.FindProperty("topTexture");
                SerializedProperty sideTexProp = serializedObject.FindProperty("sideTexture");

                if (topTexProp != null) DrawSpritePreview("Top Texture", topTexProp.intValue);
                GUILayout.Space(20);
                if (sideTexProp != null) DrawSpritePreview("Side Texture", sideTexProp.intValue);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox("타일 미리보기를 보려면 아틀라스 텍스처를 할당해주세요.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSpritePreview(string label, int globalTextureIndex)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);

            Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
            rect.x += (100 - 64) / 2f;
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

            int localIndex = globalTextureIndex % 64;
            int col = localIndex % 8;
            int row = localIndex / 8;

            float uMin = col / 8f;
            float vMin = 1.0f - ((row + 1) / 8f);
            float uvSize = 1f / 8f;

            Rect uvRect = new Rect(uMin, vMin, uvSize, uvSize);
            GUI.DrawTextureWithTexCoords(rect, _atlasTexture, uvRect);
            EditorGUILayout.EndVertical();
        }
    }
}
#endif