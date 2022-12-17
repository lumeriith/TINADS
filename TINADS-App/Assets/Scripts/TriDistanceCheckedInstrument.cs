using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriDistanceCheckedInstrument : Instrument
{
    public Transform distanceCheckOrigin;
    public float nearThresholdRadius;
    public float farThresholdRadius;

    public eInstrumentType nearType;
    public eInstrumentType midType;
    public eInstrumentType farType;
    
    public override eInstrumentType GetInstrumentType(Vector3 worldPoint)
    {
        var point = distanceCheckOrigin.InverseTransformPoint(worldPoint);
        var sqrMag = point.sqrMagnitude;
        if (sqrMag > farThresholdRadius * farThresholdRadius) 
            return farType;
        if (sqrMag > nearThresholdRadius * nearThresholdRadius)
            return midType;
        return nearType;
    }

    private void OnDrawGizmos()
    {
        if (distanceCheckOrigin == null) return;
        Gizmos.DrawWireSphere(distanceCheckOrigin.position, distanceCheckOrigin.lossyScale.x * farThresholdRadius);
        Gizmos.DrawWireSphere(distanceCheckOrigin.position, distanceCheckOrigin.lossyScale.x * nearThresholdRadius);
    }
}
