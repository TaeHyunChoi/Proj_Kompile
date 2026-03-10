namespace Script.GamePlay
{
    using static Script.Input.Data.Definition;
    
    public interface IInputReceiver
    {
        bool OnInputReceive(InputState inputState);
    }
}