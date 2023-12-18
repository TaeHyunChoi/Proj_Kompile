using System.Collections.Generic;
using UnityEngine;

public interface IDataSetter
{
    public void Set(Dictionary<string, string> table);
}

public interface IUpdateBySection
{
    public delegate void UpdateDele();
    public void MoveNext();
}