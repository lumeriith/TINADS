using System;
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
    /// <summary>
    /// Scalar velocity between 0 and 1.
    /// </summary>
    public float normalizedVelocity;
}

public class InstrumentManager : SingletonBehaviour<InstrumentManager>
{
    public Action<HitInfo> onInstrumentHit;

    private void Start()
    {
        StartCoroutine(TestRoutine());

        IEnumerator TestRoutine()
        {
            yield return new WaitForSeconds(1f);
            while (true)
            {
                onInstrumentHit?.Invoke(new HitInfo
                {
                    instrument = InstrumentType.Snare,
                    normalizedVelocity = 0.8f
                });
                yield return new WaitForSeconds(0.3f);
                onInstrumentHit?.Invoke(new HitInfo
                {
                    instrument = InstrumentType.CymbalHiHat,
                    normalizedVelocity = 0.6f
                });
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}
