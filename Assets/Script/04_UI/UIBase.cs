using UnityEngine;

public abstract class UIBase
{
    protected Transform transform;
    protected GameObject gameObject;

    public abstract void Init(GameObject go);
    public abstract void Open();
    public abstract void Update();
    public abstract void Input(int input);
    public abstract void Close();
}
