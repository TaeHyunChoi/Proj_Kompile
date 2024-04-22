using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DataType;
using System;

public class OnField : ContentBase, IFixedInputHandler
{
    private Dictionary<int, Tile_t> tileMap;
    private MapTileComponent[] tiles;
    private Transform level;

    public static async Task<OnField> InitAsync(Transform level, MapData data)
    {
        OnField field = new OnField(level, data);

        Task task = Main.UnitMgr.InitAsync(level);
        await task;
        task.Dispose();

        Main.Instance.SetContent(field);
        return field;
    }

    //현재 필드에서 입력을 받으면 Player를 이동시킨다. (플레이어 이동이므로 I"Fixed"InputHandler 받음)
    public void Input(Index.IDxInput.EInput input)
    {
        Vector3 dir = TileUtility.GetDirection(input);
        Main.Player.Move(tileMap, dir);
    }
    private OnField(Transform level, MapData data)
    {
        tileMap = DataTable.LoadMappingData<Tile_t>("020_FieldTest");
        this.level = level;

        tiles = level.GetComponentsInChildren<MapTileComponent>(true);
        MapTileComponent tile;
        for (int i = 0; i < tiles.Length; ++i)
        {
            tile = tiles[i];
            tile.gameObject.SetActive(0 == tile.Layer);
        }
    }

    //OnField.cs
    public void TransLayer(int layer)
    {
        //Task.Run(), Job System 등을 고려했으나 Unity API를 사용하므로 기각..
        tiles = level.GetComponentsInChildren<MapTileComponent>(true);

        MapTileComponent tile;
        for (int i = 0; i < tiles.Length; ++i)
        {
            tile = tiles[i];
            if (layer == tile.Layer)
            {
                TransMapTile trans = new TransMapTile(tile);
                CoroutineUpdater.SetHandler(new CCoroutine<TransMapTile>(trans));
            }
            else
            {
                tile.gameObject.SetActive(false);
            }
        }
    }
    public override void Dispose()
    {

    }
}
