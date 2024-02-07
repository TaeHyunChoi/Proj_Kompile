using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMathf
{
    public static class CMath
    {
        public static float Floor1000(float value)
        {
            return Mathf.FloorToInt(value * 1000f) * 0.001f;
        }
        public static float Ceil1000(float value)
        {
            return Mathf.CeilToInt(value * 1000f) * 0.001f;
        }

        public static int FloorToInt1000(float value)
        {
            return (int)Floor1000(value);
        }
        public static int CeilToInt1000(float value)
        {
            return (int)Ceil1000(value);
        }
    }
}
