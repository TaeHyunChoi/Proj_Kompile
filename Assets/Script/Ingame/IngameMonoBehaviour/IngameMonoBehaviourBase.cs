using System.Collections.Generic;
using Script.Interface;
using Script.Manager;
using UnityEngine;

public class IngameMonoBehaviourBase : MonoBehaviour
{
    protected List<int> asset_hash_codes;

    protected virtual void OnEnable()
    {
        if (this is IIngameUpdater updater)
        {
            IngameUpdater.AddUpdater(updater);
#if UNITY_EDITOR
            Debug.Log($"[IngameMonoBehaviourBase] Add Updater({updater.GetType().Name})");
#endif
        }
        if (this is IIngameFixedUpdater fixedUpdater)
        {
            IngameUpdater.AddFixedUpdater(fixedUpdater);
        }
        if (this is IIngameLateUpdater lateUpdater)
        {
            IngameUpdater.AddLateUpdater(lateUpdater);
        }
        if (this is IInputReceiver inputReceiver)
        {
            InputHandler.AddInputReceiver(inputReceiver);
        }
    }
    protected virtual void OnDisable()
    {
        // 얘네도 비동기로 여차저차 처리하는 게 가능할 것 같기도 한데...

        if (this is IIngameUpdater updater)
        {
            IngameUpdater.RemoveUpdater(updater);
        }
        if (this is IIngameFixedUpdater fixedUpdater)
        {
            IngameUpdater.RemoveFixedUpdater(fixedUpdater);
        }
        if (this is IIngameLateUpdater lateUpdater)
        {
            IngameUpdater.RemoveLateUpdater(lateUpdater);
        }
        if (this is IInputReceiver inputReceiver)
        {
            InputHandler.RemoveInputReceiver(inputReceiver);
        }

        if (null != asset_hash_codes)
        {
            for (int i = 0; i < asset_hash_codes.Count; ++i)
            {
                AssetManager.Dispose(asset_hash_codes[i]);
            }

            asset_hash_codes = null;
        }
    }
}
