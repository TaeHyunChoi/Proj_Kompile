using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coroutiner : MonoBehaviour
{
    private static Coroutiner instance;

    private void Awake()
    {
        instance = this;
    }
    public static Coroutine PlayCoroutine(IEnumerator coroutine)
    {
        return instance.StartCoroutine(coroutine);
    }
}
