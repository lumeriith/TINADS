using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectAutoDestroy : MonoBehaviour
{
    private ParticleSystem _ps;
    private AudioSource _as;
    private float _startTime;
    
    private void Awake()
    {
        _startTime = Time.time;
        _ps = GetComponent<ParticleSystem>();
        _as = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (Time.time - _startTime < 1) return;
        if (_ps != null && _ps.IsAlive()) return;
        if (_as != null && _as.isPlaying) return;
        Destroy(gameObject);
    }
}