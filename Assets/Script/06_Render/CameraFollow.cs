using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Quaternion rotation;
    private void Awake() 
    {
        enabled = false;
    }
    //Camera.main.GetComponent<CameraFollow> 라고 한 번만 쓰고 날리는 게 좋으려나
    public void SetFollow(Transform target)
    {
        this.target = target;
        transform.SetPositionAndRotation(offset, rotation);
        enabled = true;
    }
    public void StopFollow()
    {
        enabled = false;
    }

    private void LateUpdate() 
    {
        transform.position = target.position + offset;
    }
}
