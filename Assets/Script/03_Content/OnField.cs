using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DataStruct;

public class OnField : ContentBase, IFixedInputHandler
{
    private Dictionary<int, STile> mTileMap;
    private MapTileComponent[]     mTileComponents;
    private Transform              mTransformLevel;

    public static async Task<OnField> InitAsync(Transform level, MapData data)
    {
        OnField field = new OnField(level, data);

        Task taskInitField = Main.UnitMgr.InitAsync(level);
        await taskInitField;
        taskInitField.Dispose();

        Main.Instance.SetContent(field);
        return field;
    }

    public void Input(Index.IDxInput.EInput input)
    {
        if (Index.IDxInput.EInput.NONE == input)
        {
            Main.Player.StopMove();
        }
        else
        {
            Vector3 dir = InputMgr.GetInputDirection(input);
            Main.Player.Move(mTileMap, dir);
        }
    }
    private OnField(Transform level, MapData data)
    {
        mTileMap = DataTable.LoadMappingData<STile>("020_FieldTest");
        UnityEngine.Assertions.Assert.IsNotNull(mTileMap, "Null time map");
        this.mTransformLevel = level;

        mTileComponents = level.GetComponentsInChildren<MapTileComponent>(true);
        MapTileComponent tile;
        for (int i = 0; i < mTileComponents.Length; ++i)
        {
            tile = mTileComponents[i];
            tile.gameObject.SetActive(0 == tile.Layer);
        }
    }

    //OnField.cs
    public void TransLayer(int layer)
    {
        mTileComponents = mTransformLevel.GetComponentsInChildren<MapTileComponent>(true);

        MapTileComponent tile;
        for (int i = 0; i < mTileComponents.Length; ++i)
        {
            tile = mTileComponents[i];
            if (tile.Layer == layer)
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
