namespace Script.Data
{
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;

    using MessagePack;
    using MessagePack.Formatters;
    using MessagePack.Resolvers;

    using Script.Util;
    using Script.Manager;

    public static partial class DataManager
    {
        private const string MAP_NAVI_DATA_PATH = "Rcs\\Bin\\MapNavRawData";

        public static void WriteBinaryMappingData<T>(T data, string fileName)
        {
            // 저장할 파일 경로 생성
            string filePath = Path.Combine(Application.dataPath, MAP_NAVI_DATA_PATH, fileName + ".dat");

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 데이터를 MessagePack 형식으로 직렬화하고 파일에 저장
            byte[] serializedData = MessagePackSerializer.Serialize(data, MessagePackConfig<T>.Options);
            File.WriteAllBytes(filePath, serializedData);

#if UNITY_EDITOR
            // 어드레서블 에셋으로 저장
            string assetPath = "Assets/" + MAP_NAVI_DATA_PATH + "/" + fileName + ".dat";
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup("MapNavi");
            if (group == null)
            {
                group = settings.CreateGroup("MapNavi", false, false, false, null);
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
            entry.SetLabel(fileName, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
#endif
        }
        public static async Task<T> ReadBinaryMappingDataAsync<T>(int targetGridKey)
        {
            string label = $"MapNavi_{targetGridKey}";

            // 어드레서블 에셋 로드
            AsyncOperationHandle<IList<TextAsset>> handler = Addressables.LoadAssetsAsync<TextAsset>(label, null);
            await handler.Task;

            if (handler.Status != AsyncOperationStatus.Succeeded || handler.Result.Count == 0)
            {
                throw new FileNotFoundException($"라벨에 해당하는 파일이 존재하지 않습니다: {label}");
            }

            // 파일에서 바이트 배열 읽기 및 역직렬화
            int instanceID = handler.Result[0].GetInstanceID();
            byte[] serializedData = handler.Result[0].bytes;
            T data = MessagePackSerializer.Deserialize<T>(serializedData);

            // 에셋 매니저에서 들고 있고..
            AssetManager.AddHandler(instanceID, handler);

            // 자료 구했다는 값을 전달하고
            MessageManager.Publish(new Message_t(Manager.MessageType.GET_ASSET, Index.AssetIndex.DB_MAP_NAVI, instanceID));

            //데이터의 instanceID를 넘겨야 탐색이 가능한가?


            return data;
        }
    }

    public static partial class DataManager
    {
        public class ConcurrentDictionaryFormatter<TKey, TValue> : IMessagePackFormatter<ConcurrentDictionary<TKey, TValue>>
        {
            public void Serialize(ref MessagePackWriter writer, ConcurrentDictionary<TKey, TValue> value, MessagePackSerializerOptions options)
            {
                options.Resolver.GetFormatterWithVerify<Dictionary<TKey, TValue>>().Serialize(
                    ref writer, new Dictionary<TKey, TValue>(value), options);
            }

            public ConcurrentDictionary<TKey, TValue> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var dictionary = options.Resolver.GetFormatterWithVerify<Dictionary<TKey, TValue>>()
                                        .Deserialize(ref reader, options);

                return new ConcurrentDictionary<TKey, TValue>(dictionary);
            }
        }

    }

    public static class MessagePackConfig<T>
    {

        public static readonly MessagePackSerializerOptions Options;

        static MessagePackConfig()
        {
            Options = MessagePackSerializerOptions.Standard
                        .WithResolver(CompositeResolver
                        .Create
                        (
                            new IMessagePackFormatter[]
                            {
                            new ConcurrentDictionaryFormatter<int, T>() // 커스텀 포맷터 등록
                            },
                            new IFormatterResolver[]
                            {
                            StandardResolver.Instance // 기본 Resolver
                            }
                        ));
        }
    }

}