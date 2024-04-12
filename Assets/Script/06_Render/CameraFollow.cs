using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Camera mainCam;
    private Transform target;
    [SerializeField] 
    private Vector3 offset;
    [SerializeField] 
    private Quaternion rotation;


    private void Awake() 
    {
        mainCam = transform.GetComponent<Camera>();
        enabled = false;
    }
    public void SetFollow(Transform target)
    {
        this.target = target;
        transform.SetPositionAndRotation(offset, rotation);
        enabled = true;
    }
    public void SetFOV(float scale)
    {
        mainCam.fieldOfView = 60f * scale;
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
