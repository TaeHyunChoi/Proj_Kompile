#if UNITY_EDITOR
using MessagePack;
using MessagePack.Formatters;
using UnityEngine;

public sealed class EditUnityVector3Formatter : IMessagePackFormatter<Vector3>
{
    public static EditUnityVector3Formatter Instance { get; set; } = new EditUnityVector3Formatter();

    public EditUnityVector3Formatter() { }

    public void Serialize(ref MessagePackWriter writer, Vector3 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);

        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }
    public Vector3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        int count = reader.ReadArrayHeader();
        if (3 != count)
        {
            throw new System.NotImplementedException();
        }

        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();

        return new Vector3(x, y, z);
    }
}
#endif