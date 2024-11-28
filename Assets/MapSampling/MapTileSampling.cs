#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using DataStruct;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Diagnostics;

namespace MapSampling
{
    /// <summary> reference link: https://www.youtube.com/watch?v=K-zw3QFaTqg
    /// </summary>
    public class MapTileSampling : MonoBehaviour
    {
        public static readonly object Locker = new();
        private Dictionary<long, MapTileData> _dataDic    = new();
        private Dictionary<long, List<MeshFilter>> _meshDic = new();
        
        private readonly string _assetGroupName = "MapMesh";
        private readonly string _assetLabelName = "MapNavMesh";
        
        public async void Save()
        {
            // set data
            var tiles = FindObjectsOfType<MapTile>();
            var initTasks = new Task[tiles.Length];

            for (var i = 0; i < tiles.Length; i++)
            {
                var t = i;
                initTasks[t] = tiles[t].Init(_dataDic, _meshDic);
            }
            await Task.WhenAll(initTasks);      
            
            //debug.log
            foreach (var tile in _dataDic)
            {
                var key = tile.Key;
                var mapTile = tile.Value;
                Debug.Log(UtilMap.UtilMapTile.GetTilePivot(mapTile.IndexFlag));
            }
            
            // save data
            DataTable.WriteBinaryMappingData<MapTileData>(_dataDic, "MapTileData");
            
            
            // combine mesh : 이것도 비동기로 할 수 있겠는데?
            // foreach (var key in _meshDic.Keys)
            // {
            //     var meshFilters = _meshDic[key];
            //     var combine = new CombineInstance[meshFilters.Count];
            //     for (var i = 0; i < meshFilters.Count; i++)
            //     {
            //         combine[i].mesh = meshFilters[i].sharedMesh;
            //         combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            //     }
            //     
            //     var mesh = new Mesh();
            //     mesh.CombineMeshes(combine);
            //     SaveMesh(mesh, $"NavMesh_{key}", false, true);
            // }
            
            
            // dispose refs
            for (var i = 0; i < tiles.Length; i++)
            {
                var t = i;
                initTasks[t].Dispose();
            }
            
            Debug.Log("모든 Temp 오브젝트의 Init 호출이 병렬로 완료되었습니다.");
        }
        
        private void SaveMesh(Mesh mesh, string assetName, bool makeNewInstance, bool optimizeMesh)
        {
            var path = "Assets/Rcs/MapNav/" + assetName + ".asset";
            
            // 이미 같은 이름의 에셋이 있는지 확인합니다.
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) is not null)
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
            var group = settings.FindGroup(_assetGroupName);

            if (group is not null)
            {
                // Addressable 에셋 생성
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                entry.SetAddress(assetName);
                entry.labels.Add(_assetLabelName);
                
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
    }
}
#endif