using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerInField : MonoBehaviour
{
    [SerializeField] InteractType type;
    private bool isInTrigger;

    //이거 설계 필요하다. 일단 구성만 좀 들고 가자.
    private void Call()
    {
        switch (type)
        {
            case InteractType.Talk: Talk(); break;
        }
    }

    private void Talk()
    {
        Debug.Log("Talk");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInTrigger
            && other.GetComponent<UnitPlayer>() != null)
        {
            isInTrigger = true;
            Call();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<UnitPlayer>() != null)
        {
            isInTrigger = false;
        }
    }
}
