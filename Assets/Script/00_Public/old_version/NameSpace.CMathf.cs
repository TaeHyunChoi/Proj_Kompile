namespace CMathf
{
    using System;
    using UnityEngine;

    public static class CMath
    {
        public static float Floor(float value, int digits = 3)
        {
            float d = (int)Mathf.Pow(10, digits);
            float sign = value < 0 ? -1 : 1;
            value *= sign;

            float d_invert;
            switch (digits)
            {
                case 2: d_invert = 0.01f; break;
                case 3: d_invert = 0.001f; break;
                default: d_invert = 1 / d; break;
            }

            return sign * (int)(value * d) * d_invert;
        }
        public static int FloorToInt(float value, int digits = 3)
        {
            return (int)Floor(value, digits);
        }
        //public static Vector3 FloorToVector(Vector3 value, int exponent = 3)
        //{
        //    float d = (int)Mathf.Pow(10, exponent);


        //    float x = MathF.Truncate(value.x * 1000) * 0.001f; // 이게 된다는거네
        //    float y = Floor(value.y, exponent);
        //    float z = Floor(value.z, exponent);

        //    return new Vector3(x, y, z);
        //}

        public static Vector3 Truncate(this Vector3 value, int digits = 3)
        {
            var x = Truncate(value.x, digits);
            var y = Truncate(value.y, digits);
            var z = Truncate(value.z, digits);

            return new Vector3(x, y, z);
        }
        public static float Truncate(this float value, int digits = 3)
        {
            //float d, d_invert;
            //switch (exponent)
            //{
            //    case 2:
            //        d = 100f;
            //        d_invert = 0.01f;
            //        break;
            //    case 3:
            //        d = 1000f;
            //        d_invert = 0.001f;
            //        break;
            //    default:
            //        d = Mathf.Pow(10, exponent);
            //        d_invert = 1 / d;
            //        break;
            //}

            return (float)Math.Round(value, digits);
            //return (float)(Math.Truncate(value * d) * d_invert);
            //value *= d;
            //value = (value >= 0) ? MathF.Floor(value) : MathF.Ceiling(value);
            ////return value * d_invert;
            //return (float)Math.Round(value * d_invert, 3);
        }

        public static Vector3Int ToInt(this Vector3 value)
        {
            return new Vector3Int((int)value.x, (int)value.y, (int)value.z);
        }

    }
}