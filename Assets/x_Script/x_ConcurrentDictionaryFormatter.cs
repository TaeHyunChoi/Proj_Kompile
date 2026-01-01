namespace Script.Data
{
    using System.Collections.Generic;
    using MessagePack.Formatters;
    using MessagePack;

    /// <summary> MessagePack의 (Cumstom) Attribute 설정에 쓰인다. 없으면 컴파일 에러 </summary>
    public class x_ConcurrentDictionaryFormatter<TKey, TValue> : IMessagePackFormatter<x_ConcurrentDictionary<TKey, TValue>>
    {
        public void Serialize(ref MessagePackWriter writer, x_ConcurrentDictionary<TKey, TValue> value, MessagePackSerializerOptions options)
        {
            options.Resolver.GetFormatterWithVerify<Dictionary<TKey, TValue>>().Serialize(
                ref writer, new Dictionary<TKey, TValue>(value), options);
        }

        public x_ConcurrentDictionary<TKey, TValue> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var dictionary = options.Resolver.GetFormatterWithVerify<Dictionary<TKey, TValue>>()
                                    .Deserialize(ref reader, options);

            return new x_ConcurrentDictionary<TKey, TValue>(dictionary);
        }
    }
}