using Script.Interface;
using Script.Manager;

namespace Script.Content
{
    public class x_ContentStateMachine
    {
        //private x_ContentBase currentState;
        //private bool isTransitioning = false;

        //public async void ChangeState(x_ContentBase newContent)
        //{
        //    if (true == isTransitioning)
        //    {
        //        return;
        //    }
        //    isTransitioning = true;

        //    // exit before state
        //    if (null != currentState)
        //    {
        //        currentState.Exit();
        //    }

        //    // enter new state async
        //    currentState = newContent;
        //    if (null != currentState)
        //    {
        //        await currentState.EnterAync();
        //    }

        //    if (newContent is IContentUpdater newUpdater)
        //    {
        //        IngameUpdateManager.Register(newUpdater);
        //    }

        //    isTransitioning = false;
        //}

        //~x_ContentStateMachine()
        //{
        //    currentState.Exit();
        //}
    }
}
