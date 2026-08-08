namespace Kompile.Manager
{
    using Data;

    public static class InGameLogic
    {
        public static void Progress(RequestBase req)
        {
            switch (req.Type)
            {
                case RequestType.Actor_Update:
                    InGame.Actor.UpdateInField();
                    break;
                default:
                    break;
            }
        }
    }
}
