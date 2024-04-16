using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DataType;
using System;

public class OnField : ContentBase, IGetInput, IGetFixedInput
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
        Main.Instance.SetFixedInputGetter(field);

        return field;
    }

    public void SetLayer(int layer)
    {
        for (int i = 0; i < tiles.Length; ++i)
        {
            tiles[i].gameObject.SetActive(layer == tiles[i].Layer);
        }
    }
    public void Input(int input)
    {

    }
    public void FixedInput(int input)
    {
        Vector3 dir = TileUtility.GetDirection(input);
        Main.Player.Move(tileMap, dir);
    }

    public override void Dispose()
    {

    }

    private OnField(Transform level, MapData data)
    {
        tileMap = DataTable.LoadMappingData<Tile_t>("020_FieldTest");

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
