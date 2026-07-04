namespace Kompile.Data
{
    using System;
    using UnityEngine;

    [Serializable]
    public struct MapTileHeightsData
    {
        [Range(-1, 8)] // -1은 '없음' 혹은 '기본'을 의미
        public sbyte[] PointHeights;

        public void EnsureInitialized()
        {
            if (PointHeights == null || PointHeights.Length != 13)
            {
                PointHeights = new sbyte[13];
                for (int i = 0; i < 13; i++) PointHeights[i] = -1;
            }
        }

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