namespace Script.Interface
{
    using Script.Index;

    public interface IContentTaskUpdater
    {
        public ContentTaskState MoveNext(IDxInput.EInputFlag inputFlag);
    }
}
