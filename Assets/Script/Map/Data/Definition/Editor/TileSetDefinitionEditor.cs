#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using UnityEditor;
    using UnityEngine;
    // TileSetDefinition이 정의된 네임스페이스 (필요시 수정)
    using Script.Map.Data;

    /// <summary>
    /// [Framework] TileSetDefinition의 인스펙터에서 Top/Side 텍스처를 시각적으로 미리보기 할 수 있도록 지원합니다.
    /// </summary>
    [CustomEditor(typeof(TileSetDefinition))]
    [CanEditMultipleObjects] // 여러 개의 타일셋을 동시 선택해도 에러가 나지 않도록 지원
    public class TileSetDefinitionEditor : UnityEditor.Editor
    {
        private Texture2D _atlasTexture;

        private void OnEnable()
        {
            // 에디터가 열릴 때마다 텍스처를 다시 넣는 수고를 덜기 위해,
            // 이전에 등록했던 아틀라스 경로를 에디터 환경 설정(EditorPrefs)에서 기억하여 불러옵니다.
            string path = EditorPrefs.GetString("KompileMap_AtlasPreview", "");
            if (!string.IsNullOrEmpty(path))
            {
                _atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        public override void OnInspectorGUI()
        {
            // 1. 기존 데이터 동기화 및 그리기 (Enum 필드 등)
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space(15);

            // 2. 미리보기 UI 영역
            EditorGUILayout.LabelField("🎨 아틀라스 미리보기 (Preview)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // 아틀라스 텍스처를 등록받는 필드
            EditorGUI.BeginChangeCheck();
            _atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Atlas Texture", _atlasTexture, typeof(Texture2D), false);

            if (EditorGUI.EndChangeCheck() && _atlasTexture != null)
            {
                // 텍스처가 할당되거나 변경되면 그 경로를 기억해둡니다.
                EditorPrefs.SetString("KompileMap_AtlasPreview", AssetDatabase.GetAssetPath(_atlasTexture));
            }

            if (_atlasTexture != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // TileSetDefinition의 필드 속성을 이름으로 찾아옵니다.
                SerializedProperty topTexProp = serializedObject.FindProperty("topTexture");
                SerializedProperty sideTexProp = serializedObject.FindProperty("sideTexture");

                // 프로퍼티가 정상적으로 존재한다면 텍스처를 잘라서 그려줍니다.
                if (topTexProp != null) DrawSpritePreview("Top Texture", topTexProp.intValue);
                GUILayout.Space(20);
                if (sideTexProp != null) DrawSpritePreview("Side Texture", sideTexProp.intValue);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox("타일 미리보기를 보려면 인게임에서 사용하는 맵 아틀라스 텍스처를 할당해주세요.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            // 3. 변경 사항 저장
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 아틀라스 텍스처에서 특정 인덱스의 이미지를 잘라와 인스펙터에 그립니다.
        /// </summary>
        private void DrawSpritePreview(string label, int textureIndex)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            // 라벨 (중앙 정렬)
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);

            // 64x64 픽셀 크기의 그리기 영역 확보
            Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
            rect.x += (100 - 64) / 2f; // 영역 내 가운데 정렬

            // 배경에 어두운 박스를 깔아 투명 텍스처도 잘 보이게 처리
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

            // 나으리의 UV 계산 로직 (8x8 그리드 기준)
            int col = textureIndex % 8;
            int row = textureIndex / 8;

            float uMin = col / 8f;
            float vMin = 1.0f - ((row + 1) / 8f);
            float uvSize = 1f / 8f;

            Rect uvRect = new Rect(uMin, vMin, uvSize, uvSize);

            // 계산된 UV 좌표만큼만 텍스처에서 잘라서 그려줍니다.
            GUI.DrawTextureWithTexCoords(rect, _atlasTexture, uvRect);

            EditorGUILayout.EndVertical();
        }
    }
}
#endif