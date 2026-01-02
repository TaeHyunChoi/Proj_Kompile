using Script.Interface;
using UnityEngine;

public interface x_IContentState : x_IContentUpdater
{
    public Awaitable EnterAync();
    public void Exit();
}
