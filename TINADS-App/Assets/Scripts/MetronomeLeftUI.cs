using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MetronomeLeftUI : MonoBehaviour
{
    public GameObject onGameObject;
    public GameObject offGameObject;

    public TextMeshProUGUI bpmText;
    
    private void Update()
    {
        onGameObject.SetActive(InstrumentManager.instance.IsMetronomePlaying() || InstrumentManager.instance.IsCalculatingBpm());
        offGameObject.SetActive(!InstrumentManager.instance.IsMetronomePlaying() && !InstrumentManager.instance.IsCalculatingBpm());

        bpmText.text = SetTempoManager.instance.bpm.ToString();
    }
}
