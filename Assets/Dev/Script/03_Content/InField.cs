using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

internal class InField : ISequenceUpdater
{
    private GameObject gameObject;
    private Transform transform;

    private Unit player;

    public InField(GameObject obj)
    {
        gameObject = obj;
        transform  = obj.transform;
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
        
    }    
}
