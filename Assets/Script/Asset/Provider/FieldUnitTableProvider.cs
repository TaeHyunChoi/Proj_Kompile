namespace Kompile.Asset.Provider
{
    using Kompile.Asset.Data;
    using UnityEngine;

    /// <summary> 필드 위의 유닛 데이터; </summary>
    public static class FieldUnitTableProvider
    {
        private static FieldUnitTableData[] _sheet;
        public static FieldUnitTableData[] Sheet => _sheet;

        public static async Awaitable InitializeAsync()
        {
            _sheet = await AssetProvider.ReadBinaryDataAsync<FieldUnitTableData[]>("FieldUnitTable");

            if (null == _sheet
                || 0 == _sheet.Length)
            {
                Debug.LogError("[UnitTableProvider] 데이터 로드 실패!");
            }
        }
        public static ref readonly FieldUnitTableData GetData(int index)
        {
            int left = 0;
            int right = _sheet.Length - 1;

            while (left <= right)
            {
                int mid = left + ((right - left) >> 1);

                if (index == _sheet[mid].Index)
                {
                    return ref _sheet[mid];
                }

                if (index < _sheet[mid].Index)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            throw new System.Exception($"[UnitTableProvider] 데이터 없음; ID: {index}");
        }
    }
}
