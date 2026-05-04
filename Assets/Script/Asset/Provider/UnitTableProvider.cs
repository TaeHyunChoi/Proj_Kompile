namespace Kompile.Asset.Provider
{
    using Kompile.Asset.Data;
    using UnityEngine;
    
    /// <summary> 유닛 기획 데이터를 보관하는 Provider; (Value-Centric을 지향) </summary>
    public static class UnitTableProvider
    {
        private static UnitTableData[] _sheets;

        public static UnitTableData[] Sheets => _sheets;
        
        public static async Awaitable InitializeAsync()
        {
            _sheets = await AssetProvider.ReadBinaryDataAsync<UnitTableData[]>(AssetConst.UNIT_TABLE);
            
            if (null == _sheets
                || 0 == _sheets.Length)
            {
                Debug.LogError("[UnitTableProvider] 데이터 로드 실패!");
            }
        }

        public static ref readonly UnitTableData GetUnitData(int unitID)
        {
            int left = 0;
            int right = _sheets.Length - 1; // 길이-1이 안전한 초기 인덱스입니다.

            while (left <= right) // 조건 교정: <= 로 해야 마지막 요소까지 탐색합니다.
            {
                int mid = left + (right - left) / 2; // 오버플로우 방지형 중간값 계산

                if (unitID == _sheets[mid].ID)
                {
                    return ref _sheets[mid];
                }

                // 오름차순 이진 탐색의 올바른 분기
                if (unitID < _sheets[mid].ID)
                {
                    right = mid - 1; // 타겟이 작으면 왼쪽 절반을 탐색
                }
                else
                {
                    left = mid + 1;  // 타겟이 크면 오른쪽 절반을 탐색
                }
            }

            throw new System.Exception($"[UnitTableProvider] 데이터 없음; ID: {unitID}");
        }
    }
}