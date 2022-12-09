using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    public Effect hitEffect;
    public float minScale = 0.2f;
    public float maxScale = 1f;
    
    private void Start()
    {
        InstrumentManager.instance.onInstrumentHit += info =>
        {
            var eff = hitEffect.PlayNew(info.point, Quaternion.identity);
            eff.transform.localScale = Mathf.Lerp(minScale, maxScale, info.normalizedVelocity) * Vector3.one;
        };
    }
}
