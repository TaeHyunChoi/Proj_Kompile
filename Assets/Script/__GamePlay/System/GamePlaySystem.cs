namespace Script.GamePlay
{
    using Script.Global.Provider;

    public class GamePlaySystem
    {
        public readonly IngameInputProvider Input;


        public GamePlaySystem()
        {
            Input = new IngameInputProvider();
        }
    }
}