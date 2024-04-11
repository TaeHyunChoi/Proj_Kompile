using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DataType;
using System;

public class OnField : ContentBase, IGetInput
{
    private Dictionary<int, Tile_t> tileMap;
    private MapTileComponent[] tiles;

    public static async Task<OnField> InitAsync(Transform level, MapData data)
    {
        OnField field = new OnField(level, data);

        Task task = Main.UnitMgr.InitAsync(level);
        await task;
        task.Dispose();

        Main.Instance.SetContent(field);
        Main.Instance.SetInputGetter(field);

        return field;
    }

    public void Input(int input)
    {
        Vector3 dir = PTile.GetDirection(input);
        Main.Player.Move(tileMap, dir);
    }
    public override void Dispose()
    {

    }

    private OnField(Transform level, MapData data)
    {
        tileMap = DataTable.LoadMappingData<Tile_t>("020_FieldTest");

        foreach (int key in tileMap.Keys)
        {
            Tile_t tile = tileMap[key];
            Debug.Log($"[{PTile.GetPivot(key, tile.GetScale())}] {System.Convert.ToString(tile.Move, 2)}");
        }

        //Task.Run()을 고려했으나 .SetActive()가 Unity API라서 사용 불가
        tiles = level.GetComponentsInChildren<MapTileComponent>();
        for (int i = 0; i < tiles.Length; ++i)
        {
            if (0 != tiles[i].Layer)
            {
                tiles[i].gameObject.SetActive(false);
            }
        }
    }
}
