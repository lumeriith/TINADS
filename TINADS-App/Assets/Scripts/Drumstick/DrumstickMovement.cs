using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumstickMovement : MonoBehaviour
{
    public Transform targetTransform;
    public Rigidbody viewModelBody;
    public Vector3 rotationOffset;
    
    public float posSmooth = 0.015f;
    public float rotSmooth = 0.03f;

    public float viewModelPosSmooth = 0.015f;
    public float viewModelRotSmooth = 0.03f;
    
    private Vector3 _viewPosCv;
    private Quaternion _viewRotCv;    

    private Vector3 _posCv;
    private Quaternion _rotCv;

    private void FixedUpdate()
    {
        var targetPos = targetTransform.position;
        var targetRot = targetTransform.rotation * Quaternion.Euler(rotationOffset);
    
        viewModelBody.MovePosition(Vector3.SmoothDamp(viewModelBody.position, targetPos, ref _viewPosCv, viewModelPosSmooth, 1000f, Time.fixedDeltaTime));
        viewModelBody.MoveRotation(QuaternionUtil.SmoothDamp(viewModelBody.rotation, targetRot, ref _viewRotCv, viewModelRotSmooth, Time.fixedDeltaTime));
        viewModelBody.velocity = Vector3.zero;
        viewModelBody.angularVelocity = Vector3.zero;
    }

    private void Update()
    {
        var targetPos = targetTransform.position;
        var targetRot = targetTransform.rotation * Quaternion.Euler(rotationOffset);
        
        transform.SetPositionAndRotation(
            Vector3.SmoothDamp(transform.position, targetPos, ref _posCv, posSmooth), 
            QuaternionUtil.SmoothDamp(transform.rotation, targetRot, ref _rotCv, rotSmooth, Time.deltaTime)
            );
    }
}
