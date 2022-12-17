using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleMetronome : MonoBehaviour
{
    public Effect mainBeat;
    public Effect smallBeat;

    private void Start()
    {
        InstrumentManager.instance.onMetronomeBeat += OnMetronomeBeat;
    }

    private void OnMetronomeBeat(int beat)
    {
        if (beat == 0) mainBeat.PlayNew();
        else smallBeat.PlayNew();
    }
}
