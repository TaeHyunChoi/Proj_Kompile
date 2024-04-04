using System.Collections.Generic;

public interface IDataSetter
{
    public void Set(Dictionary<string, string> table);
}
public interface ISequenceUpdater
{
    public void Start();
    public void Update();
    public void Close();
}
public interface IUpdateRoutine
{
    public int Update(int index);
}
