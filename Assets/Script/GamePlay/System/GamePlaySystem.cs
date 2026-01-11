namespace Script.GamePlay
{
    using Script.GameSystem;

    public class GamePlaySystem
    {
        public readonly GameplayInputSystem Input;


        public GamePlaySystem()
        {
            Input = new GameplayInputSystem();
        }
    }
}