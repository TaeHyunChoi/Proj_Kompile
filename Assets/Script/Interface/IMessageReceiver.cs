namespace Script.Interface
{
    public interface IMessageReceiver
    {
        public bool Receive<T>(IngameMessageType type, T data) where T : struct;
    }
}