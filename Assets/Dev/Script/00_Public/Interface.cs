using System.Collections.Generic;

public interface IDataSetter
{
    public void Set(Dictionary<string, string> table);
}

public interface ICoroutine
{
    public delegate void MoveDele();

    public void MoveNext(int index);
}