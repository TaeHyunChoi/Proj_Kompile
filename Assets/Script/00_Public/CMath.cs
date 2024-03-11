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

        public static Vector3 Floor1000Vector3(Vector3 value)
        {
            return new Vector3(Floor1000(value.x), Floor1000(value.y), Floor1000(value.z));
        }
        public static float Floor1000(float value)
        {
            value = value >= 0 ?  Mathf.FloorToInt( value * 1000f) * 0.001f 
                               : -Mathf.FloorToInt(-value * 1000f) * 0.001f;

            return value;
        }
        public static int FloorToInt1000(float value)
        {
            return (int)Floor1000(value);
        }
    }
}
