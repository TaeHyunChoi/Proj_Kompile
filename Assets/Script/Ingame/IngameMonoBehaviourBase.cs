using Script.Interface;
using Script.Manager;
using UnityEngine;

public class IngameMonoBehaviourBase : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        if (this is IIngameUpdater updater)
        {
            IngameUpdater.AddUpdater(updater);
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
            InputManager.AddInputReceiver(inputReceiver);
        }
    }
    protected virtual void OnDisable()
    {
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
            InputManager.RemoveInputReceiver(inputReceiver);
        }
    }
}
