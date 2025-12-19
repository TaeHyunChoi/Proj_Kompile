#if UNITY_EDITOR
namespace Study.Pathfind
{
    using MessagePack;
    using MessagePack.Resolvers;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    public sealed class STUDY_NodeCacheManager
    {
        private Dictionary<long, STUDY_NodeData> nodeMap;
        private Dictionary<int3, long> posToID;

        public void InitializeFromList(List<STUDY_NodeData> list)
        {
            int count = list.Count;
            nodeMap = new Dictionary<long, STUDY_NodeData>(capacity: count);
            posToID = new Dictionary<int3, long>(capacity: count);

            foreach (var n in list)
            {
                nodeMap.Add(n.ID, n);
                posToID.Add(n.ComputeAbsPosition(), n.ID);
            }
        }
        public async Awaitable LoadFromAddressableAsync(string addressOrLabel)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(addressOrLabel);
            var ta = await handle.Task;
            if (null == ta)
            {
                Debug.LogError($"NodeCacheManager: Addressable not found: {addressOrLabel}");
                return;
            }

            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            var list = MessagePackSerializer.Deserialize<List<STUDY_NodeData>>(ta.bytes, options);
            InitializeFromList(list);

            Addressables.Release(handle);
            Debug.Log($"NodeCacheManager: Load {list.Count} nodes from Addressable: {addressOrLabel}");
        }

        public IReadOnlyDictionary<long, STUDY_NodeData> NodeMap => nodeMap; // "Add, Remove, Clear 같은 수정 메서드가 없습니다." - 엄청 좋은거네?
        public bool TryGetNode(long id, out STUDY_NodeData node)
        {
            node = null;
            if (null == nodeMap)
            {
                return false;
            }

            return nodeMap.TryGetValue(id, out node);
        }
        public bool TryGetID(int3 absPos, out long id)
        {
            id = 0L;
            if (null == posToID)
            {
                return false;
            }

            return posToID.TryGetValue(absPos, out id);
        }
    }

}
#endif