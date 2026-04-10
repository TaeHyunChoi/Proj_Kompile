using UnityEngine;

namespace Script.Global.Asset.Provider
{
    using Script.Global.Asset.Data;
    
    /// <summary> 유닛 기획 데이터를 보관하는 Provider; (Value-Centric을 지향) </summary>
    public static class UnitTableProvider
    {
        private static UnitTableData[] _tableData;

        public static void Initialize()
        {
            //AssetProvider에 뭐가 있을걸?...
            //여기서 테이블 여차저차 해야 하는구나? 코드 다 날렸나?
        }

        public static ref readonly UnitTableData GetUnitData(int unitID)
        {
            int left = 0;
            int right = _tableData.Length;

            while (left < right)
            {
                int mid = left + Mathf.FloorToInt((right - left) * 0.5f);
                if (unitID == _tableData[mid].UnitID)
                {
                    return ref _tableData[mid];
                }

                if (unitID < _tableData[mid].UnitID)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            throw new System.Exception("데이터 없음;");
        }
    }
}