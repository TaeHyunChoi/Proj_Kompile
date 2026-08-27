namespace Kompile.Provider
{
    using Data;
    using Utility;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Mathematics;
    using System.Collections.Generic;
    
    /// <summary> 맵 그리드의 바이너리 데이터 로드, 공간 위상 캐싱 등 순수 데이터를 공급 (Value-Centric) </summary>
    public class MapProvider
    {
        private const int GRID_SIZE = 64;
        
        private readonly Dictionary<int, MapGridData> _mapGridDataDic;
        private readonly HashSet<int> _invalidGrids;
        private readonly Dictionary<int, string> _gridKeyAddressCache;
        private NativeHashMap<long, MapTileInfo> _nativeTileMap;
        
        
        public NativeHashMap<long, MapTileInfo> NativeTileMap => _nativeTileMap;

        public bool IsInvalidGrid(int gridKey) => _invalidGrids.Contains(gridKey);

        // --- Initialize, Dispose ---
        public MapProvider()
        {
            _mapGridDataDic = new Dictionary<int, MapGridData>();
            _invalidGrids = new HashSet<int>();
            _gridKeyAddressCache = new Dictionary<int, string>();
            _nativeTileMap = new NativeHashMap<long, MapTileInfo>(512, Allocator.Persistent);
        }
        public void Dispose()
        {
            _mapGridDataDic.Clear();
            _invalidGrids.Clear();
            _gridKeyAddressCache.Clear();

            if (_nativeTileMap.IsCreated)
            {
                _nativeTileMap.Dispose();
            }
        }

        // --- Load/Unload Data ---
        public async Awaitable<MapGridData> LoadGridDataAsync(int gridKey)
        {
            if (_invalidGrids.Contains(gridKey))
            {
                return null;
            }
            
            // 문자열 보간 캐싱으로 GC Alloc 방지 (매번 새롭게 파싱해서 gc 낭비 ㄴㄴ)
            if (!_gridKeyAddressCache.TryGetValue(gridKey, out string addressKey))
            {
                addressKey = $"MapNavi_{gridKey}";
                _gridKeyAddressCache[gridKey] = addressKey;
            }

            MapGridData gridData = await AssetProvider.ReadBinaryDataAsync<MapGridData>(addressKey);
            
            // [안전장치] 비동기 대기 중에 Dispose가 호출되어 Native Collection이 파괴되었는지 확인
            if (!_nativeTileMap.IsCreated)
            {
                return null;
            }
            if (null == gridData)
            {
                _invalidGrids.Add(gridKey);
                return null;
            }
            
            _mapGridDataDic[gridKey] = gridData;
            
            sbyte gx = (sbyte)((gridKey >> 16) & 0xFF);
            sbyte gy = (sbyte)((gridKey >> 8) & 0xFF);
            sbyte gz = (sbyte)(gridKey & 0xFF);
            int baseTx = gx * 64;
            int baseTz = gz * 64;
            
            // 64x64 공간의 모든 수직 높이 레이어(0~63)를 스캔하여 복층 구조 타일을 굽기 
            // (현재 그리드의 모든 값은 미리 챙겨두기~)
            int columnCounter = 0;

            int gridSize = GRID_SIZE;
            for (int x = 0; x < gridSize; ++x)
            {
                for (int z = 0; z < gridSize; ++z)
                {
                    int globalTx = baseTx + x;
                    int globalTz = baseTz + z;

                    for (int y = 0; y < gridSize; ++y)
                    {
                        float worldY = gy * gridSize + y;
                        float3 testWorldPos = new float3(globalTx + 0.5f, worldY + 0.5f, globalTz + 0.5f);
                        InUtilMapKey.ComputeKey(testWorldPos, out int gKey, out int tKey);

                        if (gridData.TryGetTileData(tKey, out MapTileData tileData))
                        {
                            long packedKey = ((long)gKey << 32) | (uint)tKey;
                            InUtilMapKey.GetPivot(gKey, tKey, out float3 pivot);
                            _nativeTileMap[packedKey] = new MapTileInfo()
                            {
                                TileData = tileData,
                                TileBaseY =  pivot.y
                            };
                        }
                    }
                }
                
                // 렉 방지를 위해 병목을 분산
                ++columnCounter;
                if (0 == columnCounter % 8)
                {
                    await Awaitable.NextFrameAsync();
                    
                    // [안전장치] 프레임 대기 중에 Dispose 되었는지 검사
                    if (!_nativeTileMap.IsCreated)
                    {
                        return null;
                    }
                }
            }

            return gridData;
        }
        public void UnloadGridData(int gridKey)
        {
            if (!_nativeTileMap.IsCreated) return;

            sbyte gx = (sbyte)((gridKey >> 16) & 0xFF);
            sbyte gy = (sbyte)((gridKey >> 8) & 0xFF);
            sbyte gz = (sbyte)(gridKey & 0xFF);

            int gridSize = GRID_SIZE;
            int baseTx = gx * gridSize;
            int baseTz = gz * gridSize;

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        float worldY = gy * gridSize + y;
                        float3 testWorldPos = new float3(baseTx + x + 0.5f, worldY + 0.5f, baseTz + z + 0.5f);
                        InUtilMapKey.ComputeKey(testWorldPos, out int gKey, out int tKey);
                        long packedKey = ((long)gKey << 32) | (uint)tKey;
                        _nativeTileMap.Remove(packedKey);
                    }
                }
            }

            _mapGridDataDic.Remove(gridKey);
        }

        // --- Getter ---
        public bool TryGetTileData(in float3 worldPos, out MapTileData tileData)
        {
            InUtilMapKey.ComputeKey(in worldPos, out int gKey, out int tKey);
            if (_mapGridDataDic.TryGetValue(gKey, out MapGridData gridData))
            {
                return gridData.TryGetTileData(tKey, out tileData);
            }

            tileData = default;
            return false;
        }
    }
}
