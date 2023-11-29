using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class Interface
{
    public interface IDataSetter
    {
        public abstract void SetData(Dictionary<string, string> table);
        public void Debug()
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