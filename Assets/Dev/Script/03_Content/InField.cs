using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

internal class InField : ISequenceUpdater
{
    private GameObject gameObject;
    private Transform transform;

    private static Dictionary<int, byte> voxel;
    private static Unit player;

    public InField(GameObject obj)
    {
        gameObject = obj;
        transform  = obj.transform;

        voxel = DataTable.LoadMapVoxel(obj.name);
    }
    public async Task<bool> InitMap()
    {
        try
        {
            GameObject obj = await AssetManager.InstantiateAsync("UnitBase", Main.GameMgr.GetTransform(), true);
            player = obj.AddComponent<Unit>();

            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            camFollow.SetFollow(player.transform);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }

        return true;
    }

    public void Start()
    {
        
    }
    public void Update()
    {
        
    }
    public void Close()
    {

    }

    public static void Input(int input)
    {
        Vector3 dir = new Vector3();

        //interact
        if (IDxInput.Compare(input, IDxInput.ENTER, IDxInput.ENTER_HOLD))
        {

        }

        //move
        if (IDxInput.Compare(input, IDxInput.UP, IDxInput.UP_HOLD))
        {
            dir += Vector3.forward;
        }
        if (IDxInput.Compare(input, IDxInput.DOWN, IDxInput.DOWN_HOLD))
        {
            dir += Vector3.back;
        }
        if (IDxInput.Compare(input, IDxInput.LEFT, IDxInput.LEFT_HOLD))
        {
            dir += Vector3.left;
        }
        if (IDxInput.Compare(input, IDxInput.RIGHT, IDxInput.RIGHT_HOLD))
        {
            dir += Vector3.right;
        }

        if (dir != Vector3.zero)
        {
            player.Move(voxel, dir.normalized);
        }
    }    
}
