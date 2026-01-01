using Script.Interface;
using UnityEngine;

public interface x_IContentState : IContentUpdater
{
    public Awaitable EnterAync();
    public void Exit();
}
