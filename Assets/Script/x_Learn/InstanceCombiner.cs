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
        Dictionary<long, MapTileSampler> data = new Dictionary<long, MapTileSampler>();
        Dictionary<short, List<MeshFilter>> gridMeshFilter = new Dictionary<short, List<MeshFilter>>();
        //Dictionary<long, DataStruct.MapTileData> table = new Dictionary<long, DataStruct.MapTileData>();

        var samples = transform.GetComponentsInChildren<MapTileSampler>();
        MapTileSampler sample;
        for (int i = 0; i < samples.Length; ++i)
        {
            sample = samples[i];
            samples[i].Init();

            long index = (long)(sample.GridIndexFlag) << 16 | (long)sample.IndexFlag;
            if (false == data.ContainsKey(index))
            {
                data.Add(index, sample);
            }
            else
            {
                data[index] |= sample;
            }
        }



        foreach (MapTileSampler sampler in data.Values)
        {
            var gridIndexFlag = sampler.GridIndexFlag;
            if (false == gridMeshFilter.ContainsKey(sampler.GridIndexFlag))
            {
                gridMeshFilter.Add(gridIndexFlag, new List<MeshFilter>());
            }

            gridMeshFilter[gridIndexFlag].Add(sampler.MeshFilter);
        }

        foreach (var meshFilters in gridMeshFilter.Values)
        {
            var combine = new CombineInstance[meshFilters.Count];
            for (int i = 0; i < meshFilters.Count; ++i)
            {
                combine[i].mesh = meshFilters[i].mesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            var mesh = new Mesh();
            mesh.CombineMeshes(combine);

            //grid 별로 위에서 합친 mesh를 저장하여 파일로 만들면 된다ㅠㅠㅠ

            // for test
            //GameObject obj = new GameObject("temp", typeof(MeshFilter), typeof(MeshRenderer));
            //obj.transform.position = Vector3.zero;
            //obj.transform.GetComponent<MeshFilter>().mesh = mesh;
            //obj.transform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Sprites-Default");
        }

        gameObject.SetActive(false);
    }
}
