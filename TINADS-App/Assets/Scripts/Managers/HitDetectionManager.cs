using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDetectionManager : SingletonBehaviour<HitDetectionManager>
{
    public bool enableTestSound;
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

        var samplePoint = stick.GetSamplePoint(SetTimingManager.instance.prediction);
        
        if (stick.filteredVelocity.sqrMagnitude < minVelSqr || stick.filteredVelocity.y > 0)
        {
            lastCheckPoint = samplePoint;
            return;
        }

        if (GameManager.instance.currentTool != Tool.DrumStick &&
            GameManager.instance.currentTool != Tool.SetTempo &&
            GameManager.instance.currentTool != Tool.SetTiming)
        {
            lastCheckPoint = samplePoint;
            return;
        }

        if (GameManager.instance.currentTool == Tool.SetTempo && stick == leftStick) return;
        
        
        var normalized = Mathf.Clamp((stick.filteredVelocity.magnitude - minVelocity) / (maxVelocity - minVelocity), 0, 1);
        var dir = samplePoint - lastCheckPoint;
        if (dir.sqrMagnitude < 0.001f) return;
        var count = Physics.RaycastNonAlloc(lastCheckPoint, dir, _hitBuffer, dir.magnitude);
        for (int i = 0; i < count; i++)
        {
            if (_hitBuffer[i].collider.TryGetComponent<Instrument>(out var instrument))
            {
                var insType = instrument.GetInstrumentType(_hitBuffer[i].point);
                if (lastHit.collider == _hitBuffer[i].collider && Time.time - lastHit.time < minInterval) return;
                Debug.Log($"Hit {insType} with {normalized} strength");
                var info = new HitInfo
                {
                    time = Time.time,
                    instrument = insType,
                    velocity = stick.filteredVelocity,
                    normalizedVelocity = normalized,
                    point = _hitBuffer[i].point,
                    collider = _hitBuffer[i].collider
                };
                InstrumentManager.instance.onInstrumentHit?.Invoke(info);
                lastHit = info;

                if (enableTestSound) GetComponent<AudioSource>().Play();
            }
        }

        lastCheckPoint = samplePoint;
    }
}
