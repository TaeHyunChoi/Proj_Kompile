using UnityEngine;

public abstract class IngameUnitBase : IngameMonoBehaviourBase
{
    public Vector3 Position => transform.position;
    protected float moveSpeed = 7.5f;
}
