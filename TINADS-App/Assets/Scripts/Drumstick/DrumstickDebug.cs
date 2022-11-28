using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DrumstickDebug : MonoBehaviour
{
    public TextMeshProUGUI velocityText;
    
    private Drumstick _drumstick;

    private void Start()
    {
        _drumstick = GetComponent<Drumstick>();
    }

    private void Update()
    {
        velocityText.text = _drumstick.immediateVelocity.magnitude.ToString("0.0000");
    }
}
