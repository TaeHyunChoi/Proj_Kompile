namespace Kompile.Data
{
    public abstract class RequestBase
    {
        public RequestType Type { get; protected set; }
        internal bool IsPooled { get; set; }
        public abstract void Clear();
    }
}
