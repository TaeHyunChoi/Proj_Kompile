namespace Kompile.Asset.Utility
{
    using Kompile.Asset.Utility;
    using MessagePack;
    using MessagePack.Formatters;
    using MessagePack.Resolvers;
    using UnityEngine;

    public static class MessagePackInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var customResolver = CompositeResolver.Create(
                new IMessagePackFormatter[] { new FixedString32BytesFormatter() },
                new IFormatterResolver[] { StandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(customResolver);

            MessagePackSerializer.DefaultOptions = options;

            Debug.Log("[Debug][MessagePackInitializer] FixedString32Bytes 포매터 등록 완료.");
        }
    }
}