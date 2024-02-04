using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMathf
{
    public static class CMath
    {
        public static float FloorToInt1000(float value)
        {
            return Mathf.FloorToInt(value * 1000f) * 0.001f;
        }
        public static float CeilToInt1000(float value)
        {
            return Mathf.CeilToInt(value * 1000f) * 0.001f;
        }
    }
}
