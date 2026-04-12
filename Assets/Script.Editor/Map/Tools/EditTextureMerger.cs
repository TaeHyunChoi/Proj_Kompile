#if UNITY_EDITOR
namespace Kompile.Map.Editor.Tools
{
    using Script.Map.Data;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public class EditTextureMerger : MonoBehaviour
    {
        private const string ROOT_INPUT_PATH = "Assets/Rcs/Map";

        private const int TARGET_TEXTURE_WIDTH = 2048;
        private const int TARGET_TEXTURE_HEIGHT = 2048;
        private const int SPRITE_WIDTH = 256;
        private const int SPRITE_HEIGHT = 256;
        private const int MAX_SPRITES_PER_ATLAS = 64;

        private static int _columns = TARGET_TEXTURE_WIDTH / SPRITE_WIDTH;

        [MenuItem("Tools/Asset/Map/Merge All Map Sprites", priority = 1)]
        public static void MergeAllSprites()
        {
            if (false == Directory.Exists(ROOT_INPUT_PATH))
            {
                return;
            }

            string[] directories = Directory.GetDirectories(ROOT_INPUT_PATH);
            if (0 == directories.Length)
            {
                return;
            }

            int totalMergedCount = 0;
            try
            {
                EditorUtility.DisplayProgressBar("Merging Textures", "맵 타일 텍스처를 병합 중입니다...", 0f);

                float lengthRecip = 1f / directories.Length;
                for (int i = 0; i < directories.Length; ++i)
                {
                    string dirPath = directories[i];
                    string folderName = new DirectoryInfo(dirPath).Name;

                    // [핵심 변경 1] 각 폴더 전용 로컬 테이블을 찾거나 생성합니다.
                    string localTablePath = $"{dirPath}/MapTextureTable-{folderName}.asset";
                    MapTextureTable localTable = AssetDatabase.LoadAssetAtPath<MapTextureTable>(localTablePath);

                    if (false == localTable)
                    {
                        localTable = ScriptableObject.CreateInstance<MapTextureTable>();
                        AssetDatabase.CreateAsset(localTable, localTablePath);
                        AssetDatabase.SaveAssets();
                    }

                    totalMergedCount += ProcessFolder(dirPath, folderName, localTable);

                    EditorUtility.SetDirty(localTable);
                    EditorUtility.DisplayProgressBar("Merging Textures",
                        $"Processing {folderName}...",
                        (float)(i + 1) * lengthRecip);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[Framework] 폴더별 독립 아틀라스 병합 완료! 총 {totalMergedCount}개의 아틀라스 페이지가 생성/갱신되었습니다.");
            }
        }

        private static int ProcessFolder(string folderPath, string folderName, MapTextureTable table)
        {
            string[] rawFiles = Directory.GetFiles(folderPath, "*.*");
            List<string> filteredFiles = new List<string>(rawFiles.Length);

            foreach (string file in rawFiles)
            {
                if (false == file.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileName = Path.GetFileName(file);
                if (fileName.StartsWith("merged-"))
                {
                    continue;
                }

                filteredFiles.Add(file);
            }

            if (0 == filteredFiles.Count)
            {
                return 0;
            }

            // 1. 그룹화와 정렬을 동시에 처리하기 위해 SortedDictionary 사용
            // Key: AtlasPage (Index / MAX), Value: 해당 페이지에 들어갈 파일 리스트
            SortedDictionary<int, List<KeyValuePair<int, string>>> atlasGroups =
                new SortedDictionary<int, List<KeyValuePair<int, string>>>();

            foreach (string file in filteredFiles)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(file);
                int index = table.GetOrAssignIndex(fileNameNoExt);
                int atlasPage = index / MAX_SPRITES_PER_ATLAS;

                if (false == atlasGroups.ContainsKey(atlasPage))
                {
                    atlasGroups.Add(atlasPage, new List<KeyValuePair<int, string>>());
                }

                // 중복 방지 로직 (기존 validFiles의 역할 포함)
                bool isDuplicate = false;
                foreach (var item in atlasGroups[atlasPage])
                {
                    if (item.Key == index)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (false == isDuplicate)
                {
                    atlasGroups[atlasPage].Add(new KeyValuePair<int, string>(index, file));
                }
            }

            // 2. 그룹별 아틀라스 생성
            // group = [page_index], [tile_index,name]
            foreach (var group in atlasGroups)
            {
                int atlasPage = group.Key;
                List<KeyValuePair<int, string>> pageItems = group.Value;

                string suffix = (atlasPage == 0) ? "" : $"-{atlasPage}";
                string outputPath = string.Format("{0}/merged-{1}{2}.png", folderPath, folderName, suffix);

                CreateAtlas(pageItems, outputPath);
            }

            return atlasGroups.Count;
        }

        private static void CreateAtlas(List<KeyValuePair<int, string>> files, string outputPath)
        {
            Texture2D texture2D =
                new Texture2D(TARGET_TEXTURE_WIDTH, TARGET_TEXTURE_HEIGHT, TextureFormat.RGBA32, false);
            Color[] clearPixels = new Color[TARGET_TEXTURE_WIDTH * TARGET_TEXTURE_HEIGHT];

            for (int i = 0; i < clearPixels.Length; ++i)
            {
                clearPixels[i] = new Color(0f, 0f, 0f, 0f);
            }

            texture2D.SetPixels(clearPixels);

            foreach (KeyValuePair<int, string> kvp in files)
            {
                int tileIndex = kvp.Key;
                string tileName = kvp.Value;

                Texture2D spriteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(kvp.Value);
                if (false == spriteTexture)
                {
                    continue;
                }

                int localIndex = tileIndex % MAX_SPRITES_PER_ATLAS;
                int xIndex = localIndex % _columns;
                int yIndex = localIndex / _columns;

                // 💡 주의: 원본 텍스처 파일들의 Import Settings에서 'Read/Write Enabled'가 반드시 체크되어 있어야 합니다!
                try
                {
                    Color[] pixels = spriteTexture.GetPixels();
                    texture2D.SetPixels(xIndex * SPRITE_WIDTH,
                        TARGET_TEXTURE_HEIGHT - ((yIndex + 1) * SPRITE_HEIGHT),
                        SPRITE_WIDTH,
                        SPRITE_HEIGHT,
                        pixels);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[Framework] 텍스처 픽셀을 읽을 수 없습니다. ({kvp.Value}) 인스펙터에서 'Read/Write Enabled'를 켜주세요. / 에러: {e.Message}");
                }
            }

            texture2D.Apply();

            byte[] pngData = texture2D.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(outputPath, pngData);
            }
        }
    }
}
#endif