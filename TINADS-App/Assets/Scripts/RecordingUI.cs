using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RecordingUI : MonoBehaviour
{
    public GameObject blinker;
    public TextMeshProUGUI timeText;
    
    private void OnEnable()
    {
        StartCoroutine(nameof(BlinkerRoutine));
    }

    private void OnDisable()
    {
        StopCoroutine(nameof(BlinkerRoutine));
    }

    private IEnumerator BlinkerRoutine()
    {
        blinker.SetActive(true);
        while (true)
        {
            yield return new WaitForSeconds(1f);
            blinker.SetActive(!blinker.activeSelf);
        }
    }

    private void Update()
    {
        if (!InstrumentManager.instance.IsRecording()) return;
        TimeSpan t = TimeSpan.FromSeconds(InstrumentManager.instance.GetCurrentRecordingDurationBySeconds());

        timeText.text = string.Format("{0:D2}:{1:D2}.{2:D3}", 
            t.Minutes, 
            t.Seconds, 
            t.Milliseconds);
    }
}
