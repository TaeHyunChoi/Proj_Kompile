namespace Kompile.Provider
{
    using Kompile.Data;
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
            int right = _sheets.Length - 1;

            while (left <= right)
            {
                int mid = left + ((right - left) >> 1);

                if (unitID == _sheets[mid].ID)
                {
                    return ref _sheets[mid];
                }

                if (unitID < _sheets[mid].ID)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            throw new System.Exception($"[UnitTableProvider] 데이터 없음; ID: {unitID}");
        }
    }
}