#if UNITY_EDITOR
using Script.Map.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [MenuItem("Tools/Asset/Map/Merge All Map Sprites", priority = 1)]
    public static void MergeAllSprites()
    {
        if (!Directory.Exists(ROOT_INPUT_PATH)) return;

        string[] directories = Directory.GetDirectories(ROOT_INPUT_PATH);
        if (directories.Length == 0) return;

        int totalMergedCount = 0;
        try
        {
            EditorUtility.DisplayProgressBar("Merging Textures", "맵 타일 텍스처를 병합 중입니다...", 0f);

            for (int i = 0; i < directories.Length; i++)
            {
                string dirPath = directories[i];
                string folderName = new DirectoryInfo(dirPath).Name;

                // [핵심 변경 1] 각 폴더 전용 로컬 테이블을 찾거나 생성합니다.
                string localTablePath = $"{dirPath}/MapTextureTable.asset";
                MapTextureTable localTable = AssetDatabase.LoadAssetAtPath<MapTextureTable>(localTablePath);

                if (localTable == null)
                {
                    localTable = ScriptableObject.CreateInstance<MapTextureTable>();
                    AssetDatabase.CreateAsset(localTable, localTablePath);
                    AssetDatabase.SaveAssets(); // 생성 직후 디스크 고정
                }

                totalMergedCount += ProcessFolder(dirPath, folderName, localTable);

                // 해당 폴더의 테이블 작업이 끝났으므로 저장
                EditorUtility.SetDirty(localTable);
                EditorUtility.DisplayProgressBar("Merging Textures", $"Processing {folderName}...", (float)(i + 1) / directories.Length);
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
        // [안전장치] 확장자가 .PNG (대문자)인 경우도 놓치지 않도록 검색 강화
        var allFiles = Directory.GetFiles(folderPath, "*.*")
                                .Where(f => f.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(f).StartsWith("merged-"))
                                .ToList();

        if (allFiles.Count == 0) return 0;

        Dictionary<int, string> validFiles = new Dictionary<int, string>();

        foreach (string file in allFiles)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(file);
            // 해당 폴더 전용 테이블에서 인덱스를 발급받습니다. (항상 0번부터 시작)
            int index = table.GetOrAssignIndex(fileNameNoExt);

            if (!validFiles.ContainsKey(index)) validFiles.Add(index, file);
        }

        var groupedFiles = validFiles.GroupBy(kvp => kvp.Key / MAX_SPRITES_PER_ATLAS)
                                     .OrderBy(g => g.Key).ToList();

        foreach (var group in groupedFiles)
        {
            int atlasPage = group.Key;
            string suffix = (atlasPage == 0) ? "" : $"-{atlasPage}";
            string outputPath = $"{folderPath}/merged-{folderName}{suffix}.png";
            CreateAtlas(group.ToList(), outputPath);
        }

        return groupedFiles.Count;
    }

    private static void CreateAtlas(List<KeyValuePair<int, string>> files, string outputPath)
    {
        Texture2D texture2D = new Texture2D(TARGET_TEXTURE_WIDTH, TARGET_TEXTURE_HEIGHT, TextureFormat.RGBA32, false);
        Color[] clearPixels = new Color[TARGET_TEXTURE_WIDTH * TARGET_TEXTURE_HEIGHT];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = new Color(0, 0, 0, 0);
        texture2D.SetPixels(clearPixels);

        int columns = TARGET_TEXTURE_WIDTH / SPRITE_WIDTH;

        foreach (var kvp in files)
        {
            Texture2D spriteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(kvp.Value);
            if (spriteTexture == null) continue;

            int localIndex = kvp.Key % MAX_SPRITES_PER_ATLAS;
            int xIndex = localIndex % columns;
            int yIndex = localIndex / columns;

            // 💡 주의: 원본 텍스처 파일들의 Import Settings에서 'Read/Write Enabled'가 반드시 체크되어 있어야 합니다!
            try
            {
                Color[] pixels = spriteTexture.GetPixels();
                texture2D.SetPixels(xIndex * SPRITE_WIDTH, TARGET_TEXTURE_HEIGHT - ((yIndex + 1) * SPRITE_HEIGHT), SPRITE_WIDTH, SPRITE_HEIGHT, pixels);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Framework] 텍스처 픽셀을 읽을 수 없습니다. ({kvp.Value}) 인스펙터에서 'Read/Write Enabled'를 켜주세요. / 에러: {e.Message}");
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
#endif