using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMathf
{
    public static class CMath
    {
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
