namespace Script.Data
{
    using MessagePack;
    using MessagePack.Resolvers;

    /// <summary> MessagePack의 옵션 설정 </summary>
    public static class MessagePackConfig<T>
    {
        public static MessagePackSerializerOptions Options
        {
            get
            {
                // StandardResolver가 이미 ConcurrentDictionary를 포함한
                // 대부분의 C# 컬렉션을 지원하므로 이것만으로 충분합니다.
                return MessagePackSerializerOptions.Standard
                        .WithResolver(StandardResolver.Instance);
            }
        }
    }
}