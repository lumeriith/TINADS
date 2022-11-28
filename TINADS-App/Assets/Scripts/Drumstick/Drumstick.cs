using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drumstick : MonoBehaviour
{
    public Transform velocitySamplePoint;
    public Vector3 immediateVelocity { get; private set; }
    
    private Vector3 _lastPosition;
    private float _lastSampleTime;

    private void Start()
    {
        _lastPosition = velocitySamplePoint.position;
        _lastSampleTime = Time.time;
    }

    private void Update()
    {
        if (velocitySamplePoint.position == _lastPosition) return;
        immediateVelocity = (velocitySamplePoint.position - _lastPosition) / (Time.time - _lastSampleTime);
        _lastSampleTime = Time.time;
        _lastPosition = velocitySamplePoint.position;
    }
}
