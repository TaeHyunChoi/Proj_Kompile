namespace Script.Util
{
    public static class FuncUtil
    {
        public static T[] Reset<T>(this T[] array, int length)
        {
            if (null == array
                || length != array.Length)
            {
                return new T[length];
            }

            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = default;
            }

            return array;
        }
    }
}