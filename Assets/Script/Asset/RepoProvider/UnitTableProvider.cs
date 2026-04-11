using Script.Asset.Data;
using UnityEngine;
using System.Threading.Tasks;

namespace Script.Global.Asset.Provider
{
    using Script.Global.Asset.Data;
    
    /// <summary> 유닛 기획 데이터를 보관하는 Provider; (Value-Centric을 지향) </summary>
    public static class UnitTableProvider
    {
        private static UnitTableData[] Sheets;

        public static async Task InitializeAsync()
        {
            // Context 없이 배열 타입 자체로 곧바로 로드합니다.
            Sheets = await AssetProvider.LoadBinaryDataAsync<UnitTableData[]>(new AssetKey("UnitTable"));
            
            if (Sheets == null)
            {
                Debug.LogError("[UnitTableProvider] 데이터 로드 실패!");
            }
        }

        public static ref readonly UnitTableData GetUnitData(int unitID)
        {
            int left = 0;
            int right = Sheets.Length - 1; // 길이-1이 안전한 초기 인덱스입니다.

            while (left <= right) // 조건 교정: <= 로 해야 마지막 요소까지 탐색합니다.
            {
                int mid = left + (right - left) / 2; // 오버플로우 방지형 중간값 계산

                if (unitID == Sheets[mid].ID)
                {
                    return ref Sheets[mid];
                }

                // 오름차순 이진 탐색의 올바른 분기
                if (unitID < Sheets[mid].ID)
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