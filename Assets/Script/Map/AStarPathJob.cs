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
            var allNodes  = new NativeList<PathVerticeNode>(Allocator.Temp);
            var closedSet = new NativeHashMap<int3, int>(1024, Allocator.Temp);
            var openHeap  = new NativeList<int>(Allocator.Temp);


            // add start vertice path node
            int3 startVerticeInt = GetVerticeInt(StartPos);
            allNodes.Add(new PathVerticeNode
            {
                VerticeInt  = startVerticeInt,
                ParentIndex = -1,
                G = 0,
                H = math.distance(StartPos, EndPos)
            });
            openHeap.Add(0);
            closedSet.Add(startVerticeInt, 0);

            int nextIndex;

            // A* Loop
            while (0 < openHeap.Length)
            {
                int currIndex = PopMinHeap(ref openHeap, ref allNodes);
                PathVerticeNode currentNode = allNodes[currIndex];

                if (PATH_SEARCH_UNIT >= math.distance(currentNode.Vertice, EndPos))
                {
                    ReconstructPath(currIndex, allNodes);
                    //ResultPath.Add(EndPos);
                    break;
                }

                // 탐색 위치 (current) => Tile 조회
                int3 currentVerticeInt = currentNode.VerticeInt;
                long currentID = MapPathUtil.ComputeTileIDInt(currentVerticeInt);
                if (false == Map.TryGetValue(currentID, out (long navi, long link) item))
                {
                    continue;
                }

                for (int i = 0; i < NEIGHBOR_OFFSETS_INT.Length; ++i)
                {
                    //이웃 x,z 값을 우선 구한 후 -> 이후에 y값을 더하는 것으로..
                    int3 targetVerticeInt = currentVerticeInt + NEIGHBOR_OFFSETS_INT[i];
                    long targetID = MapPathUtil.ComputeTileIDInt(targetVerticeInt);
                    float3 pos = PATH_SEARCH_UNIT * new float3(targetVerticeInt.x, targetVerticeInt.y, targetVerticeInt.z);

                    // vertice 이동을 하니 다른 타일이다 -> 이웃 타일 탐색
                    if (targetID != currentID)
                    {
                        int3 diffInt = GetTileDiff(currentID, targetID);

                        int yMask;
                        switch ((diffInt.x, diffInt.z))
                        {
                            case (-1, -1): yMask = 0; break;
                            case ( 0, -1): yMask = 1; break;
                            case ( 1, -1): yMask = 2; break;
                            case ( 1,  0): yMask = 3; break;
                            case ( 1,  1): yMask = 4; break;
                            case ( 0,  1): yMask = 5; break;
                            case (-1,  1): yMask = 6; break;
                            case (-1,  0): yMask = 7; break;
                            default:
                                continue;
                        }

                        int y = (int)(item.link >> (yMask * 2)) & 0b11;
                        switch (y)
                        {
                            case LINK_ZERO: y = 0;  break;
                            case LINK_UP:   y = 1;  break;
                            case LINK_DOWN: y = -1; break;
                            default:
                                continue;
                        }

                        targetVerticeInt += PATH_SEARCH_RECIPROCAL * new int3(0, y, 0);
                        targetID = MapPathUtil.ComputeTileIDInt(targetVerticeInt);
                        if (false == Map.TryGetValue(targetID, out item))
                        {
                            continue;
                        }
                    }

                    // 이미 방문한 노드인지 확인
                    if (true == closedSet.ContainsKey(targetVerticeInt))
                    {
                        continue;
                    }

                    // 이동한 vertice에서 확인해야 할 타일: 본인
                    float3 targetPivot    = MapPathUtil.ComputeWorldPosition(targetID);
                    int3   targetPivotInt = PATH_SEARCH_RECIPROCAL * MapPathUtil.ComputeWorldPositionInt(targetID);
                    float3 circleCenter   = PATH_SEARCH_UNIT * new float3(targetVerticeInt.x, targetVerticeInt.y, targetVerticeInt.z);
                    if (false == IsVerticeMovable(item.navi, targetPivot, circleCenter, Radius))
                    {
                        // 해당 지점으로 이동 불가; 탐색 중지
                        continue;
                    }

                    // 이동한 vertice에서 확인해야 할 타일: 주변부
                    int verticeIndex = GetVerticeIndex(targetVerticeInt - targetPivotInt);
                    if (false == TryGetNeighborLinkIndex(verticeIndex, out int linkIndex, out int length))
                    {
                        continue;
                    }
                    else if (0 == length) // v06: 자기 본인은 앞에서 확인 완료
                    {
                        allNodes.Add(new PathVerticeNode()
                        {
                            VerticeInt = targetVerticeInt,
                            ParentIndex = currIndex,
                            G = currentNode.G + GetMoveCost(i),
                            H = math.distance(targetVerticeInt, EndPos)
                        });

                        nextIndex = allNodes.Length - 1;
                        closedSet.Add(targetVerticeInt, nextIndex);
                        PushMinHeap(ref openHeap, ref allNodes, nextIndex);
                        break;
                    }

                    for (int l = 0; l < length; ++l)
                    {
                        // 연산 순서가 잘못 되었음
                        // 거리가 닿는지 먼저 판단한 다음에 => 닿으면 연결 여부를 확인해야 한다.
                        // 이걸 어떻게 해야 좋을까~!! 
                        

                        // 연결 여부 확인
                        int x, y, z;
                        int index = (linkIndex + l + 8) % 8;
                        if (false == MapPathUtil.TryGetYInt(item.link, index, out y))
                        {
                            goto CONTINUE;
                        }

                        switch (index)
                        {
                            case 0: x = -1; z = -1; break;
                            case 1: x =  0; z = -1; break;
                            case 2: x =  1; z = -1; break;
                            case 3: x =  1; z =  0; break;
                            case 4: x =  1; z =  1; break;
                            case 5: x =  0; z =  1; break;
                            case 6: x = -1; z =  1; break;
                            case 7: x = -1; z =  0; break;
                            default:
                                goto CONTINUE;
                        }

                        int3 neighborPivotInt = targetPivotInt + PATH_SEARCH_RECIPROCAL * new int3(x, y, z);
                        long neighborID       = MapPathUtil.ComputeTileIDInt(neighborPivotInt);

                        if (false == Map.ContainsKey(neighborID))
                        {
                            goto CONTINUE;
                        }

                        long neighborNaviMask = Map[targetID].Navi;
                        if (false == IsVerticeMovable(neighborNaviMask, neighborPivotInt, circleCenter, Radius))
                        {
                            // 하나라도 이동이 불가하면 해당 정점에서 유효성 확인 종료;
                            goto CONTINUE;
                        }
                    }

                    // 이미 방문한 노드인지 확인
                    if (true == closedSet.ContainsKey(targetVerticeInt))
                    {
                        continue;
                    }

                    allNodes.Add(new PathVerticeNode()
                    {
                        VerticeInt  = targetVerticeInt,
                        ParentIndex = currIndex,
                        G = currentNode.G + GetMoveCost(i),
                        H = math.distance(targetVerticeInt, EndPos)
                    });

                    nextIndex = allNodes.Length - 1;
                    closedSet.Add(targetVerticeInt, nextIndex);
                    PushMinHeap(ref openHeap, ref allNodes, nextIndex);

                CONTINUE:
                    continue;
                }
            }

            // dispose native
            allNodes.Dispose();
            closedSet.Dispose();
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