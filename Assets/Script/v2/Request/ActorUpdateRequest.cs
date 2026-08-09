namespace Kompile.Data
{
    using UnityEngine;
    using Provider;

    /// <summary> 액터 생성 요청. 액터의 index를 매개변수로 전달 </summary>
    public class ActorInstantiateRequest : RequestBase
    {
        public int Index { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Quaternion { get; private set; }

        public ActorInstantiateRequest()
        {
            Type = RequestType.Actor_Instantiate;
        }
        public static ActorInstantiateRequest Create(int index, Vector3 position, Quaternion rotation)
        {
            var request = RequestProvider<ActorInstantiateRequest>.Get();

#if DEV_BUILD
            if (null == request)
            {
                InDev.LogError("Can`t get 'ActorInstantiateRequest';");
            }
#endif
            
            request.Index = index;
            request.Position = position;
            request.Quaternion = rotation;
            
            return request;
        }
        public override void ReturnToPool()
        {
            RequestProvider<ActorInstantiateRequest>.Return(this);
        }
        public override void Clear()
        {
            Index = 0;
        }
    }

    /// <summary> 필드 위에서의 액터 업데이트 요청 </summary>
    public class ActorUpdateRequest : RequestBase
    {
        public ActorUpdateRequest()
        {
            Type = RequestType.Actor_Update;
        }

        public override void Clear()
        {

        }

        public override void ReturnToPool()
        {
            RequestProvider<ActorUpdateRequest>.Return(this);
        }
    }

}
