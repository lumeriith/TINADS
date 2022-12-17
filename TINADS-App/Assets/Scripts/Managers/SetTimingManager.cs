using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTimingManager : SingletonBehaviour<SetTimingManager>
{
    public float sensitivity = 0.1f;
    public Vector2 clampRange;
    public float prediction;

    private void Start()
    {
        prediction = PreferencesManager.instance.prediction;
    }

    private void Update()
    {
        if (GameManager.instance.currentTool != Tool.SetTiming) return;
                
        var axis = Input.GetAxis("XRI_Left_Primary2DAxis_Vertical");
        if (Mathf.Abs(axis) > 0.1f)
        {
            prediction -= axis * sensitivity * Time.deltaTime;
            prediction = Mathf.Clamp(prediction, clampRange.x, clampRange.y);
            PreferencesManager.instance.prediction = prediction;
        }
    }
}
