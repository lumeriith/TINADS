using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerButtonIndicator : MonoBehaviour
{
    public bool isLeft;
    public float smoothTime = 0.1f;

    private float _cv;
    private Vector3 _defaultScale;

    private void Start()
    {
        _defaultScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        transform.localScale =
            Mathf.SmoothDamp(transform.localScale.x / _defaultScale.x, GetButton() ? 1 : 0, ref _cv, smoothTime) * _defaultScale;
    }

    private bool GetButton()
    {
        return Input.GetButton(isLeft ? "XRI_Left_TriggerButton" : "XRI_Right_TriggerButton");
    }
}
