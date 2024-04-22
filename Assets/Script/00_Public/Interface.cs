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
    public void Input(Index.IDxInput.EInput input);
}
public interface IFixedInputHandler
{
    public void Input(Index.IDxInput.EInput input);
}