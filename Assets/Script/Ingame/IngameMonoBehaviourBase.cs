using Script.Interface;
using Script.Manager;
using UnityEngine;

public class IngameMonoBehaviourBase : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        if (this is IIngameUpdater updater)
        {
            IngameManager.AddUpdater(updater);
        }
        if (this is IIngameFixedUpdater fixedUpdater)
        {
            IngameManager.AddFixedUpdater(fixedUpdater);
        }
        if (this is IIngameLateUpdater lateUpdater)
        {
            IngameManager.AddLateUpdater(lateUpdater);
        }
    }
    protected virtual void OnDisable()
    {
        if (this is IIngameUpdater updater)
        {
            IngameManager.RemoveUpdater(updater);
        }
        if (this is IIngameFixedUpdater fixedUpdater)
        {
            IngameManager.RemoveFixedUpdater(fixedUpdater);
        }
        if (this is IIngameLateUpdater lateUpdater)
        {
            IngameManager.RemoveLateUpdater(lateUpdater);
        }
    }
}
