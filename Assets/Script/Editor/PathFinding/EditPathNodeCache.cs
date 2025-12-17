#if UNITY_EDITOR
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EditPathNodeCache
{
    private readonly string FILE_NAME = "path_node_cache.bin";
    private string savePath;
    private MessagePackSerializerOptions options;

    public EditPathNodeCache()
    {
        savePath = Path.Combine(Application.persistentDataPath, FILE_NAME);

        // MessagePack 초기화:
        // (1)Vector3 등을 사용하고 (2)IL2CPP 빌드를 하려면 Resolver 설정이 매우 중요하다.
        InitializeMessagePack();
    }

    private void InitializeMessagePack()
    {
        // 값이 있다면(!= null) 중단. 할 필요가 없다.
        if (false == MessagePackSerializer.DefaultOptions.Resolver.GetFormatter<PathNodeData>().Equals(null))
        {
            return;
        }

        var resolver = CompositeResolver.Create(
            new IMessagePackFormatter[] { EditUnityVector3Formatter.Instance }, // 커스텀 Formatter 목록
            new IFormatterResolver[]    { StandardResolver.Instance }       // 기존 Resolver 목록
        );

        options = MessagePackSerializerOptions.Standard
                  .WithResolver(resolver)
                  .WithCompression(MessagePackCompression.Lz4BlockArray);

        MessagePackSerializer.DefaultOptions = options;
    }

    public async Awaitable SaveNodesAsync(List<PathNodeData> nodes)
    {
        if (null == nodes)
        {
            Debug.LogWarning("[SaveNodesAsync()] 저장할 노드 데이터가 없습니다.");
            return;
        }

        try
        {
            using (var fileStream = File.Create(savePath))
            {
                await MessagePackSerializer.SerializeAsync(fileStream, nodes, options);
            }
            Debug.Log($" 맵 타일 저장 성공 ({nodes.Count} 개)\nPath = {savePath}");
        }
        catch(System.Exception e)
        {
            Debug.LogError("맵 타일 저장 실패");
        }
    }

    public async Awaitable<List<PathNodeData>> LoadNodesAsync()
    {
        if (false == File.Exists(savePath))
        {
            Debug.LogWarning("노드 캐시 파일이 없습니다.");
            return null;
        }

        try
        {
            using (var fileStream = File.OpenRead(savePath))
            {
                var nodes = await MessagePackSerializer.DeserializeAsync<List<PathNodeData>>(fileStream, options);
                Debug.Log($"노드 캐시 로드 (count: {nodes.Count})");
                return nodes;
            }
        }
        catch(System.Exception e)
        {
            Debug.LogError($"노드 캐시 로드 실패 ({e.Message})\nfile path:{savePath}");
            return null;
        }
    }
}
#endif