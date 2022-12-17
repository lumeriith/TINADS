using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTempoManager : SingletonBehaviour<SetTempoManager>
{
    public int bpm { get; private set; } = 90;
    
    private void Update()
    {
        if (GameManager.instance.currentTool != Tool.SetTempo) return;

        if (Input.GetButtonDown("XRI_Right_TriggerButton"))
        {
            InstrumentManager.instance.StopMetronome();
            InstrumentManager.instance.StartBpmCounter();
        }

        if (Input.GetButton("XRI_Right_TriggerButton"))
        {
            bpm = (int)InstrumentManager.instance.GetCurrentBpm();
        }

        if (Input.GetButtonUp("XRI_Right_TriggerButton"))
        {
            InstrumentManager.instance.StopBpmCounter();
            bpm = (int)InstrumentManager.instance.GetCurrentBpm();
        }

        if (Input.GetButtonDown("XRI_Left_TriggerButton"))
        {
            if (InstrumentManager.instance.IsMetronomePlaying())
            {
                InstrumentManager.instance.StopMetronome();
            }
            else
            {
                InstrumentManager.instance.StartMetronome(bpm);
            }
        }
    }
}
