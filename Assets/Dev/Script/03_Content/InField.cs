using System;
using System.Threading.Tasks;
using UnityEngine;

internal class InField : ISequenceUpdater
{
    private static InField instance;
    private GameObject gameObject;
    private Transform transform;

    public InField(GameObject go)
    {
        gameObject = go;
        transform = go.transform;
    }
    public static async Task<InField> InitAsync(string address, Transform root)
    {
        if (instance != null)
        {
            return null;
        }

        try
        {
            GameObject go = await AssetManager.InstantiateAsync(address, root, true);
            instance = new InField(go);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading assets: " + ex.Message);
            return null;
        }

        return instance;
    }
    public void Start()
    {
        Main.InputMgr.Set(Input);
    }

    public void Close()
    {

    }

    public void Update()
    {
        //필드에서 업데이트 할 게... 있나...?
    }
    public static void Input(int input)
    {

    }
    
}
