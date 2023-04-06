using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceMgr
{
    public static Dictionary<string, GameObject> Prefab { get; private set; }
    public static AnimatorOverrideController AOC { get; private set; }
    public static Dictionary<string, AnimationClip> Anime { get; private set; }
    public static Dictionary<string, Sprite> SPIcon { get; private set; }

    public static void LoadAssetFromRcs()
    {
        Prefab = new Dictionary<string, GameObject>();
        GameObject[] go = Resources.LoadAll<GameObject>("Prefab");
        for (int i = 0; i < go.Length; ++i)
            Prefab.Add(go[i].name, go[i]);


        AOC = Resources.Load("Animation/AOC/UnitAOC") as AnimatorOverrideController;


        Anime = new Dictionary<string, AnimationClip>();
        AnimationClip[] clips;
        foreach (var unit in DataMgr.UnitTBL)
        {
            clips = Resources.LoadAll<AnimationClip>("Animation/" + unit.RcsCode);
            foreach (var clip in clips)
                Anime.Add(unit.RcsCode + "/" + clip.name, clip);
        }


        SPIcon = new Dictionary<string, Sprite>();
        Sprite[] menu = Resources.LoadAll<Sprite>("Sprite/Icon");
        for (int i = 0; i < menu.Length; ++i)
            SPIcon.Add(menu[i].name, menu[i]);
    }
}
