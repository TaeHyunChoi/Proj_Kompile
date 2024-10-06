using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary> reference link: https://www.youtube.com/watch?v=K-zw3QFaTqg
/// </summary>
public class InstanceCombiner : MonoBehaviour
{
    [SerializeField] private List<MeshFilter> listMeshFilters;
    [SerializeField] private MeshFilter targetMesh;

    [ContextMenu("Combine Meshes")]
    private void CombineMesh()
    {
        var combine = new CombineInstance[listMeshFilters.Count];

        for (int i = 0; i < listMeshFilters.Count; ++i)
        {
            combine[i].mesh = listMeshFilters[i].sharedMesh;
            combine[i].transform = listMeshFilters[i].transform.localToWorldMatrix;
        }

        var mesh = new Mesh();
        mesh.CombineMeshes(combine);

        targetMesh.mesh = mesh;
        SaveMesh(targetMesh.sharedMesh, gameObject.name, false, true);

        print($"<color=#20E7B0>Combine Meshes was Successful!</color>");
    }
    private void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        string path = EditorUtility.SaveFilePanel("Save Seperate Mesh Asset", "Assets/", name, "asset");
        path = FileUtil.GetProjectRelativePath(path);

        Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
        {
            MeshUtility.Optimize(meshToSave);
        }

        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
    }

    //for test
    private void Start()
    {

        Dictionary<long, DataStruct.MapTileData> table = new Dictionary<long, DataStruct.MapTileData>();
        //Dictionary<short, Mesh> 계속 추가하며 쌓으면 되나본데?
        //그런데 각 grid별 tile count가 필요하구나? 흠.. 이걸 어떻게 더해야 하는지 생각을 못했네.

        var tileSample = transform.GetComponentsInChildren<MapTileSampler>();
        for (int i = 0; i < tileSample.Length; ++i)
        {
            (long, DataStruct.MapTileData) tile = tileSample[i].Set();

            var index = tile.Item1;
            var data  = tile.Item2;

            if (false == table.ContainsKey(index))
            {
                table.Add(index, data);
            }
            else
            {
                table[index] |= data;
            }
        }

        // 완료 후엔? 이렇게 저렇게 합니다. 그런데 이제 Mesh를 추가한...
        // 
    }
}
