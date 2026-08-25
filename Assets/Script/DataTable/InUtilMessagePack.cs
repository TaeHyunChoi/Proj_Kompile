namespace Kompile.Utility
{
    using MessagePack;
    using MessagePack.Formatters;
    using MessagePack.Resolvers;
    using UnityEngine;

    public static class InUtilMessagePack
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeMessagePack()
        {
            // 커스텀 포매터 및 기본 Resolver 등록
            var customResolver = CompositeResolver.Create(
                new IMessagePackFormatter[] { new InUtilFixedString32BytesFormatter() },
                new IFormatterResolver[]
                {
                    StandardResolver.Instance,
                    ContractlessStandardResolver.Instance // 필요 시 Contractless 지원 추가
                }
            );

            // 전역 DefaultOptions 지정
            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(customResolver);

            Debug.Log("[SerializeUtil] MessagePack Resolver 초기화 완료.");
        }

        public static byte[] Serialize<T>(T value)
        {
            return MessagePackSerializer.Serialize(value);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }

            // DefaultOptions가 적용된 전역 설정을 사용
            return MessagePackSerializer.Deserialize<T>(bytes);
        }
    }
}