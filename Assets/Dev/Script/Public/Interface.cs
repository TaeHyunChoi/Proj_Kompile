using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class Interface
{
    public interface IDataSetter
    {
        public void SetTable(Dictionary<string, string> table)
        { 
            
        }
    }
}

public static class DInterface
{
    public interface IDataSetter
    {
        public abstract void Set(byte[] data, int start);
    }
}