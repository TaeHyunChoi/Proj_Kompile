namespace Kompile.Manager
{
    public partial class InGame // ManagerCache.cs
    {
        private static class ManagerCache<T> where T : GameLogicMgrBase
        {
            public static T Instance;
        }
    }
}
