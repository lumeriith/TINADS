using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDetectionManager : SingletonBehaviour<HitDetectionManager>
{
    public Drumstick leftStick;
    public Drumstick rightStick;
    public float minVelocity;
    public float maxVelocity;
    public float minInterval = 0.2f;

    private Vector3 _leftStickLastCheckPoint;
    private Vector3 _rightStickLastCheckPoint;

    private HitInfo _leftLastHitInfo;
    private HitInfo _rightLastHitInfo;
    
    private void Update()
    {
        DetectHit(leftStick, ref _leftStickLastCheckPoint, ref _leftLastHitInfo);
        DetectHit(rightStick, ref _rightStickLastCheckPoint, ref _rightLastHitInfo);
    }

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[256];
    private void DetectHit(Drumstick stick, ref Vector3 lastCheckPoint, ref HitInfo lastHit)
    {
        var minVelSqr = minVelocity * minVelocity;
     
        if (stick.filteredVelocity.sqrMagnitude < minVelSqr || stick.filteredVelocity.y > 0)
        {
            lastCheckPoint = stick.velocitySamplePoint.position;
            return;
        }
        
        
        var normalized = Mathf.Clamp((stick.filteredVelocity.magnitude - minVelocity) / (maxVelocity - minVelocity), 0, 1);
        var dir = stick.velocitySamplePoint.position - lastCheckPoint;
        var count = Physics.RaycastNonAlloc(lastCheckPoint, dir, _hitBuffer, dir.magnitude);
        for (int i = 0; i < count; i++)
        {
            if (_hitBuffer[i].collider.TryGetComponent<Instrument>(out var instrument))
            {
                if (lastHit.instrument == instrument.type && Time.time - lastHit.time < minInterval) return;
                Debug.Log($"Hit {instrument.type} with {normalized} strength");
                var info = new HitInfo
                {
                    time = Time.time,
                    instrument = instrument.type,
                    velocity = stick.filteredVelocity,
                    normalizedVelocity = normalized,
                    point = _hitBuffer[i].point
                };
                InstrumentManager.instance.onInstrumentHit?.Invoke(info);
                lastHit = info;
            }
        }

        lastCheckPoint = stick.velocitySamplePoint.position;
    }
}
