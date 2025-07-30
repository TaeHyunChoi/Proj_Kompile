using UnityEngine;

public abstract class IngameUnitBase : IngameMonoBehaviourBase
{
    public Vector3 Position => transform.position;
}
