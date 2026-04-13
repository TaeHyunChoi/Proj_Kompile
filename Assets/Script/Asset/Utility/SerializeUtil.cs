namespace Kompile.Asset.Utility
{
    using MessagePack;
    using MessagePack.Resolvers;

    public static class SerializeUtil
    {
        private static readonly MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

        public static T Deserialize<T>(byte[] bytes)
        {
            if (null == bytes || 0 == bytes.Length)
            {
                return default;
            }

            return MessagePackSerializer.Deserialize<T>(bytes, options);
        }
    }
}