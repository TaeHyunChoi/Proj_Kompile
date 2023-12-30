using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coroutiner : MonoBehaviour
{
    private static Coroutiner instance;
    private List<IEnumerator> ieList;

    private void Awake()
    {
        instance = this;
        ieList = new List<IEnumerator>();
    }

    public static void PlayCoroutine(IEnumerator coroutine)
    {
        instance.StartCoroutine(instance.Play(coroutine));
    }
    private IEnumerator Play(IEnumerator coroutine)
    {
        ieList.Add(coroutine);
        yield return this.StartCoroutine(coroutine);
        ieList.Remove(coroutine);
    }
}
