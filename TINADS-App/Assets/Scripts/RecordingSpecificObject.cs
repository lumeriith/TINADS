using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordingSpecificObject : MonoBehaviour
{
    public bool disableOnRecording;
    
    private void Start()
    {
        InstrumentManager.instance.onIsRecordingChanged += OnIsRecordingChanged;
        OnIsRecordingChanged(InstrumentManager.instance.IsRecording());
    }

    private void OnIsRecordingChanged(bool obj)
    {
        gameObject.SetActive(disableOnRecording ? !obj : obj);
    }
}
