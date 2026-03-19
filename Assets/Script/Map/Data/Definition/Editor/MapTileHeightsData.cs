#if UNITY_EDITOR
namespace Script.Map.Data
{
    using System;
    using UnityEngine;

    /// <summary>
    /// [Framework] Data: 타일의 13개 포인트에 대한 높이 데이터를 관리합니다.
    /// </summary>
    [Serializable]
    public struct MapTileHeightsData
    {
        // 13개의 점에 대한 높이 인덱스 (-4 ~ 4)
        [Range(-4, 4)]
        public sbyte[] PointHeights;

        /// <summary>
        /// 배열이 비어있을 경우 안전하게 초기화합니다.
        /// </summary>
        public void EnsureInitialized()
        {
            if (PointHeights == null || PointHeights.Length != 13)
            {
                PointHeights = new sbyte[13];
            }
        }

        // 인덱서 추가 (외부에서 배열처럼 쉽게 접근하기 위함)
        public sbyte this[int index]
        {
            get
            {
                EnsureInitialized();
                return PointHeights[index];
            }
            set
            {
                EnsureInitialized();
                PointHeights[index] = value;
            }
        }
    }
}
#endif