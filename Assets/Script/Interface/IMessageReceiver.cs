namespace Script.Interface
{
    public interface IMessageReceiver
    {
        public bool ReceiveIngameMessage<T>(T data) where T : struct;
    }
}