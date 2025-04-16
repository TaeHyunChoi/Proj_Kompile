namespace Script.Interface
{
    public interface IMessageReceiver
    {
        public bool Receive_IngameEvent<T>(IngameEventType type, T data) where T : struct;
    }
}