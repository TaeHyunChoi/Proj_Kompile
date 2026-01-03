namespace Script.Map
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    [BurstCompile]
    public struct AStarPathJob : IJob
    {

        private const float PATH_SEARCH_UNIT       = 0.125f;  // sub-tile은 0.25 간격이지만 y값은 0.125 간격이라서 단위를 하나로 통일하였음
        private const int   PATH_SEARCH_RECIPROCAL = 8;

        // --- input data ---
        [ReadOnly] public float3 StartPos;
        [ReadOnly] public float3 EndPos;
        [ReadOnly] public float  Radius;
        [ReadOnly] public NativeHashMap<long, (long Navi, long Link)> Map; // (Key:TileID, Value:NaviMask)

        // --- output data ---
        public NativeList<float3> ResultPath;

        // --- internal structs ----
        private struct PathVerticeNode
        {
            public int3 VerticeInt;     // 정수형 좌표 (부동 소수점 오차 방지)
            public int ParentIndex;     // 경로 역추적을 위한 부모 노드 인덱스
            public float G;             // 지금까지 온 거리
            public float H;             // 앞으로 갈 거리

            public readonly float F => G + H;    // 거리 비용 총합
            public readonly float3 Vertice
            {
                get
                {
                    return PATH_SEARCH_UNIT * new float3(VerticeInt.x, VerticeInt.y, VerticeInt.z);
                }
            }
        }

        private const int LINK_ZERO = 0b_01;
        private const int LINK_UP   = 0b_10;
        private const int LINK_DOWN = 0b_11;
        private const int LINK_NONE = 0b_00;
        private const int LINK_MASK = 0b_11;

        // A* 방향 벡터 (상하좌우 + 대각선): 0.25f( = 2* PATH_SEARCH_UNIT) 단위 이동을 정수 좌표(1)로 표현
        private static readonly int3[] NEIGHBOR_OFFSETS_INT = new int3[]
        {
            new int3(-2, 0, -2), new int3(0, 0, -4), new int3( 2, 0, -2), new int3( 4, 0, 0),
            new int3( 2, 0, 2),  new int3(0, 0,  4), new int3(-2, 0,  2), new int3(-4, 0, 0)
        };

        public void Execute()
        {
            // init data
            var allNodes = new NativeList<PathVerticeNode>(Allocator.Temp);
            var gCosts = new NativeHashMap<int3, float>(1024, Allocator.Temp);
            var openHeap = new NativeList<int>(Allocator.Temp);

            // --- Start Node 설정 ---
            int3 startVerticeInt = GetVerticeInt(StartPos);

            // 시작 지점 유효성 체크
            long startID = MapPathUtil.ComputeTileIDInt(startVerticeInt);
            if (false == Map.ContainsKey(startID))
            {
                allNodes.Dispose(); gCosts.Dispose(); openHeap.Dispose();
                return;
            }

            allNodes.Add(new PathVerticeNode
            {
                VerticeInt = startVerticeInt,
                ParentIndex = -1,
                G = 0,
                H = math.distance(StartPos, EndPos)
            });
            openHeap.Add(0);
            gCosts.Add(startVerticeInt, 0);

            int nextIndex;

            // A* Loop
            while (0 < openHeap.Length)
            {
                // 1. Min Heap Pop
                int currIndex = PopMinHeap(ref openHeap, ref allNodes);
                PathVerticeNode currentNode = allNodes[currIndex];
                int3 currentVerticeInt = currentNode.VerticeInt;

                // [Lazy Deletion] 더 짧은 경로로 이미 방문했다면 스킵
                if (gCosts.TryGetValue(currentVerticeInt, out float bestG))
                {
                    if (currentNode.G > bestG)
                    {
                        continue;
                    }
                }

                // [도착 판정]
                if (PATH_SEARCH_UNIT >= math.distance(currentNode.Vertice, EndPos))
                {
                    ReconstructPath(currIndex, allNodes);
                    break;
                }

                long currentID = MapPathUtil.ComputeTileIDInt(currentVerticeInt);

                // 현재 타일 정보 가져오기 ('currentItem'으로 명명하여 오염 방지)
                if (false == Map.TryGetValue(currentID, out (long navi, long link) currentItem))
                {
                    continue;
                }

                // 이웃 순회
                for (int i = 0; i < NEIGHBOR_OFFSETS_INT.Length; ++i)
                {
                    int3 targetVerticeInt = currentVerticeInt + NEIGHBOR_OFFSETS_INT[i];
                    long targetID = MapPathUtil.ComputeTileIDInt(targetVerticeInt);

                    // [중요] 타겟 타일 정보 초기화 (기본값: 현재 타일)
                    // 루프 돌 때마다 리셋되어야 변수 오염이 발생하지 않음
                    (long navi, long link) targetItem = currentItem;

                    // 1. 타일 경계(Link) 처리
                    if (targetID != currentID)
                    {
                        int3 diffInt = GetTileDiff(currentID, targetID);
                        int yMask;

                        switch ((diffInt.x, diffInt.z))
                        {
                            case (-1, -1): yMask = 0; break;
                            case (0, -1): yMask = 1; break;
                            case (1, -1): yMask = 2; break;
                            case (1, 0): yMask = 3; break;
                            case (1, 1): yMask = 4; break;
                            case (0, 1): yMask = 5; break;
                            case (-1, 1): yMask = 6; break;
                            case (-1, 0): yMask = 7; break;
                            default: continue;
                        }

                        // Link 정보를 통해 층(Layer) 이동 확인 (0, +1, -1)
                        int y = (int)(currentItem.link >> (yMask * 2)) & 0b11;
                        switch (y)
                        {
                            case LINK_ZERO: y = 0; break;
                            case LINK_UP: y = 1; break;
                            case LINK_DOWN: y = -1; break;
                            default: continue;
                        }

                        targetVerticeInt += PATH_SEARCH_RECIPROCAL * new int3(0, y, 0);
                        targetID = MapPathUtil.ComputeTileIDInt(targetVerticeInt);

                        // 이동한 타일의 정보로 갱신
                        if (false == Map.TryGetValue(targetID, out targetItem))
                        {
                            continue;
                        }
                    }

                    // 2. [Slope Logic] NaviMask 높이 적용
                    // 타일의 기준 층(Base Layer) 계산 (음수 좌표 대응)
                    int baseLayerY = (int)math.floor((float)targetVerticeInt.y / PATH_SEARCH_RECIPROCAL) * PATH_SEARCH_RECIPROCAL;

                    // 타일 내 로컬 좌표(0~8) 계산
                    // targetPivot(타일 원점)을 구해서 차이를 계산하는 것이 가장 정확함
                    int3 targetPivotInt = PATH_SEARCH_RECIPROCAL * MapPathUtil.ComputeWorldPositionInt(targetID);
                    int3 localPos = targetVerticeInt - targetPivotInt;

                    int vIndex = MapPathUtil.GetVertexIndexFromLocalPos(localPos.x, localPos.z);
                    if (vIndex != -1)
                    {
                        int heightY = MapPathUtil.GetHeightFromNaviMask(targetItem.navi, vIndex);

                        // 구멍(15)이면 이동 불가
                        if (heightY == MapPathUtil.NONE_SUBTILE)
                        {
                            continue;
                        }

                        // 높이 갱신: 기준 층 + NaviMask높이
                        targetVerticeInt.y = baseLayerY + heightY;
                    }

                    // 3. 이동 유효성 검사 (IsVerticeMovable)
                    float3 targetPivot = MapPathUtil.ComputeWorldPosition(targetID); // World Pivot (float)
                    // PivotInt는 위에서 구한 값 재사용 가능하나, 안전을 위해 재계산하거나 그대로 사용
                    targetPivotInt = PATH_SEARCH_RECIPROCAL * MapPathUtil.ComputeWorldPositionInt(targetID);

                    float3 circleCenter = PATH_SEARCH_UNIT * new float3(targetVerticeInt.x, targetVerticeInt.y, targetVerticeInt.z);

                    // [수정] targetItem.navi 사용
                    if (false == IsVerticeMovable(targetItem.navi, targetPivot, circleCenter, Radius))
                    {
                        continue;
                    }

                    // 4. 주변 연결(Link) 검사
                    int verticeIndex = GetVerticeIndex(targetVerticeInt - targetPivotInt);
                    if (false == TryGetNeighborLinkIndex(verticeIndex, out int linkIndex, out int length))
                    {
                        continue;
                    }
                    else if (0 == length)
                    {
                        goto ADD_PATH;
                    }

                    for (int l = 0; l < length; ++l)
                    {
                        int index = (linkIndex + l + 8) % 8;
                        int x, y, z;

                        switch (index)
                        {
                            case 0: x = -1; z = -1; break;
                            case 1: x = 0; z = -1; break;
                            case 2: x = 1; z = -1; break;
                            case 3: x = 1; z = 0; break;
                            case 4: x = 1; z = 1; break;
                            case 5: x = 0; z = 1; break;
                            case 6: x = -1; z = 1; break;
                            case 7: x = -1; z = 0; break;
                            default: goto CONTINUE;
                        }

                        // 범위 검사
                        long tempID = MapPathUtil.ComputeTileIDInt(targetPivotInt + PATH_SEARCH_RECIPROCAL * new int3(x, 0, z));
                        var tempInt = MapPathUtil.ComputeWorldPositionInt(tempID);
                        if (false == MapPathUtil.IsCircleOverlappingSquare(tempInt, new float2(circleCenter.x, circleCenter.z), Radius))
                        {
                            continue;
                        }

                        // 이웃 타일 연결성 확인 (targetItem.link 사용)
                        if (false == MapPathUtil.TryGetYInt(targetItem.link, index, out y))
                        {
                            goto CONTINUE;
                        }

                        int3 neighborPivotInt = targetPivotInt + PATH_SEARCH_RECIPROCAL * new int3(x, y, z);
                        long neighborID = MapPathUtil.ComputeTileIDInt(neighborPivotInt);

                        if (false == Map.TryGetValue(neighborID, out var neighborItem))
                        {
                            goto CONTINUE;
                        }

                        // [중요] 이웃 타일의 NaviMask 체크 (neighborItem.Navi 사용)
                        if (false == IsVerticeMovable(neighborItem.Navi, neighborPivotInt, circleCenter, Radius))
                        {
                            goto CONTINUE;
                        }
                    }

                ADD_PATH:
                    float newG = currentNode.G + GetMoveCost(i);
                    bool foundBetterPath = false;

                    // 비용 갱신 및 큐 추가
                    if (gCosts.TryGetValue(targetVerticeInt, out float oldG))
                    {
                        if (newG < oldG)
                        {
                            gCosts[targetVerticeInt] = newG;
                            foundBetterPath = true;
                        }
                    }
                    else
                    {
                        gCosts.Add(targetVerticeInt, newG);
                        foundBetterPath = true;
                    }

                    if (foundBetterPath)
                    {
                        allNodes.Add(new PathVerticeNode()
                        {
                            VerticeInt = targetVerticeInt,
                            ParentIndex = currIndex,
                            G = newG,
                            H = math.distance(targetVerticeInt, EndPos)
                        });

                        nextIndex = allNodes.Length - 1;
                        PushMinHeap(ref openHeap, ref allNodes, nextIndex);
                    }

                CONTINUE:
                    continue;
                }
            }

            // dispose native
            allNodes.Dispose();
            gCosts.Dispose();
            openHeap.Dispose();
        }

        private readonly int3 GetVerticeInt(float3 p)
        {
            int x = (int)math.round(p.x * PATH_SEARCH_RECIPROCAL);
            int y = (int)math.round(p.y * PATH_SEARCH_RECIPROCAL);
            int z = (int)math.round(p.z * PATH_SEARCH_RECIPROCAL);

            return new int3(x, y, z);
        }
        private readonly int3 GetTileDiff(long idFrom, long idTo)
        {
            const int TILE_BITS = 6;
            const int TILE_MASK = 0x3F;

            int gX1 = (sbyte)((idFrom >> 48) & 0xFF);
            int gZ1 = (sbyte)((idFrom >> 32) & 0xFF);

            int gX2 = (sbyte)((idTo >> 48) & 0xFF);
            int gZ2 = (sbyte)((idTo >> 32) & 0xFF);

            int tX1 = (int)((idFrom >> 12) & TILE_MASK);
            int tZ1 = (int)((idFrom >> 0) & TILE_MASK);

            int tX2 = (int)((idTo >> 12) & TILE_MASK);
            int tZ2 = (int)((idTo >> 0) & TILE_MASK);

            int diffX = ((gX2 - gX1) << TILE_BITS) + (tX2 - tX1);
            int diffZ = ((gZ2 - gZ1) << TILE_BITS) + (tZ2 - tZ1);

            return new int3(diffX, 0, diffZ);
        }
        private readonly bool IsVerticeMovable(long naviMask, float3 tilePivot, float3 circleCenter, float radius)
        {
            float2 localCircleCenter = new float2(circleCenter.x - tilePivot.x, circleCenter.z - tilePivot.z);
            float radisuSq = radius * radius;

            // 모든 서브 타일에 대하여 순회
            for (int sIndex = 0; sIndex < 16; ++sIndex)
            {
                // 기하학적 교차 검사 (서브타일 삼각형 vs 원)
                if (false == MapPathUtil.IsCircleOverlappingSubTile(sIndex, localCircleCenter, radisuSq))
                {
                    continue;
                }
                if (false == MapPathUtil.IsSubTileValid(naviMask, sIndex))
                {
                    return false;
                }
            }

            return true;
        }
        private readonly int GetVerticeIndex(int3 diffInt)
        {
            switch ((diffInt.x, diffInt.z))
            {
                case (0, 0): return 0;
                case (4, 0): return 1;
                case (8, 0): return 2;
                case (2, 2): return 3;
                case (6, 2): return 4;
                case (0, 4): return 5;
                case (4, 4): return 6;
                case (8, 4): return 7;
                case (2, 6): return 8;
                case (6, 6): return 9;
                case (0, 8): return 10;
                case (4, 8): return 11;
                case (8, 8): return 12;
                default:
                    break;
            }

            // error
            return -1;
        }
        private readonly bool TryGetNeighborLinkIndex(int verticeIndex, out int linkIndex, out int length)
        {
            linkIndex = 0;
            length = 0;

            switch (verticeIndex)
            {
                // South West
                case 0:
                case 3:
                    linkIndex = 7;
                    length = 3;
                    break;

                // South
                case 1:
                    linkIndex = 1;
                    length = 1;
                    break;

                // South East
                case 2:
                case 4:
                    linkIndex = 1;
                    length = 3;
                    break;

                // MySelf: 자기 자신이라 탐색할 이웃이 없음
                case 6:
                    return true;

                // East
                case 7:
                    linkIndex = 3;
                    length = 1;
                    break;

                // North East
                case 9:
                case 12:
                    linkIndex = 3;
                    length = 3;
                    break;

                // North
                case 11:
                    length = 1;
                    break;

                // North West
                case 8:
                case 10:
                    linkIndex = 5;
                    length = 3;
                    break;

                // West
                case 5:
                    linkIndex = 7;
                    length = 1;
                    break;

                default:
                    break;
            }

            return true;
        }

        private readonly void PushMinHeap(ref NativeList<int> heap, ref NativeList<PathVerticeNode> nodes, int index)
        {
            heap.Add(index);

            int i = heap.Length - 1;
            while (0 < i)
            {
                int p = (int)((i - 1) * 0.5f);
                if (nodes[heap[i]].F >= nodes[heap[p]].F)
                {
                    break;
                }

                int temp = heap[i];
                heap[i] = heap[p];
                heap[p] = temp;

                i = p;
            }
        }
        private readonly int PopMinHeap(ref NativeList<int> heap, ref NativeList<PathVerticeNode> nodes)
        {
            int result = heap[0];

            int last = heap.Length - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            int i = 0;
            int length = heap.Length;
            while (true)
            {
                int smallest = i;
                int left     = 2 * i + 1;
                int right    = 2 * i + 2;

                if (left < length
                    && nodes[heap[left]].F < nodes[heap[smallest]].F)
                {
                    smallest = left;
                }
                if (right < length
                    && nodes[heap[right]].F < nodes[heap[smallest]].F)
                {
                    smallest = right;
                }
                if (smallest == i)
                {
                    break;
                }

                // swap
                int temp = heap[i];
                heap[i] = heap[smallest];
                heap[smallest] = temp;

                i = smallest;
            }

            return result;
        }
        private void ReconstructPath(int endIndex, NativeList<PathVerticeNode> nodes)
        {
            int curr = endIndex;
            while (-1 != curr)
            {
                float3 pathPos = nodes[curr].Vertice;
                ResultPath.Add(pathPos);

                curr = nodes[curr].ParentIndex;
            }

            // in-place reverse
            int count = ResultPath.Length;
            int half = (int)(count * 0.5f);
            for (int i = 0; i < half; ++i)
            {
                float3 temp = ResultPath[i];
                ResultPath[i] = ResultPath[count - 1 - i];
                ResultPath[count - 1 - i] = temp;
            }
        }
        private readonly float GetMoveCost(int i)
        {
            return i switch
            {
                1 or 3 or 5 or 7 => PATH_SEARCH_UNIT * 4,   // 직선 (상하좌우), 0.5
                _ => (PATH_SEARCH_UNIT * 2) * math.sqrt(2)
                //_ => 0.3535f //0.25 * math.sqrt(2),   // 대각선
            };
        }
    }
}