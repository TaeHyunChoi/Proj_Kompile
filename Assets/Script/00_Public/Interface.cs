using System.Collections.Generic;

public interface IDataSetter
{
    public void Set(Dictionary<string, string> table);
}
public interface IRoutineUpdater
{
    public int MoveNext(int index);
}
public interface IInputHandler
{
    public void Input(int input);
}
public interface IFixedInputHandler
{
    public void Input(int input);
}