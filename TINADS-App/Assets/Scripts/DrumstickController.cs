using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumstickController : MonoBehaviour
{
    public Transform targetTransform;
    public Vector3 rotationOffset;

    private Rigidbody _rb;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(targetTransform.position);
        _rb.MoveRotation(Quaternion.Euler(rotationOffset) * targetTransform.rotation);
    }
}
