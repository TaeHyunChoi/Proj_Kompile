using System.Collections.Generic;

namespace Kompile.Manager
{
    using Data;
    using Provider;
    using static Data.Definition;

    /// <summary> receivers를 Stack처럼 구현 -> 함수명도 Stack 처럼</summary>
    public static class InputRouteSystem
    {
        private static readonly List<IInputReceivable> _receivers = new List<IInputReceivable>(8);
        private static readonly InputProvider _provider = new InputProvider();

        public static void Push(IInputReceivable receiver)
        {
            if (!_receivers.Contains(receiver))
            { 
                _receivers.Add(receiver);
            }
        }
        public static void Pop(IInputReceivable receiver)
        {
            _receivers.Remove(receiver);
        }

        public static void OnUpdate()
        {
            IDxInput input = _provider.Current.Current;
            if (IDxInput.NONE == input)
            {
                return;
            }

            for (int i = _receivers.Count - 1; i >= 0; --i)
            {
                // 어디선가 입력을 소진했다면? 순회 종료;
                if (_receivers[i].OnReceiveInput(input))
                {
                    break;
                }
            }

            // 입력값 갱신(또는 초기화)
            _provider.OnEndOfFrame();
        }
    }
}
