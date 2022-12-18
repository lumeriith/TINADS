using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NotificationManager : SingletonBehaviour<NotificationManager>
{
    public Transform hudTransform;
    public GameObject notificationPrefab;

    public void CreateNotification(string text)
    {
        var newNoti = Instantiate(notificationPrefab, hudTransform);
        newNoti.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    public void NotifyApplyTempo()
    {
        if (!InstrumentManager.instance.IsMetronomePlaying()) return;
        CreateNotification($"Set tempo of {SetTempoManager.instance.bpm}bpm");
    }
    
    public void NotifyEndMove()
    {
        CreateNotification($"Saved drum-set placement");
    }
    
    public void NotifyRecordingDone()
    {
        CreateNotification($"Recording saved in 'TINADS Recordings'");
    }
    
    public void NotifyApplyAdjustment()
    {
        CreateNotification($"Saved prediction timing of {(int)(SetTimingManager.instance.prediction * 1000)}ms");
    }
}
