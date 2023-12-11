using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    public abstract void Close();
    public abstract void Input(int input);

    public int GetID()
    {
        return gameObject.GetInstanceID();
    }
}
