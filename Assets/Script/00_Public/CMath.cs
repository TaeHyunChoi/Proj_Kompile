using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMathf
{
    public static class CMath
    {
        public static float Floor(float value, int exponent)
        {
            float d = (int)Mathf.Pow(10, exponent);
            float di = 1 / d;

            value = value >= 0 ? Mathf.FloorToInt(value * d) * di
                               : -Mathf.FloorToInt(-value * d) * di;

            return value;
        }
        public static int FloorToInt(float value, int exponent)
        {
            return (int)Floor(value, exponent);
        }
        public static Vector3 FloorToVector(Vector3 value, int exponent)
        {
            return new Vector3(Floor(value.x, exponent), Floor(value.y, exponent), Floor(value.z, exponent));
        }
    }
}
