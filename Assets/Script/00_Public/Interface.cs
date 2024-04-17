using System;
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
    public int MoveNext(int index);
}

public interface IGetInput
{
    public void Input(int input);
}

// This is the code recommended by Chat-GPT, but it is not well understood. (event-base input callback)
/*
public interface IGetInput
{
    event Action<int> OnInputReceived;
    void UpdateInput(int input);
}
public class InputGetter : IGetInput
{
    public event Action<int> OnInputReceived;

    public void UpdateInput(int input)
    {
        OnInputReceived?.Invoke(input);
    }
}
public class InputHandler
{
    public InputHandler(IGetInput inputGetter)
    {
        inputGetter.OnInputReceived += HandleInput;
    }

    private void HandleInput(int input)
    {
        // �Է� ó�� ����
    }
}
//*/