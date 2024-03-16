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

            //float sign = value >= 0 ? 1f : -1f;
            //value = Mathf.FloorToInt(sign * value * d);
            //value *= sign * di;

            value = Mathf.FloorToInt(value * d) * di;
            return value;
        }
        public static int FloorToInt(float value, int exponent)
        {
            return (int)Floor(value, exponent);
        }
        public static Vector3 FloorToVector(Vector3 value, int exponent)
        {
            float x = Floor(value.x, exponent);
            float y = Floor(value.y, exponent);
            float z = Floor(value.z, exponent);

            return new Vector3(x, y, z);
        }
    }
}
