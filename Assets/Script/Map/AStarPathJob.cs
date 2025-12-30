namespace Script.Map
{
    using UnityEngine;
    using Unity.Jobs;
    using Unity.Burst;
    using Unity.Collections;
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
        [ReadOnly] public NativeHashMap<long, (long,long)> Map; // (Key:TileID, Value:NaviMask)

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

        // A* 방향 벡터 (상하좌우 + 대각선): 0.25f 단위 이동을 정수 좌표(1)로 표현
        private static readonly int3[] NEIGHBOR_OFFSETS_INT = new int3[]
        {
            new int3(-2, 0, -2), new int3(0, 0, -4), new int3( 2, 0, -2), new int3( 4, 0, 0),
            new int3( 2, 0, 2),  new int3(0, 0,  4), new int3(-2, 0,  2), new int3(-4, 0, 0)
        };

        public void Execute()
        {
            // init data
            var allNodes = new NativeList<PathVerticeNode>(Allocator.Temp);
            var closedSet = new NativeHashMap<int3, int>(1024, Allocator.Temp);
            var openHeap = new NativeList<int>(Allocator.Temp);

            // add start vertice path node
            int3 startVerticeInt = GetVerticeInt(StartPos);
            allNodes.Add(new PathVerticeNode
            {
                VerticeInt = startVerticeInt,
                ParentIndex = -1,
                G = 0,
                H = math.distance(StartPos, EndPos)
            });
            openHeap.Add(0);
            closedSet.Add(startVerticeInt, 0);


            // A* Loop
            while (0 < openHeap.Length)
            {
                int currIndex = PopMinHeap(ref openHeap, ref allNodes);
                PathVerticeNode currentNode = allNodes[currIndex];

                if (PATH_SEARCH_UNIT * 2 >= math.distance(currentNode.Vertice, EndPos))
                {
                    ReconstructPath(currIndex, allNodes);
                    ResultPath.Add(EndPos);
                    break;
                }

                // 탐색 위치 (current) => Tile 조회
                int3 currentVerticeInt = currentNode.VerticeInt;
                long currentID = MapPathUtil.ComputeTileIDInt(currentVerticeInt);
                if (false == Map.TryGetValue(currentID, out (long navi, long link) item))
                {
                    continue;
                }

                // 탐색 위치 (current) => Sub-Tile 조회
                int subAreaIndex = GetSubTileIndexInt(currentVerticeInt.x, currentVerticeInt.z);
                if (false == MapPathUtil.IsSubTileValid(item.navi, subAreaIndex))
                {
                    continue;
                }

                // 이웃 노드 탐색
                for (int i = 0; i < NEIGHBOR_OFFSETS_INT.Length; ++i)
                {
                    const int LINK_ZERO = 0b_01;
                    const int LINK_UP   = 0b_10;
                    const int LINK_DOWN = 0b_11;
                    const int LINK_NONE = 0b_00;

                    int3 targetVerticeInt = currentVerticeInt + new int3(NEIGHBOR_OFFSETS_INT[i].x, 0, NEIGHBOR_OFFSETS_INT[i].z);
                    long targetID = MapPathUtil.ComputeTileIDInt(targetVerticeInt);

                    // 타겟 정점이 다른 타일에 존재한다면? 
                    if (targetID != currentID)
                    {
                        // 유효한 타일인지 확인
                        if (false == Map.ContainsKey(targetID))
                        {
                            continue;
                        }

                        // 이게 어느 방향으로 연결되었는지를 확인해야 하는덴
                        // link 확인 (i번째 방향으로 길이 열려 있는가?)
                        int yMask = (int)(item.link >> (i * 2)) & 0b11;
                        if (LINK_NONE == yMask)
                        {
                            continue;
                        }

                        // (다른 타일이므로) y값 갱신
                        int yInt;
                        switch (yMask)
                        {
                            case LINK_ZERO: yInt = 0; break;
                            case LINK_UP: yInt = 1; break;
                            case LINK_DOWN: yInt = -1; break;
                            default:
                                continue;
                        }
                        targetVerticeInt += new int3(0, yInt, 0);
                    }

                    // 타겟 위치에서 이동 가능한지 여부 확인
                    if (false == IsPositionWalkable(targetVerticeInt))
                    {
                        continue;
                    }

                    // 이미 방문한 노드인지 확인
                    if (true == closedSet.ContainsKey(targetVerticeInt))
                    {
                        continue;
                    }

                    float3 targetVertice = PATH_SEARCH_UNIT * new float3(targetVerticeInt.x, targetVerticeInt.y, targetVerticeInt.z);
                    allNodes.Add(new PathVerticeNode
                    {
                        VerticeInt = targetVerticeInt,
                        ParentIndex = currIndex,
                        G = currentNode.G + GetMoveCost(i),
                        H = math.distance(targetVertice, EndPos)
                    });
                    int nextIndex = allNodes.Length - 1;
                    closedSet.Add(targetVerticeInt, nextIndex);
                    PushMinHeap(ref openHeap, ref allNodes, nextIndex);
                }
            }

            // dispose native
            allNodes.Dispose();
            closedSet.Dispose();
            openHeap.Dispose();
        }

        public static int GetSubTileIndexInt(int indexX, int indexZ)
        {
            // 1. 유닛 타일(1.0f 크기) 내의 로컬 인덱스 (0..7) 구하기
            // 설명: 1.0f는 0.125f가 8개 모인 것이므로 8로 나눈 나머지
            int lx = (indexX % 8 + 8) % 8;
            int lz = (indexZ % 8 + 8) % 8;

            // 2. 사분면(Quadrant) 판별
            // 0.125f 기준이므로 8칸의 절반인 4가 0.5f 지점입니다.
            int col = (lx >= 4) ? 1 : 0;
            int row = (lz >= 4) ? 1 : 0;

            // 각 사분면의 서브타일 시작 인덱스 (s0, s4, s8, s12)
            int baseIndex = (row * 8) + (col * 4);

            // 3. 사분면 내 중심점(Pivot) 인덱스 계산 (여기가 핵심 수정 사항)
            // 0.25f 단위일 때: (col * 2) + 1  => 결과: 1, 3
            // 0.125f 단위일 때: (col * 4) + 2  => 결과: 2, 6
            // 이유: 
            //   Col 0 (0.0~0.5)구역의 중심 0.25는 0.125 단위로 '2'입니다.
            //   Col 1 (0.5~1.0)구역의 중심 0.75는 0.125 단위로 '6'입니다.
            int cx = (col * 4) + 2;
            int cz = (row * 4) + 2;

            // 4. 중심으로부터의 오차 (dx, dz)
            int dx = lx - cx;
            int dz = lz - cz;

            // 5. 방향 결정 (절대값 비교)
            // 인덱스 범위가 커졌을 뿐(dx가 -2 ~ +1 범위), 대소 비교 로직은 동일하게 유효합니다.
            int offset;

            int absDx = (dx < 0) ? -dx : dx;
            int absDz = (dz < 0) ? -dz : dz;

            if (absDx > absDz)
            {
                // 가로형 (Left/Right)
                offset = (dx > 0) ? 1 : 3;
            }
            else
            {
                // 세로형 (Top/Bottom)
                offset = (dz > 0) ? 2 : 0;
            }

            return baseIndex + offset;
        }
        private readonly int3 GetVerticeInt(float3 p)
        {
            int x = (int)math.round(p.x * PATH_SEARCH_RECIPROCAL);
            int y = (int)math.round(p.y * PATH_SEARCH_RECIPROCAL);
            int z = (int)math.round(p.z * PATH_SEARCH_RECIPROCAL);

            return new int3(x, y, z);
        }
        private bool IsPositionWalkable(int3 posInt)
        {
            // [설정값 계산]
            // Radius(0.35f) / PATH_SEARCH_UNIT(0.125f) = 2.8
            // 정수 좌표계에서의 탐색 범위: Ceil(2.8) = 3칸
            const int SEARCH_RANGE = 3;

            // 거리 제곱 임계값: 2.8 * 2.8 = 7.84
            // 정수 거리 제곱이 7 이하이면 반경 내에 포함됨 (8 이상은 포함 안 됨)
            const int RADIUS_SQ_LIMIT = 7;

            // 1. posInt를 중심으로 +-3칸 범위 순회
            // minX, maxX 등의 변수 할당 없이 루프 범위로 직접 제어
            for (int dx = -SEARCH_RANGE; dx <= SEARCH_RANGE; ++dx)
            {
                for (int dz = -SEARCH_RANGE; dz <= SEARCH_RANGE; ++dz)
                {
                    // 2. 정수 거리 제곱 계산 (x^2 + z^2)
                    // float math.lengthsq()를 완전히 대체합니다.
                    int distSq = (dx * dx) + (dz * dz);

                    // 3. 반경(2.8칸) 밖이면 검사 제외
                    if (distSq > RADIUS_SQ_LIMIT)
                    {
                        continue;
                    }

                    // 4. 검사할 타겟의 절대 정수 좌표 계산
                    int targetX = posInt.x + dx;
                    int targetZ = posInt.z + dz;

                    // 5. Tile ID 계산 (Int 버전 사용)
                    // posInt.y는 현재 높이를 그대로 사용 (필요시 y 탐색 범위 추가 가능)
                    long targetID = MapPathUtil.ComputeTileIDInt(new int3(targetX, posInt.y, targetZ));

                    // 6. Map 데이터 존재 여부 확인
                    // 데이터가 아예 없으면(void), 이동 불가로 치지 않고 무시(continue)하는 기존 로직 유지
                    if (false == Map.TryGetValue(targetID, out (long navi, long link) item))
                    {
                        continue;
                    }

                    // 7. Sub-Tile 유효성 검사 (Int 버전 사용)
                    // float 변환 없이 정수 좌표를 그대로 넘김
                    if (false == MapPathUtil.IsSubTileValid(item.navi, MapPathUtil.GetSubTileIndex(targetX, targetZ)))
                    {
                        // 반경 내에 "데이터는 있는데 유효하지 않은(높이 1111)" 서브타일이 있으면 이동 불가 판정
                        return false;
                    }
                }
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