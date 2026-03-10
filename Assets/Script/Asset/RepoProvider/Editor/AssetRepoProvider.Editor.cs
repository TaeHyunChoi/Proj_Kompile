#if UNITY_EDITOR
namespace Script.Asset.Provider
{
    using MessagePack;
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;

    public static partial class AssetRepoProvider 
    {
        /// <summary>
        /// 데이터를 바이너리로 직렬화하여 저장하고 어드레서블에 등록합니다.
        /// </summary>
        public static void WriteBinaryFile<T>(T data, string relativePath, string fileName, string addressableGroup = null, string addressableLabel = null)
        {
            string fullDir = Path.Combine(Application.dataPath, relativePath);
            string filePath = Path.Combine(fullDir, fileName + ".bytes");
            string assetPath = Path.Combine("Assets", relativePath, fileName + ".bytes");

            if (!Directory.Exists(fullDir)) Directory.CreateDirectory(fullDir);

            var options = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            try
            {
                byte[] bytes = MessagePackSerializer.Serialize(data, options);
                File.WriteAllBytes(filePath, bytes);
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

                var group = settings.FindGroup(addressableGroup) ?? settings.CreateGroup(addressableGroup, false, false, false, null);
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