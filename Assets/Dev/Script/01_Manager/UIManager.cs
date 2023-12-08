using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private static UIManager instance;

    public static UIManager Instance { get => instance; }


    public UIManager()
    {
        if (instance != null)
            return;

        instance = this;

        //각종 UI를 어찌 처리하면 좋을까?
        //Hmmmmm
    }

    public int Update(int input)
    {

        return input;
    }
}