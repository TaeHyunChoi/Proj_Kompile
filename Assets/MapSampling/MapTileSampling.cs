using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using DataStruct;
using UnityEditor;

namespace MapSampling
{
    /// <summary> reference link: https://www.youtube.com/watch?v=K-zw3QFaTqg
    /// </summary>
    public class MapTileSampling : MonoBehaviour
    {
        public static readonly object Locker = new();
        private Dictionary<long, MapTileData> _dataDic    = new();
        private Dictionary<long, List<MeshFilter>> _meshDic = new();
        
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
            
            // save data
            
            // combine mesh
            foreach (var key in _meshDic.Keys)
            {
                var meshFilters = _meshDic[key];
                var combine = new CombineInstance[meshFilters.Count];
                for (var i = 0; i < meshFilters.Count; i++)
                {
                    combine[i].mesh = meshFilters[i].mesh;
                    combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                }
                
                var mesh = new Mesh();
                mesh.CombineMeshes(combine);
                SaveMesh(mesh, $"NavMesh_{key}", false, true);
            }
            
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
            var path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", assetName, "asset");
            path = FileUtil.GetProjectRelativePath(path);

            var meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

            if (optimizeMesh)
            {
                MeshUtility.Optimize(meshToSave);
            }

            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }
    }
}