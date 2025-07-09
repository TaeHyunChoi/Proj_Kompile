using Script.Index;

namespace Script.Interface
{
    public interface IMessageReceiver
    {
        public bool ReceiveIngameMessage<T>(T data) where T : struct;
    }

    public interface IInputReceiver
    {
        public bool ReceiveInput(IDxInput.InputFlag flag);
    }
}