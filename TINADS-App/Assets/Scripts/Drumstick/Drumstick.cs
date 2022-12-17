using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drumstick : MonoBehaviour
{
    public Vector3 immediateVelocity { get; private set; }
    public Vector3 filteredVelocity { get; private set; }
    
    public Transform velocitySamplePoint;
    public float filterRiseTime;
    public float filterDecayTime;

    private Vector3 _lastPosition;
    private float _lastSampleTime;

    private Vector3 _filterCv;

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
        if (filteredVelocity.sqrMagnitude > immediateVelocity.sqrMagnitude)
        {
            filteredVelocity = Vector3.SmoothDamp(filteredVelocity, immediateVelocity, ref _filterCv, filterDecayTime);
        }
        else
        {
            filteredVelocity = Vector3.SmoothDamp(filteredVelocity, immediateVelocity, ref _filterCv, filterRiseTime);
        }
        
        if (float.IsNaN(filteredVelocity.x) || float.IsNaN(_filterCv.x))
        {
            filteredVelocity = Vector3.zero;
            _filterCv = Vector3.zero;
        }
    }

    public Vector3 GetSamplePoint(float prediction)
    {
        return velocitySamplePoint.position + filteredVelocity * prediction;
    }
}
