namespace Kompile
{
    using Domain;
    using Data;
    using UnityEngine;
    using Unity.Mathematics;

    public class MapUpdateRequest : RequestBase
    {
        private float3 _position;
        public float3 Position => _position;

        public static MapUpdateRequest Create(Vector3 pos)
        {
            var request = RequestProvider<MapUpdateRequest>.Get();

            request.Type = RequestType.Map_Update;
            request._position = new float3(pos.x, pos.y, pos.z);

            return request;
        }
        public override void Clear()
        {
            _position = default;
        }

        public override void ReturnToPool()
        {
            RequestProvider<MapUpdateRequest>.Return(this);
        }
    }
}
