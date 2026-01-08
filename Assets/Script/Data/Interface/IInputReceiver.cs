namespace Script.GamePlay
{
    public interface IInputReceiver
    {
        bool OnInputReceive(Data.DataType.IDxInput inputFlag);
    }
}