namespace Kompile
{
    using System.Text;
    using System.Diagnostics;

    public static partial class InDev
    {
        private static readonly StringBuilder _sb = new StringBuilder();
        private static string _content;

#if DEV_BUILD
        private static string GetString(string msg = null)
        {
            _sb.Append("[<color=green>DEBUG</color>]");
            
            if (!string.IsNullOrEmpty(msg))
            {
                _sb.Append($" {msg}");
            }

            _content = _sb.ToString();
            _sb.Clear();

            return _content;
        }
#endif

        [Conditional("DEV_BUILD")]
        public static void Log(string msg = null)
        {
#if DEV_BUILD
            GetString(msg);
            UnityEngine.Debug.Log(_content);
#endif
        }

        [Conditional("DEV_BUILD")]
        public static void LogWarning(string msg = null)
        {
#if DEV_BUILD
            GetString(msg);
            UnityEngine.Debug.LogWarning(_content);
#endif
        }

        [Conditional("DEV_BUILD")]
        public static void LogError(string msg = null)
        {
#if DEV_BUILD
            GetString(msg);
            UnityEngine.Debug.LogError(_content);
#endif
        }
    }
}