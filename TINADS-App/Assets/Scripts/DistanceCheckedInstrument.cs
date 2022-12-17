using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceCheckedInstrument : Instrument
{
    public Transform distanceCheckOrigin;
    public float thresholdRadius;
    public eInstrumentType nearType;
    public eInstrumentType farType;
    
    public override eInstrumentType GetInstrumentType(Vector3 worldPoint)
    {
        var point = distanceCheckOrigin.InverseTransformPoint(worldPoint);
        if (point.sqrMagnitude > thresholdRadius * thresholdRadius) 
            return farType;
        return nearType;
    }

    private void OnDrawGizmos()
    {
        if (distanceCheckOrigin == null) return;
        Gizmos.DrawWireSphere(distanceCheckOrigin.position, distanceCheckOrigin.lossyScale.x * thresholdRadius);
    }
}
