using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Camera    mMainCam;
    private Transform mTarget;
    [SerializeField] 
    private Vector3 mOffset;
    [SerializeField] 
    private Quaternion mRotation;


    private void Awake() 
    {
        mMainCam = transform.GetComponent<Camera>();
        enabled = false;
    }
    public void SetFollow(Transform target)
    {
        this.mTarget = target;
        transform.SetPositionAndRotation(mOffset, mRotation);
        enabled = true;
    }
    public void SetFOV(float scale)
    {
        mMainCam.fieldOfView = 60f * scale;
    }
    public void StopFollow()
    {
        enabled = false;
    }

    private void LateUpdate() 
    {
        transform.position = mTarget.position + mOffset;
    }
}
