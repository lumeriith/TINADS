using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumstickMovement : MonoBehaviour
{
    public Transform targetTransform;
    public Vector3 rotationOffset;
    
    public float posSmoothTime = 0.1f;
    public float rotSmoothTime = 0.1f;

    private Rigidbody _rb;
    private Vector3 _posVelocity;
    private Quaternion _rotVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        var targetPos = targetTransform.position;
        var targetRot = targetTransform.rotation * Quaternion.Euler(rotationOffset);
        _rb.MovePosition(Vector3.SmoothDamp(_rb.position, targetPos, ref _posVelocity, posSmoothTime, 1000f, Time.fixedDeltaTime));
        _rb.MoveRotation(QuaternionUtil.SmoothDamp(_rb.rotation, targetRot, ref _rotVelocity, rotSmoothTime, Time.fixedDeltaTime));
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}
