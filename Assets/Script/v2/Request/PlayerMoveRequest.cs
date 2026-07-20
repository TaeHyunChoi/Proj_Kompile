using UnityEngine;

namespace Kompile.Data
{
    public class PlayerMoveRequest : RequestBase
    {
        public Vector3 InputVector;
        public PlayerMoveRequest()
        {
            Type = RequestType.PlayerMove;
        }
        public override void Clear()
        {
            InputVector = Vector3.zero;
        }
    }
}
