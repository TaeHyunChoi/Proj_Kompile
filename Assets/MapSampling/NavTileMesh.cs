using System;
using UnityEngine;

[Serializable]
public class NavTileMesh : MonoBehaviour
{
    [SerializeField]
    private long heightFlag;

    public void Initialize(int[] heights)
    {
        var i = 0;
        foreach (long height in heights)
        {
            var h = height;
            if (-1 == height)
            {
                h = 0b1111;
            }

            heightFlag |= h << i;
            i += 4;
        }
        
        Debug.Log("Initialized: " + Convert.ToString((long)heightFlag,2));
    }
}
