using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InstrumentType
{
    Bass, 
    CymbalRideSmall, CymbalCrash, CymbalRideBig, CymbalHiHat,
    TomHigh, TomMid, TomFloor,
    Snare
}

public struct HitInfo
{
    public InstrumentType instrument;
    public Vector3 velocity;
    public float normalizedVelocity;
}

public class InstrumentManager : SingletonBehaviour<InstrumentManager>
{
    
}
