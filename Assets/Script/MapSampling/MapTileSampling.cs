#if UNITY_EDITOR
namespace MapSampling
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEditor;
    using Script.Util;
    using Script.Data;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using System.Collections;

    /// <summary> reference link: https://www.youtube.com/watch?v=K-zw3QFaTqg
    /// </summary>
    public class MapTileSampling : MonoBehaviour
    {
        private readonly string assetGroupName = "MapRender";
        private readonly string assetLabelName = "MapNavMesh";

        [SerializeField] private Transform instanceTransform;
        private ConcurrentDictionary<int, RawMapGridData> map;

        public async void Save()
        {
            // set data
            EditMapNavData[] tiles = instanceTransform.GetComponentsInChildren<EditMapNavData>();
            if (0 == tiles.Length)
            {
                Debug.LogWarning("NavTileMesh.Length = 0;");
                return;
            }

            // async : nav data
            Task taskSaveNavData = SaveMapNavDataAsync(tiles);

            // sync : render data (unity api 사용하므로 async 불가)
            StartCoroutine(IESaveRender(tiles));

            await Task.WhenAll(taskSaveNavData);
            taskSaveNavData.Dispose();

            AssetDatabase.Refresh();
            Debug.Log("모든 Temp 오브젝트의 Init 호출이 병렬로 완료되었습니다.");
        }

        public async Task SaveMapNavDataAsync(EditMapNavData[] tiles)
        {
            map = new ConcurrentDictionary<int, RawMapGridData>();
            int length = tiles.Length;
            int i, t;

            // bake + dispose
            Task[] initTasks = new Task[length];
            for (i = 0; i < length; i++)
            {
                t = i;
                initTasks[t] = tiles[t].BakeMesh(map);
            }
            await Task.WhenAll(initTasks);
            for (i = 0; i < length; i++)
            {
                t = i;
                initTasks[t].Dispose();
            }

            // save data
            foreach (var grid in map)
            {
                DataMgr.WriteBinaryMappingData<RawMapGridData>(grid.Value, $"MapGrid_{grid.Key}");
            }
        }

        private IEnumerator IESaveRender(EditMapNavData[] tiles)
        {
            ConcurrentDictionary<int, List<MeshFilter>> temp = new ConcurrentDictionary<int, List<MeshFilter>>();
            foreach (var tile in tiles)
            {
                MeshFilter meshFilter = tile.MeshFilter;

                int layer = tile.Layer;
                if (false == temp.ContainsKey(layer))
                {
                    temp.TryAdd(layer, new List<MeshFilter>());
                }

                if (false == temp[layer].Contains(meshFilter))
                {
                    temp[layer].Add(meshFilter);
                }
                yield return null;
            }

            foreach (var layer in temp.Keys)
            {
                var list = temp[layer];
                var count = list.Count;
                var combine = new CombineInstance[count];
                for (int m = 0; m < count; ++m)
                {
                    combine[m].mesh = list[m].sharedMesh;
                    combine[m].transform = list[m].transform.localToWorldMatrix;
                }
                Mesh combinedMesh = new Mesh();
                combinedMesh.CombineMeshes(combine);
                SaveMesh(combinedMesh, $"test_layer_{layer}", true, true);
                yield return null;
            }
        }
        private void SaveMesh(Mesh mesh, string assetName, bool makeNewInstance, bool optimizeMesh)
        {
            var path = "Assets/Rcs/MapRender/" + assetName + ".asset";

            // 이미 같은 이름의 에셋이 있는지 확인합니다.
            if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            var meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
            if (optimizeMesh)
            {
                MeshUtility.Optimize(meshToSave);
            }

            AssetDatabase.CreateAsset(meshToSave, path);

            // Addressable Assets에 등록
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(assetGroupName);

            if (group is not null)
            {
                // Addressable 에셋 생성
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                entry.SetAddress(assetName);
                entry.labels.Add(assetLabelName);

                EditorUtility.SetDirty(settings);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }
            else
            {
                Debug.LogError("Addressable Asset Group not found.");
                return;
            }

            AssetDatabase.SaveAssets();
        }

        //public void Load()
        //{
        //    MapGridData data = DataMgr.ReadBinaryMappingData<MapGridData>("MapGrid_0");
        //}
    }
}
#endif