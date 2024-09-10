using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class String_
{
    public static string GetSceneName(EScene scene)
    {
        switch (scene)
        {
            case EScene.Opening: return "010_OpeningScene";
            default: break;
        }

        return null;
    }
}
