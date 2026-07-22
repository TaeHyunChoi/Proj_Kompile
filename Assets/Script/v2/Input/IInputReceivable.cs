using UnityEngine;

namespace Kompile.Data
{
    public interface IInputReceivable
    {
        /// <summary> 입력을 성공적으로 소비(Consume)했다면 true 반환
        /// </summary>
        bool OnReceiveInput(Definition.IDxInput inputState);
    }
}
