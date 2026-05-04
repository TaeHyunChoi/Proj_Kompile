namespace Kompile.Asset.Provider
{
    using Unity.Collections;
    using UnityEngine;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Kompile.Asset.Data;

    /// <summary> 로컬라이제이션 데이터를 공급하는 Provider (Value-Centric) </summary>
    public static class LocalizationTableProvider
    {
        private static LocalizationTableData[] Sheets;
        
        // Key 문자열을 통한 빠른 참조를 위한 인덱스 캐시 (Manager에서 관리해도 무방함)
        private static readonly Dictionary<FixedString32Bytes, int> KeyIndexMap = new Dictionary<FixedString32Bytes, int>();

        public static async Task InitializeAsync()
        {
            // 1. 바이너리 배열 로드
            Sheets = await AssetProvider.ReadBinaryDataAsync<LocalizationTableData[]>(new AssetKey("LocalizationTable"));

            if (Sheets == null) return;

            // 2. Key 조회를 위한 인덱스 맵 구성 (초기 1회)
            KeyIndexMap.Clear();
            for (int i = 0; i < Sheets.Length; i++)
            {
                if (!KeyIndexMap.TryAdd(Sheets[i].Key, i))
                {
                    Debug.LogWarning($"[LocalizationTableProvider] 중복된 키 발견: {Sheets[i].Key}");
                }
            }
        }

        /// <summary> ID 기반 조회 (이진 탐색) </summary>
        public static ref readonly LocalizationTableData GetByID(int id)
        {
            int left = 0;
            int right = Sheets.Length - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (id == Sheets[mid].ID) return ref Sheets[mid];

                if (id < Sheets[mid].ID) right = mid - 1;
                else left = mid + 1;
            }

            throw new System.Exception($"[LocalizationTableProvider] ID 없음: {id}");
        }

        /// <summary> Key 기반 조회 (Dictionary Index) </summary>
        public static ref readonly LocalizationTableData GetByKey(string key)
        {
            FixedString32Bytes fixedKey = new FixedString32Bytes(key);
            if (KeyIndexMap.TryGetValue(fixedKey, out int index))
            {
                return ref Sheets[index];
            }

            throw new System.Exception($"[LocalizationTableProvider] Key 없음: {key}");
        }
    }
}