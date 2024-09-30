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
        var tileSample = transform.GetComponentsInChildren<x_MapTileSampler>();
        for (int i = 0; i < tileSample.Length; ++i)
        {
            tileSample[i].Set();
        }
    }
}
