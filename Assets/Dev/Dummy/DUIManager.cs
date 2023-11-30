using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DUIManager
{
    private static DUIManager instance;

    public static DUIManager Instance { get => instance; }


    public DUIManager()
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