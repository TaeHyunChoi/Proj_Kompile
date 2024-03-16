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

            //exponent가 대부분 2 또는 3이므로 캐싱
            float d_invert;
            switch (exponent)
            {
                case 2:  d_invert = 0.01f;  break;
                case 3:  d_invert = 0.001f; break;
                default: d_invert = 1 /d;   break;
            }

            return (int)(value * d) * d_invert;
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
