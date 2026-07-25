namespace Kompile.Manager
{
    using System.Collections.Generic;
    using Unity.Jobs;
    using Unity.Collections;
    using Unity.Mathematics;
    using Kompile.Component;
    using Kompile.Data;

    /// <summary>
    /// [Framework] Manager 계층
    /// FieldManager의 지시를 받아 등록된 모든 유닛의 이동 Job을 스케줄링합니다.
    /// </summary>
    public class x_UnitMoveManager
    {
        // Manager(Instance-Centric): 인스턴스 제어
        private readonly HashSet<UnitMoveComponent> _activeUnits = new HashSet<UnitMoveComponent>();
        private readonly List<UnitMoveComponent> _unitCacheList = new List<UnitMoveComponent>(); // 빠른 순회를 위한 캐시

        public void Register(UnitMoveComponent unit)
        {
            if (_activeUnits.Add(unit))
            {
                _unitCacheList.Add(unit);
            }
        }

        public void Unregister(UnitMoveComponent unit)
        {
            if (_activeUnits.Remove(unit))
            {
                _unitCacheList.Remove(unit);
            }
        }

        // 💡 [에러 해결] 매개변수 타입을 (int2, MapTileData)에서 (long, BurstTileInfo)로 교정하여 파이프라인을 연결합니다.
        public void ExecuteMoveJobs(float deltaTime, NativeHashMap<long, BurstTileInfo> nativeTileMap)
        {
            int unitCount = _unitCacheList.Count;
            if (unitCount == 0) return;

            // 1. 네이티브 배열 할당 (Allocator.TempJob 사용으로 1프레임 내 자동 관리 지향)
            NativeArray<float2> inputs = new NativeArray<float2>(unitCount, Allocator.TempJob);
            NativeArray<float3> positions = new NativeArray<float3>(unitCount, Allocator.TempJob);
            NativeArray<float3> results = new NativeArray<float3>(unitCount, Allocator.TempJob);

            for (int i = 0; i < unitCount; i++)
            {
                inputs[i] = _unitCacheList[i].CurrentInput;
                positions[i] = _unitCacheList[i].transform.position;
            }

            // 2. Job 구성
            UnitMoveJob moveJob = new UnitMoveJob
            {
                MoveInputs = inputs,
                CurrentPositions = positions,
                NextPositions = results,
                DeltaTime = deltaTime,
                TileMap = nativeTileMap // 💡 MapManager -> FieldManager를 거쳐 온 3D 패킹 네이티브 데이터 주입
            };

            // 3. 스케줄링 및 완료 대기
            JobHandle handle = moveJob.Schedule(unitCount, 64);
            handle.Complete();

            // 4. Transform 위치 동기화
            for (int i = 0; i < unitCount; i++)
            {
                _unitCacheList[i].transform.position = results[i];
            }

            // 5. 메모리 해제
            inputs.Dispose();
            positions.Dispose();
            results.Dispose();
        }

        public void Dispose()
        {
            _activeUnits.Clear();
            _unitCacheList.Clear();
        }
    }
}