using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInstrument : Instrument
{
    public eInstrumentType type;
    
    public override eInstrumentType GetInstrumentType(Vector3 worldPoint)
    {
        return type;
    }
}
