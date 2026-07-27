namespace Kompile.Manager
{
    public partial class InGame
    {
        private static class ManagerCache<T> where T : GameLogicMgrBase
        {
            public static T Instance;
        }
    }
}
