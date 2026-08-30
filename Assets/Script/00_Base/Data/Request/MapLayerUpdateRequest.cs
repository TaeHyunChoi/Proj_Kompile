namespace Kompile
{
    using Domain;
    using Data;
    using UnityEngine;
    using Unity.Mathematics;

    public class MapLayerUpdateRequest : RequestBase
    {
        private float3 _position;
        public float3 Position => _position;

        public static MapLayerUpdateRequest Create(Vector3 pos)
        {
            var request = RequestProvider<MapLayerUpdateRequest>.Get();

            request.Type = RequestType.MapLayerUpdate;
            request._position = new float3(pos.x, pos.y, pos.z);

            return request;
        }
        public override void Clear()
        {
            _position = default;
        }

        public override void ReturnToPool()
        {
            RequestProvider<MapLayerUpdateRequest>.Return(this);
        }
    }
}
