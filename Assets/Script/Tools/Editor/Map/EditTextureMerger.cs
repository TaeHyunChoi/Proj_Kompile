#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Script.Index; // MapTextureType Enum 사용

public class EditTextureMerger : MonoBehaviour
{
    // [설정] 맵 텍스처들이 들어있는 최상위 루트 폴더
    private const string ROOT_INPUT_PATH = "Assets/Rcs/Map";

    private const int TARGET_TEXTURE_WIDTH = 2048;
    private const int TARGET_TEXTURE_HEIGHT = 2048;
    private const int SPRITE_WIDTH = 256;
    private const int SPRITE_HEIGHT = 256;
    private const int MAX_SPRITES_PER_ATLAS = (2048 / 256) * (2048 / 256); // 8 * 8 = 64개

    [MenuItem("Tools/Asset/Map/Merge All Map Sprites", priority = 1)]
    public static void MergeAllSprites()
    {
        if (!Directory.Exists(ROOT_INPUT_PATH))
        {
            Debug.LogError($"[Framework] 지정된 루트 폴더가 없습니다: {ROOT_INPUT_PATH}");
            return;
        }

        // 1. Assets/Rcs/Map 하위의 모든 폴더(town, field_00 등)를 가져옵니다.
        string[] directories = Directory.GetDirectories(ROOT_INPUT_PATH);

        if (directories.Length == 0)
        {
            Debug.LogWarning($"[Framework] {ROOT_INPUT_PATH} 아래에 처리할 폴더가 없습니다.");
            return;
        }

        int totalMergedCount = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Merging Textures", "맵 타일 텍스처를 병합 중입니다...", 0f);

            for (int i = 0; i < directories.Length; i++)
            {
                string dirPath = directories[i];
                string folderName = new DirectoryInfo(dirPath).Name;

                // 해당 폴더 병합 처리
                int mergedCount = ProcessFolder(dirPath, folderName);
                totalMergedCount += mergedCount;

                EditorUtility.DisplayProgressBar("Merging Textures", $"Processing {folderName}...", (float)(i + 1) / directories.Length);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"[Framework] 텍스처 병합 완료! 총 {totalMergedCount}개의 아틀라스가 생성/갱신되었습니다.");
        }
    }

    /// <summary>
    /// 특정 폴더 내의 스프라이트들을 읽어들여 64개 단위로 아틀라스를 생성합니다.
    /// </summary>
    private static int ProcessFolder(string folderPath, string folderName)
    {
        // 폴더 내의 모든 png 파일을 찾습니다. (단, 이전에 만들어진 merged- 파일은 제외)
        var allFiles = Directory.GetFiles(folderPath, "*.png")
                                .Where(f => !Path.GetFileName(f).StartsWith("merged-"))
                                .ToList();

        if (allFiles.Count == 0) return 0;

        // MapTextureType Enum에 맞춰 인덱스 순서대로 정렬하기 위한 딕셔너리
        Dictionary<int, string> validFiles = new Dictionary<int, string>();

        foreach (string file in allFiles)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(file);

            if (Enum.TryParse(typeof(MapTextureType), fileNameNoExt, out object enumObj))
            {
                int index = (int)enumObj;
                if (!validFiles.ContainsKey(index))
                {
                    validFiles.Add(index, file);
                }
                else
                {
                    Debug.LogWarning($"[Framework] 중복된 Enum 인덱스 발견: {fileNameNoExt} in {folderPath}");
                }
            }
            else
            {
                Debug.LogWarning($"[Framework] 파일명이 MapTextureType Enum에 존재하지 않아 병합에서 제외됩니다: {fileNameNoExt}");
            }
        }

        // 인덱스 오름차순으로 정렬된 파일 목록
        var sortedFiles = validFiles.OrderBy(kvp => kvp.Key).ToList();

        int atlasCount = Mathf.CeilToInt((float)sortedFiles.Count / MAX_SPRITES_PER_ATLAS);

        for (int atlasIndex = 0; atlasIndex < atlasCount; atlasIndex++)
        {
            // 아틀라스에 들어갈 64개의 스프라이트 추출
            var batch = sortedFiles.Skip(atlasIndex * MAX_SPRITES_PER_ATLAS).Take(MAX_SPRITES_PER_ATLAS).ToList();

            // 출력 파일명 결정 (첫 번째는 merged-folder.png, 두 번째부터는 merged-folder-1.png ...)
            string suffix = (atlasIndex == 0) ? "" : $"-{atlasIndex}";
            string outputPath = $"{folderPath}/merged-{folderName}{suffix}.png";

            CreateAtlas(batch, outputPath);
        }

        return atlasCount;
    }

    /// <summary>
    /// 주어진 파일 목록(최대 64개)을 하나의 2048x2048 텍스처로 합쳐서 저장합니다.
    /// </summary>
    private static void CreateAtlas(List<KeyValuePair<int, string>> files, string outputPath)
    {
        Texture2D texture2D = new Texture2D(TARGET_TEXTURE_WIDTH, TARGET_TEXTURE_HEIGHT, TextureFormat.RGBA32, false);

        // 투명으로 초기화
        Color[] clearPixels = new Color[TARGET_TEXTURE_WIDTH * TARGET_TEXTURE_HEIGHT];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = new Color(0, 0, 0, 0);
        texture2D.SetPixels(clearPixels);

        int columns = TARGET_TEXTURE_WIDTH / SPRITE_WIDTH; // 8

        foreach (var kvp in files)
        {
            int globalEnumIndex = kvp.Key;
            string filePath = kvp.Value;

            Texture2D spriteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            if (spriteTexture == null)
            {
                Debug.LogError($"[Framework] 텍스처 로드 실패: {filePath}");
                continue;
            }

            // 아틀라스 내부에서의 상대적 인덱스 (0 ~ 63)
            // 전체 Enum 값이 100이더라도, 이 아틀라스 배열의 첫 번째 자리에 꽂으려면 순차적 처리가 필요함.
            // (주의: 나으리의 기존 코드는 Enum 값을 그대로 XY 인덱스로 썼기 때문에, Enum값이 64를 넘어가면 에러가 났습니다.)
            // [해결] 이 아틀라스 묶음 내에서의 "순번(0~63)"을 기준으로 XY를 계산합니다.
            int localIndex = files.IndexOf(kvp);

            int xIndex = localIndex % columns;
            int yIndex = localIndex / columns;

            // 스프라이트 복사 (하단에서부터 채워나감)
            Color[] pixels = spriteTexture.GetPixels();
            texture2D.SetPixels(xIndex * SPRITE_WIDTH, TARGET_TEXTURE_HEIGHT - ((yIndex + 1) * SPRITE_HEIGHT), SPRITE_WIDTH, SPRITE_HEIGHT, pixels);
        }

        texture2D.Apply();
        byte[] pngData = texture2D.EncodeToPNG();

        if (pngData != null)
        {
            File.WriteAllBytes(outputPath, pngData);
            Debug.Log($"[Framework] 아틀라스 생성 완료: {outputPath}");
        }
    }
}
#endif