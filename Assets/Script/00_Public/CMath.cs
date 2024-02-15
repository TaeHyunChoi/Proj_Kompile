using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMathf
{
    public static class CMath
    {
        public static Vector3 Floor1000Vector3(Vector3 value)
        {
            //When rounding down a negative number, an unexpected value appears. (ex. -0.0000000873f => -0.001f)
            //So, Treat it as a positive number and add the sign again.
            float x = value.x >= 0 ? Floor1000(value.x) : -Floor1000(-value.x);
            float y = value.y >= 0 ? Floor1000(value.y) : -Floor1000(-value.y);
            float z = value.z >= 0 ? Floor1000(value.z) : -Floor1000(-value.z);

            return new Vector3(x, y, z);
        }
        public static Vector3 Ceil1000Vector3(Vector3 value)
        {
            //잠깐만.. ceil은 어떻게 되는거지
            float x = Ceil1000(value.x);
            float y = Ceil1000(value.y);
            float z = Ceil1000(value.z);

            return new Vector3(x, y, z);
        }

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
