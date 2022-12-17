using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredictedPointIndicator : MonoBehaviour
{
    private Drumstick _stick;
    
    private void Start()
    {
        _stick = GetComponentInParent<Drumstick>();
    }

    private void Update()
    {
        transform.position = _stick.GetSamplePoint(SetTimingManager.instance.prediction);

    }
}
