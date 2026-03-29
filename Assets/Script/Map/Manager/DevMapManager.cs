namespace Script.Map.Manager
{
    using Script.Map.Data;
    using Script.Map.Provider;
    using Script.Asset.Provider;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// [Framework] Manager: 인게임에서 맵의 논리 데이터와 시각 데이터 로딩을 총괄하고 흐름을 제어합니다.
    /// </summary>
    public class MapManager
    {
        [SerializeField] private Transform mapRoot;

        private DevMapRenderProvider _renderProvider;

        // 인게임에서 사용할 길찾기/충돌용 논리 데이터 보관소
        private Dictionary<int, MapGridData> _activeGridData = new Dictionary<int, MapGridData>();

        private void Awake()
        {
            _renderProvider = new DevMapRenderProvider(mapRoot);
        }

        private async void Start()
        {
            // 예시: 게임이 시작되면 0번 그리드(구역)를 로드합니다.
            await LoadGridAsync(0);
        }

        /// <summary>
        /// 특정 그리드(구역)의 맵 데이터를 로드하고 화면에 그립니다.
        /// </summary>
        public async Task LoadGridAsync(int gridKey)
        {
            // ==========================================================
            // 1단계: 논리 데이터(바이너리) 로드
            // ==========================================================
            string naviFileName = $"MapNavi_{gridKey}";

            // AssetRepoProvider를 통해 MapNavi_0.bin 파일을 읽어와 MapGridData 객체로 변환합니다.
            MapGridData gridData = await AssetRepoProvider.LoadBinaryDataAsync<MapGridData>(naviFileName);

            if (gridData == null)
            {
                Debug.LogError($"[Framework] {naviFileName} 데이터를 찾을 수 없습니다.");
                return;
            }

            _activeGridData[gridKey] = gridData;
            Debug.Log($"[Framework] 논리 데이터 로드 완료. (그리드: {gridKey}, 타일 개수: {gridData.NaviTileDict.Count})");

            // ==========================================================
            // 2단계: 논리 데이터에 적힌 이름(meshAssetName)으로 메쉬 로드
            // ==========================================================
            // gridData.layerMeshAssets 안에 Bake할 때 저장해둔 메쉬 이름들이 들어있습니다!
            // (예: "MapRender_0_G0_L0_Town_Field_0", "MapRender_0_G0_L1_Town_Town_0" 등)

            List<Task> renderTasks = new List<Task>();

            foreach (var kvp in gridData.layerMeshAssets)
            {
                // kvp.Value는 해당 레이어에 속한 메쉬 이름들의 리스트입니다.
                foreach (string meshAssetName in kvp.Value)
                {
                    // 방금 전 우리가 만들었던 RenderProvider에 이름을 넘겨줍니다!
                    renderTasks.Add(_renderProvider.LoadMapChunkAsync(meshAssetName));
                }
            }

            // 모든 메쉬와 머티리얼이 비동기로 로드되고 조립될 때까지 기다립니다.
            await Task.WhenAll(renderTasks);

            Debug.Log($"[Framework] 시각 데이터(메쉬/머티리얼) 조립 완료!");
        }
    }
}