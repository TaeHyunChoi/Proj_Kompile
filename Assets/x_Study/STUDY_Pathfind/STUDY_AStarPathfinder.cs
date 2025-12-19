namespace Study.Pathfind
{
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;

    public sealed class STUDY_AStarPathfinder
    {
        private readonly Dictionary<long, STUDY_NodeData> nodeMap;
        private readonly STUDY_BinaryMinHeap<long> open = new STUDY_BinaryMinHeap<long>();
        private readonly HashSet<long> closed = new HashSet<long>();
        private readonly Dictionary<long, long> cameFrom = new Dictionary<long, long>(); // (current:대상 위치가, prev:어디서부터 왔는가)
        private readonly Dictionary<long, float> gScore = new Dictionary<long, float>();

        public STUDY_AStarPathfinder(Dictionary<long, STUDY_NodeData> map)
        {
            nodeMap = map;
        }

        public bool TryFindPath(long startID, long targetID, out List<long> path)
        {
            path = null;
            // 출발점 | 도착점이 존재하지 않는다?
            if (false == nodeMap.ContainsKey(startID)
                || false == nodeMap.ContainsKey(targetID))
            {
                return false;
            }

            // 출발점 == 도착점?
            if (targetID == startID)
            {
                path = new List<long>() { startID };
                return true;
            }

            // reset
            open.Clear();
            cameFrom.Clear();
            gScore.Clear();
            closed.Clear();

            gScore[startID] = 0f;
            float fScore = 0f + Heuristic(startID, targetID);
            open.Enqueue(startID, fScore); // (id, 앞으로 남은 (추정) 거리)

            while (false == open.IsEmpty)
            {
                // 확인해야 할 노드가 남았나?
                if (false == open.TryDequeue(out long currentID))
                {
                    break;
                }

                // 목표 지점 도달?
                if (targetID == currentID)
                {
                    path = ReconstructPath(currentID);
                    return true;
                }

                // 이미 방문?
                if (false == closed.Add(currentID))
                {
                    continue;
                }

                // curr == current
                STUDY_NodeData currNode = nodeMap[currentID];
                ushort linkMask = currNode.LinkMask;
                float currG = GetScore(gScore, currentID);
                Vector3 currPos = currNode.ComputePosition();

                for (int dir = 0; dir < 8; ++dir)
                {
                    if (false == STUDY_LinkMaskUtil.TryGetYOffset(linkMask, dir, out int yOffset))
                    {
                        continue;
                    }

                    int3 neighborAbs = new int3((int)currPos.x, (int)(currPos.y + yOffset), (int)currPos.z);
                    if (false == STUDY_NodeCacheManager.Instance.TryGetID(neighborAbs, out long neighborID))
                    {
                        continue;
                    }
                    if (true == closed.Contains(neighborID))
                    {
                        continue;
                    }

                    int3 offset = STUDY_LinkMaskUtil.DirOffsets[dir];
                    STUDY_NodeData neighborNode = nodeMap[neighborID];
                    Vector3 neighborPos = neighborNode.ComputePosition();

                    // 그래서, 더 짧은 경로인가?
                    float tempG = currG + Vector3.Distance(currPos, neighborPos);
                    float nowG = GetScore(gScore, neighborID);
                    if (tempG >= nowG)
                    {
                        continue;
                    }

                    // 더 짧다면 값 갱신 -> 다음 탐색 경로로 추가
                    cameFrom[neighborID] = currentID;
                    gScore[neighborID] = tempG;
                    fScore = tempG + Heuristic(neighborID, targetID);
                    open.Enqueue(neighborID, fScore);
                }
            }

            return false;
        }

        private float Heuristic(long a, long b)
        {
            float3 pa = STUDY_PositionKeyUtil.ComputeWorldPosition(a);
            float3 pb = STUDY_PositionKeyUtil.ComputeWorldPosition(b);
            return Vector3.Distance(pa, pb);
        }
        private float GetScore(Dictionary<long, float> dic, long id)
        {
            if (true == dic.TryGetValue(id, out float score))
            {
                return score;
            }

            return float.PositiveInfinity;
        }
        private List<long> ReconstructPath(long currentID)
        {
            List<long> path = new List<long>() { currentID };
            while (cameFrom.TryGetValue(currentID, out long prevID))
            {
                currentID = prevID;
                path.Add(currentID);
            }

            path.Reverse();
            return path;
        }
    }
}