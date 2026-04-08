#if UNITY_EDITOR
namespace Script.Asset.Provider
{
    using MessagePack;
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;

    public static partial class AssetProvider
    {
        /// <summary>
        /// 데이터를 바이너리로 직렬화하여 저장하고 어드레서블에 등록합니다.
        /// </summary>
        public static void WriteBinaryFile<T>(T data, string relativePath, string fileName,
            string addressableGroup = null, string addressableLabel = null)
        {
            relativePath = relativePath.Replace('\\', '/');

            // [FIX] Path.Combine을 완전히 배제하고 슬래시(/)를 사용한 문자열 보간으로 변경
            string fullDir = $"{Application.dataPath}/{relativePath}";
            string filePath = $"{fullDir}/{fileName}.bytes";

            // 유니티 내부 API용 경로는 반드시 슬래시만 존재해야 함
            string assetPath = $"Assets/{relativePath}/{fileName}.bytes";

            if (!System.IO.Directory.Exists(fullDir))
            {
                System.IO.Directory.CreateDirectory(fullDir);
            }

            var options =
                MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver
                    .Instance);

            try
            {
                byte[] bytes = MessagePackSerializer.Serialize(data, options);
                System.IO.File.WriteAllBytes(filePath, bytes);

                // 이제 완벽하게 슬래시만 있는 경로가 들어가므로 에러가 발생하지 않음
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AssetProvider] Serialization Failed: {e.Message}");
                return;
            }

            if (!string.IsNullOrEmpty(addressableGroup))
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null) return;

                var group = settings.FindGroup(addressableGroup) ??
                            settings.CreateGroup(addressableGroup, false, false, false, null);

                // 여기서도 정상적인 경로로 GUID를 추출
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                var entry = settings.CreateOrMoveEntry(guid, group);

                if (entry != null)
                {
                    if (!string.IsNullOrEmpty(addressableLabel))
                    {
                        settings.AddLabel(addressableLabel);
                        entry.SetLabel(addressableLabel, true);
                    }

                    entry.SetAddress(fileName);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                }
            }

            Debug.Log($"[AssetProvider] Data saved: {fileName}");
        }
    }
}
#endif