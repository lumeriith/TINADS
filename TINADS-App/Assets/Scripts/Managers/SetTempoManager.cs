using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTempoManager : SingletonBehaviour<SetTempoManager>
{
    public int bpm { get; private set; } = 90;
    public float sensitivity = 15f;
    private float _step = 0;
    
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
            InstrumentManager.instance.StartMetronome(bpm);
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

        var axis = Input.GetAxis("XRI_Left_Primary2DAxis_Vertical");
        if (Mathf.Abs(axis) > 0.1f)
        {
            _step -= axis * Time.deltaTime * sensitivity;
            if (_step > 1)
            {
                _step--;
                bpm++;

                if (InstrumentManager.instance.IsMetronomePlaying())
                {
                    InstrumentManager.instance.StopMetronome();
                    InstrumentManager.instance.StartMetronome(bpm);
                }
            }
            else if (_step < -1)
            {
                _step++;
                bpm--;
                
                if (InstrumentManager.instance.IsMetronomePlaying())
                {
                    InstrumentManager.instance.StopMetronome();
                    InstrumentManager.instance.StartMetronome(bpm);
                }
            }
        }
        else
        {
            _step = 0;
        }
        
    }
}
