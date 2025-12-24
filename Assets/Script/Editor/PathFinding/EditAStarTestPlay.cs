using Script.Map;
using Unity.Mathematics;
using UnityEngine;

public class EditAStarTestPlay : MonoBehaviour
{
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;

    MapCacheManager cacheMgr;

    public async void Play()
    {
        if (null == cacheMgr)
        {
            cacheMgr = new MapCacheManager();
        }
        await cacheMgr.EditLoadAll();

        Vector3  startPos = startTransform.position;
        Vector3  endPos   = endTransform.position;
        float3[] path     = await AStarPathfinder.RequestPath(startPos, endPos, cacheMgr.TileDic);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < path.Length; ++i)
        {
            sb.Append($"{path[i]} -> ");
        }
        sb.Append("[GOAL]");

        Debug.Log(sb.ToString());
    }
}
