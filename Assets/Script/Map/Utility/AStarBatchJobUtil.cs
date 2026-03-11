namespace Script.Map.Utility
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

[BurstCompile]
    public struct AStarBatchJobUtil : IJobParallelFor
    {
        private const float PATH_SEARCH_UNIT = 0.125f;
        private const int PATH_SEARCH_RECIPROCAL = 8;
        private const int LINK_ZERO = 0b_01;
        private const int LINK_UP = 0b_10;
        private const int LINK_DOWN = 0b_11;

        [ReadOnly] public NativeArray<float3> StartPositions;
        [ReadOnly] public NativeArray<float3> EndPositions;
        [ReadOnly] public float Radius;
        [ReadOnly] public NativeHashMap<long, (long Navi, long Link)> Map;

        public NativeStream.Writer ResultPathStream;

        private struct PathVerticeNode
        {
            public int3 VerticeInt;
            public int ParentIndex;
            public float G;
            public float H;
            public readonly float F => G + H;
            public readonly float3 Vertice => PATH_SEARCH_UNIT * (float3)VerticeInt;
        }

        private static readonly int3[] NEIGHBOR_OFFSETS_INT = new int3[]
        {
            new int3(-2, 0, -2), new int3(0, 0, -4), new int3( 2, 0, -2), new int3( 4, 0, 0),
            new int3( 2, 0, 2),  new int3(0, 0,  4), new int3(-2, 0,  2), new int3(-4, 0, 0)
        };

        public void Execute(int index)
        {
            ResultPathStream.BeginForEachIndex(index);
            
            float3 startPos = StartPositions[index];
            float3 endPos = EndPositions[index];

            var allNodes = new NativeList<PathVerticeNode>(Allocator.Temp);
            var gCosts = new NativeHashMap<int3, float>(512, Allocator.Temp);
            var openHeap = new NativeList<int>(Allocator.Temp);

            int3 startVerticeInt = GetVerticeInt(startPos);
            long startID = MapCoordUtil.ComputeTileIDInt(startVerticeInt);

            if (!Map.ContainsKey(startID)) { Finalize(allNodes, gCosts, openHeap); return; }

            allNodes.Add(new PathVerticeNode {
                VerticeInt = startVerticeInt, ParentIndex = -1, G = 0, H = GetOctileDistance(startPos, endPos)
            });
            openHeap.Add(0);
            gCosts.Add(startVerticeInt, 0);

            int finalIndex = -1;

            while (openHeap.Length > 0)
            {
                int currIndex = PopMinHeap(ref openHeap, ref allNodes);
                PathVerticeNode currentNode = allNodes[currIndex];

                if (gCosts.TryGetValue(currentNode.VerticeInt, out float bestG) && currentNode.G > bestG) continue;

                if (PATH_SEARCH_UNIT >= math.distance(currentNode.Vertice, endPos))
                {
                    finalIndex = currIndex;
                    break;
                }

                long currentID = MapCoordUtil.ComputeTileIDInt(currentNode.VerticeInt);
                if (!Map.TryGetValue(currentID, out (long navi, long link) currentItem)) continue;

                for (int i = 0; i < NEIGHBOR_OFFSETS_INT.Length; i++)
                {
                    int3 targetVerticeInt = currentNode.VerticeInt + NEIGHBOR_OFFSETS_INT[i];
                    long targetID = MapCoordUtil.ComputeTileIDInt(targetVerticeInt);
                    (long navi, long link) targetItem = currentItem;

                    if (targetID != currentID)
                    {
                        int3 diff = GetTileDiff(currentID, targetID);
                        int yMask = GetYMask(diff.x, diff.z);
                        if (yMask == -1) continue;

                        int yBit = (int)(currentItem.link >> (yMask * 2)) & 0b11;
                        int yVal = yBit switch { LINK_ZERO => 0, LINK_UP => 1, LINK_DOWN => -1, _ => -99 };
                        if (yVal == -99) continue;

                        targetVerticeInt += PATH_SEARCH_RECIPROCAL * new int3(0, yVal, 0);
                        targetID = MapCoordUtil.ComputeTileIDInt(targetVerticeInt);
                        if (!Map.TryGetValue(targetID, out targetItem)) continue;
                    }

                    MapCoordUtil.ComputeWorldPositionInt(targetID, out int3 targetPivotInt);
                    targetPivotInt *= PATH_SEARCH_RECIPROCAL;
                    int vIndex = MapNaviTileUtil.GetVertexIndexFromLocalPos(targetVerticeInt.x - targetPivotInt.x, targetVerticeInt.z - targetPivotInt.z);
                    
                    if (vIndex != -1)
                    {
                        int heightY = MapNaviTileUtil.GetHeightFromNaviMask(targetItem.navi, vIndex);
                        if (heightY == 0b1111) continue;
                        targetVerticeInt.y = (int)math.floor((float)targetVerticeInt.y / PATH_SEARCH_RECIPROCAL) * PATH_SEARCH_RECIPROCAL + heightY;
                    }

                    float newG = currentNode.G + GetMoveCost(i);
                    if (gCosts.TryGetValue(targetVerticeInt, out float oldG) && newG >= oldG) continue;

                    MapCoordUtil.ComputeWorldPosition(targetID, out float3 targetPivot);
                    if (!IsVerticeMovable(targetItem.navi, targetPivot, targetVerticeInt, Radius)) continue;

                    gCosts[targetVerticeInt] = newG;
                    allNodes.Add(new PathVerticeNode {
                        VerticeInt = targetVerticeInt,
                        ParentIndex = currIndex,
                        G = newG,
                        H = GetOctileDistance(PATH_SEARCH_UNIT * (float3)targetVerticeInt, endPos)
                    });
                    PushMinHeap(ref openHeap, ref allNodes, allNodes.Length - 1);
                }
            }

            // [핵심] 경로 평탄화 프로세스 시작
            if (finalIndex != -1)
            {
                ApplyStringPulling(finalIndex, allNodes);
            }

            Finalize(allNodes, gCosts, openHeap);
            ResultPathStream.EndForEachIndex();
        }

        // --- 경로 평탄화 (String Pulling) 로직 ---
        private void ApplyStringPulling(int endIndex, NativeList<PathVerticeNode> nodes)
        {
            // 1. 역추적하여 원본 경로(격자 경로) 생성
            var rawPath = new NativeList<float3>(Allocator.Temp);
            int curr = endIndex;
            while (curr != -1)
            {
                rawPath.Add(nodes[curr].Vertice);
                curr = nodes[curr].ParentIndex;
            }

            // 2. 가시성 기반 지름길 추출
            if (rawPath.Length > 0)
            {
                int currentIdx = rawPath.Length - 1; // 시작점
                int count = 0;

                // 시작점 기록
                ResultPathStream.Write(rawPath[currentIdx]);
                count++;

                while (currentIdx > 0)
                {
                    int nextIdx = 0;
                    // 가장 멀리 있는 노드부터 검사 (지름길 우선)
                    for (int i = 0; i < currentIdx; i++)
                    {
                        if (HasLineOfSight(rawPath[currentIdx], rawPath[i]))
                        {
                            nextIdx = i;
                            break;
                        }
                    }
                    
                    // 지름길 노드 기록
                    ResultPathStream.Write(rawPath[nextIdx]);
                    count++;
                    currentIdx = nextIdx;

                    if (currentIdx == 0) break;
                }
                
                // 마지막에 정점 개수 기록
                ResultPathStream.Write(count);
            }
        }

        // --- 가시성 체크 (Line-of-Sight) ---
        // 
        private bool HasLineOfSight(float3 start, float3 end)
        {
            float dist = math.distance(start, end);
            if (dist < PATH_SEARCH_UNIT) return true;

            float3 dir = math.normalize(end - start);
            float step = PATH_SEARCH_UNIT * 1.5f; // 샘플링 간격
            float traveled = step;

            while (traveled < dist)
            {
                float3 checkPos = start + dir * traveled;
                int3 checkInt = GetVerticeInt(checkPos);
                long id = MapCoordUtil.ComputeTileIDInt(checkInt);

                if (!Map.TryGetValue(id, out var item)) return false;

                MapCoordUtil.ComputeWorldPosition(id, out float3 pivot);
                
                // 2.5D 높이 검증: 샘플링 지점의 실제 지형 높이와 경로 높이의 차이를 체크
                // HD-2D 스타일에서는 캐릭터 발밑이 지형에서 너무 떨어지거나 박히면 안 됨
                int3 pivotInt = (int3)math.round(pivot * PATH_SEARCH_RECIPROCAL);
                int vIndex = MapNaviTileUtil.GetVertexIndexFromLocalPos(checkInt.x - pivotInt.x, checkInt.z - pivotInt.z);
                
                if (vIndex != -1)
                {
                    int heightY = MapNaviTileUtil.GetHeightFromNaviMask(item.Navi, vIndex);
                    if (heightY == 0b1111) return false; // 구멍

                    float groundY = (math.floor(checkPos.y * 1) * 1) + (heightY * PATH_SEARCH_UNIT);
                    if (math.abs(checkPos.y - groundY) > 0.5f) return false; // 너무 큰 고저차 (절벽 등)
                }

                if (!IsVerticeMovable(item.Navi, pivot, checkInt, Radius)) return false;

                traveled += step;
            }

            return true;
        }

        // --- 기존 유틸리티 함수들 (동일) ---
        private float GetOctileDistance(float3 a, float3 b) {
            float3 d = math.abs(a - b);
            return (d.x + d.z) + (math.sqrt(2f) - 2f) * math.min(d.x, d.z) + math.abs(a.y - b.y);
        }
        private int3 GetVerticeInt(float3 p) => (int3)math.round(p * PATH_SEARCH_RECIPROCAL);
        private int GetYMask(int x, int z) => (x, z) switch { (-1, -1) => 0, (0, -1) => 1, (1, -1) => 2, (1, 0) => 3, (1, 1) => 4, (0, 1) => 5, (-1, 1) => 6, (-1, 0) => 7, _ => -1 };
        private int3 GetTileDiff(long idFrom, long idTo) {
            int gX1 = (sbyte)((idFrom >> 48) & 0xFF); int gZ1 = (sbyte)((idFrom >> 32) & 0xFF);
            int gX2 = (sbyte)((idTo >> 48) & 0xFF); int gZ2 = (sbyte)((idTo >> 32) & 0xFF);
            int tX1 = (int)((idFrom >> 12) & 0x3F); int tZ1 = (int)(idFrom & 0x3F);
            int tX2 = (int)((idTo >> 12) & 0x3F); int tZ2 = (int)(idTo & 0x3F);
            return new int3(((gX2 - gX1) << 6) + (tX2 - tX1), 0, ((gZ2 - gZ1) << 6) + (tZ2 - tZ1));
        }
        private bool IsVerticeMovable(long naviMask, float3 tilePivot, int3 vertInt, float radius) {
            float3 circleCenter = PATH_SEARCH_UNIT * (float3)vertInt;
            float2 localCenter = new float2(circleCenter.x - tilePivot.x, circleCenter.z - tilePivot.z);
            float rSq = radius * radius;
            for (int s = 0; s < 16; s++) {
                if (MapNaviTileUtil.IsCircleOverlappingSubTile(s, localCenter, rSq) && !MapNaviTileUtil.IsSubTileValid(naviMask, s)) return false;
            }
            return true;
        }
        private float GetMoveCost(int i) => (i % 2 == 1) ? 0.5f : 0.3535f;
        private void PushMinHeap(ref NativeList<int> heap, ref NativeList<PathVerticeNode> nodes, int index) {
            heap.Add(index); int i = heap.Length - 1;
            while (i > 0) {
                int p = (i - 1) / 2;
                if (nodes[heap[i]].F >= nodes[heap[p]].F) break;
                int t = heap[i]; heap[i] = heap[p]; heap[p] = t; i = p;
            }
        }
        private int PopMinHeap(ref NativeList<int> heap, ref NativeList<PathVerticeNode> nodes) {
            int res = heap[0]; heap[0] = heap[heap.Length - 1]; heap.RemoveAt(heap.Length - 1);
            int i = 0;
            while (true) {
                int s = i, l = 2 * i + 1, r = 2 * i + 2;
                if (l < heap.Length && nodes[heap[l]].F < nodes[heap[s]].F) s = l;
                if (r < heap.Length && nodes[heap[r]].F < nodes[heap[s]].F) s = r;
                if (s == i) break;
                int t = heap[i]; heap[i] = heap[s]; heap[s] = t; i = s;
            }
            return res;
        }
        private void Finalize(NativeList<PathVerticeNode> n, NativeHashMap<int3, float> g, NativeList<int> h) {
            n.Dispose(); g.Dispose(); h.Dispose();
        }
    }
}