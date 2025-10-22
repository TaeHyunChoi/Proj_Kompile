using Script.Data;
using Script.Manager;
using System.Collections.Generic;
using UnityEngine;

public class IngameMapGridObject :IngameMonoBehaviourBase
{
    private MapGridData raw_data;
    private List<(int, GameObject)> by_layer_objects;

    public async void Initialize(MapGridData data, int current_layer_index)
    {
        raw_data = data;
        transform.position = Vector3.zero;
        by_layer_objects = new List<(int, GameObject)>();

        List<GridLayerData> layer_table = data.layer_table;
        for (int i = 0; i < layer_table.Count; ++i)
        {
            int layer_index = layer_table[i].layer;
            for (int j = 0; j < layer_table[i].assets.Count; ++j)
            {
                Mesh layer_mesh = await AssetManager.GetMeshAssetAsync(layer_table[i].assets[j]);

                // 잠시 테스트로 좀 해봅시다..
                // 이렇게 하면 에셋 해제 관리를 할 수 없다. 확인 및 수정 요망
                GameObject obj = new GameObject(layer_table[i].assets[j]);
                obj.transform.parent = this.transform;
                MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
                meshFilter.mesh = layer_mesh;

                obj.SetActive(layer_index == current_layer_index);
                by_layer_objects.Add((layer_index, obj));
            }
        }
    }
}