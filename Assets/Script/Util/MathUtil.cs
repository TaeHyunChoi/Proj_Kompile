namespace Script.Util
{
    using System;
    using UnityEngine;

    /// <summary> System.Math, UnityEngine.Math와 구분하기 위하여 MathUtil로 명명</summary>
    public static class MathUtil
    {
        public static int ToInt(this float value, int digits = 3)
        {
            return (int)Math.Round(value, digits);
        }
        public static Vector3Int ToInt(this Vector3 value)
        {
            int intX = value.x.ToInt();
            int intY = value.y.ToInt();
            int intZ = value.z.ToInt();

            return new Vector3Int(intX, intY, intZ);
        }

    }
}