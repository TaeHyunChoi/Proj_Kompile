using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    private int id;
    public abstract void Close();
    public abstract void Input(int input);
}
