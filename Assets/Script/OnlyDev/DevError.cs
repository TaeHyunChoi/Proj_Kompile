#if UNITY_EDITOR || TEST_BUILD
namespace Script.OnlyDev
{
    using UnityEngine;
    using Script.Index;

    public static class DevError
    {
        /// <summary> 게임을 중단할 정도는 아니지만 수정 요망 </summary>
        public static void DebugWarning(ErrorCode code, string desc)
        {
            Debug.LogErrorFormat($"[{code}] {desc}");
        }

        /// <summary> 게임을 중단시킬 정도로 중대한 에러 </summary>
        public static void DebugAssert(ErrorCode code, string desc)
        {
            Debug.Assert(code == ErrorCode.NONE, $"[{code}] {desc}");
        }
    }
}
#endif