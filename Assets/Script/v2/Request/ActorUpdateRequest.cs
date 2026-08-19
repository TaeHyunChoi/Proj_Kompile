namespace Kompile.Data
{
    using Provider;
    using System;
    using UnityEngine;

    /// <summary> 액터 생성 요청. 액터의 index를 매개변수로 전달 </summary>
    public class ActorInstantiateRequest : RequestBase
    {
        public MapProvider MapProvider { get; private set; }
        public int Index { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Quaternion { get; private set; }

        public static ActorInstantiateRequest Create(MapProvider mapProvider, int index, Vector3 position, Quaternion rotation)
        {
            var request = RequestProvider<ActorInstantiateRequest>.Get();

            request.Type        = RequestType.Actor_Instantiate;
            request.MapProvider = mapProvider;
            request.Index       = index;
            request.Position    = position;
            request.Quaternion  = rotation;
            
            return request;
        }
        public override void ReturnToPool()
        {
            RequestProvider<ActorInstantiateRequest>.Return(this);
        }
        public override void Clear()
        {
            Index       = default;
            MapProvider = null;
            Index       = default;
            Position    = default;
            Quaternion  = default;
        }
    }

    /// <summary> 필드 위에서 플레이어 액터 업데이트 요청 </summary>
    public class PlayerUpdateRequest : RequestBase
    {
        public static PlayerUpdateRequest Create()
        {
            var request = RequestProvider<PlayerUpdateRequest>.Get();
            request.Type = RequestType.Actor_PlayerUpdate;

            return request;
        }
        public override void Clear()
        {
            // do nothing;
        }
        public override void ReturnToPool()
        {
            RequestProvider<PlayerUpdateRequest>.Return(this);
        }

    }

    /// <summary> 필드 위에서의 액터 업데이트 요청 </summary>
    public class ActorUpdateRequest : RequestBase
    {
        public static ActorUpdateRequest Create()
        {
            var request = RequestProvider<ActorUpdateRequest>.Get();
            request.Type = RequestType.Actor_Update;

            return request;
        }
        public override void Clear()
        {
            // do nothing;
        }
        public override void ReturnToPool()
        {
            RequestProvider<ActorUpdateRequest>.Return(this);
        }
    }
}
