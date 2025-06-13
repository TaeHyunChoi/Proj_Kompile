namespace Script.Interface
{
    public interface IMessageReceiver
    {
        public bool ReceiveIngameMessage<T>(IngameEventType type, T data) where T : struct;
    }
}