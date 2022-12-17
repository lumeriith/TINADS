using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldMetronome : MonoBehaviour
{
    public float smoothTime = 0.1f;
    public float scaleMultiplier = 1.5f;

    private Vector3 _localScale;
    private Vector3 _cv;
    
    private void Start()
    {
        _localScale = transform.localScale;
        InstrumentManager.instance.onMetronomeBeat += OnMetronomeBeat;
    }

    private void Update()
    {
        transform.localScale = Vector3.SmoothDamp(transform.localScale, InstrumentManager.instance.IsMetronomePlaying() ? _localScale : Vector3.zero, ref _cv, smoothTime);
    }

    private void OnMetronomeBeat()
    {
        transform.localScale = _localScale * scaleMultiplier;
    }
}
