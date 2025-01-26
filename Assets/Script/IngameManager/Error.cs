
namespace Script.GameManager
{
    using Script.Index;
    using System.Diagnostics;

    public static class Error
    {
        public static void DebugAssert(ErrorCode code, string desc)
        {
#if UNITY_EDITOR || TEST_BUILD
            Debug.Assert(code == ErrorCode.NONE, $"[{code}] {desc}");
#endif
        }
    }
}

