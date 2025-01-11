namespace Script.Data
{
    using UnityEngine;
    using Script.Util;
    using System;

    [Serializable]
    public class MapGridData
    {
        // 인덱스는 Dicionary의 Key값으로 저장
        [SerializeField] private ConcurrentDictionary<int, MapNavData> navMeshData;


        public bool TryAddNavMeshData(int key, MapNavData navData)
        {
            return navMeshData.TryAdd(key, navData);
        }
    }
}
