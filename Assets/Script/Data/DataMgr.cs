namespace Script.Data
{
    using System.IO;
    using System.Collections.Generic;
    using UnityEngine;
    using MessagePack;
    using MessagePack.Formatters;
    using MessagePack.Resolvers;
    using Script.Util;

    public static partial class DataMgr
    {
        public static void WriteBinaryMappingData<T>(T data, string fileName)
        {
            // 저장할 파일 경로 생성
            string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapNavRawData", fileName + ".dat");

            // 디렉토리가 없으면 생성
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 데이터를 MessagePack 형식으로 직렬화하고 파일에 저장
            byte[] serializedData = MessagePackSerializer.Serialize(data, MessagePackConfig<T>.Options);
            File.WriteAllBytes(filePath, serializedData);
        }
        public static T ReadBinaryMappingData<T>(string fileName)
        {
            // 파일 경로 생성
            string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapNavData", fileName + ".dat");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"파일이 존재하지 않습니다: {filePath}");
            }

            // 파일에서 바이트 배열 읽기 및 역직렬화
            byte[] serializedData = File.ReadAllBytes(filePath);
            T data = MessagePackSerializer.Deserialize<T>(serializedData);
            return data;
        }
    }

    public static partial class DataMgr
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