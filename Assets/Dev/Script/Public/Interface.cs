using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class Interface
{
    public interface IDataSetter
    {
        public abstract void Set(Dictionary<string, string> table);
    }
}