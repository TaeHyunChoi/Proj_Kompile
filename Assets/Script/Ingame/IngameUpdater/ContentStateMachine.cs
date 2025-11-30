using Script.Interface;
using Script.Manager;

namespace Script.Content
{
    public class ContentStateMachine
    {
        private IContentState currentState;
        private bool isTransitioning = false;

        public async void ChangeState(IContentState newState)
        {
            if (true == isTransitioning)
            {
                return;
            }
            isTransitioning = true;

            // exit before state
            if (null != currentState)
            {
                currentState.Exit();
            }

            // enter new state async
            currentState = newState;
            if (null != currentState)
            {
                await currentState.EnterAync();
            }

            if (newState is IContentUpdater newUpdater)
            {
                IngameUpdateManager.Register(newUpdater);
            }

            isTransitioning = false;
        }

        ~ContentStateMachine()
        {
            currentState.Exit();
        }
    }
}
