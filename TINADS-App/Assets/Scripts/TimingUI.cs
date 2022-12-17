using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimingUI : MonoBehaviour
{
    public TextMeshProUGUI timingText;

    private void Update()
    {
        timingText.text = (int) (SetTimingManager.instance.prediction * 1000) + "ms";
    }
}
