namespace Kompile.Utility
{
    using MessagePack;
    using MessagePack.Formatters;
    using Unity.Collections;
    
    /// <summary>
    /// MessagePack이 FixedString32Bytes를 직렬화/역직렬화할 수 있도록 돕는 포매터
    /// </summary>
    public class InUtilFixedString32BytesFormatter : IMessagePackFormatter<FixedString32Bytes>
    {
        public void Serialize(ref MessagePackWriter writer, FixedString32Bytes value,
            MessagePackSerializerOptions options)
        {
            // 바이너리로 저장할 때는 표준 string으로 기록
            writer.Write(value.ToString());
        }

        public FixedString32Bytes Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            // 읽어올 때는 할당 방지를 위해 FixedString32Bytes로 변환하여 반환
            return new FixedString32Bytes(reader.ReadString());
        }
    }
}