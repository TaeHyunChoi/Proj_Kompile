using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Content : MonoBehaviour
{
    protected delegate void UpdateDele();
    protected UpdateDele updateFunc;
    protected int status;

    public abstract void Play();
    protected abstract void MoveNext();
}
