using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public interface IDataSetter
{
    public void Set(Dictionary<string, string> table);
}
public interface ISequenceUpdater
{
    public void Update();
}