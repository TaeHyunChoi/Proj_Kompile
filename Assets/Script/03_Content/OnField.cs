using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DataType;

public class OnField : ContentBase, IGetInput
{
    private Dictionary<int, Tile_t> tileMap;
    private float tileScale;

    public static async Task<OnField> InitAsync(Transform level, MapData data)
    {
        OnField field = new OnField(data);

        Task task = Main.UnitMgr.InitAsync(level);
        await task;
        task.Dispose();

        //Task.Run()을 고려했으나 .SetActive()가 Unity API라서 사용 불가
        MapTileComponent[] tiles = level.GetComponentsInChildren<MapTileComponent>();
        for (int i = 0; i < tiles.Length; ++i)
        {
            if (0 != tiles[i].Layer)
            {
                tiles[i].gameObject.SetActive(false);
            }
        }

        Main.Instance.SetContent(field);
        Main.Instance.SetInputGetter(field);

        return field;
    }

    public void Input(int input)
    {
        Vector3 dir = PTile.GetDirection(input);
        Main.Player.Move(tileMap, dir, tileScale);
    }
    public override void Dispose()
    {

    }
    
    private OnField(MapData data)
    {
        tileMap = DataTable.LoadMappingData<Tile_t>("020_FieldTest");
        tileScale = PTile.SIZE;
    }
}
