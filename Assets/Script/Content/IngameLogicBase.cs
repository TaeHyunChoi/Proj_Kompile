namespace Script.Content
{
    using Script.Index;

    public abstract class IngameLogicBase
    {
        protected IngameLogicIndex ingameLogicType;
        public IngameLogicIndex IngameLogicType => ingameLogicType;
        public abstract IngameState MoveNext();
    }
}
