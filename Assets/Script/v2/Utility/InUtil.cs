namespace Kompile
{
    using System.Collections.Generic;
    using UnityEngine;
    using static Kompile.Data.Definition;

    public static class InUtil
    {
        public static bool Compare(this IDxInput input, IDxInput compare)
        {
            var flag = input & compare;
            return flag != IDxInput.NONE;
        }
    }
}
