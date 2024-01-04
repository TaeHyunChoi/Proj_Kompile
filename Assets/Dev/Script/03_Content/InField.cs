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
    public void Start()
    {
        Main.InputMgr.Set(Input);
    }
    public void Update()
    {
        //
    }
    public void Close()
    {

    }
    public static void Input(int input)
    {
        //이동 조작..
        //잠만..
        //UI는 어떻게 하기로 했더라
        //아.. InputDele 바꾸는구나?
    }    
}
