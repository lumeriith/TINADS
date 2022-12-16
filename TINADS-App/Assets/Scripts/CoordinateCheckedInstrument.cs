using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateCheckedInstrument : Instrument
{
    public enum ComponentType
    {
        X, Y, Z
    }

    public ComponentType usedComponent;
    public Transform coordinateCheckOrigin;
    public eInstrumentType positiveType;
    public eInstrumentType negativeType;
    
    public override eInstrumentType GetInstrumentType(Vector3 worldPoint)
    {
        var point = coordinateCheckOrigin.InverseTransformPoint(worldPoint);
        float component;
        switch (usedComponent)
        {
            case ComponentType.X:
                component = point.x;
                break;
            case ComponentType.Y:
                component = point.y;
                break;
            case ComponentType.Z:
                component = point.z;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return component > 0 ? positiveType : negativeType;
    }
}
