namespace Script.Util
{
    using Script.Content;
    using Script.Index;
    using System.Collections.Generic;

    public static class FuncUtil
    {
        public static bool TryGetIngameHandler(this List<_IngameHandlerBase> handlers, IngameHandlerType targetType, out _IngameHandlerBase handler)
        {
            for (int i = handlers.Count - 1; i >= 0; --i)
            {
                if (targetType == handlers[i].HandlerType)
                {
                    handler = handlers[i];
                    return true;
                }
            }

            handler = null;
            return false;
        }
    }

}