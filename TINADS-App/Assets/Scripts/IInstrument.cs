using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Instrument : MonoBehaviour
{
    public abstract eInstrumentType GetInstrumentType(Vector3 worldPoint);
}
