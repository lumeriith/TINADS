using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDCanvas : MonoBehaviour
{
    public float desiredDistance = 2f;
    public float smoothTime = 2f;

    private Camera _main;
    private Vector3 _posCv;
    private Quaternion _rotCv;

    private void Start()
    {
        _main = Camera.main;
    }

    private void Update()
    {
        var rot = _main.transform.rotation.eulerAngles;

        transform.position = Vector3.SmoothDamp(transform.position, _main.transform.position + _main.transform.forward * desiredDistance, ref _posCv, smoothTime);
        transform.rotation = QuaternionUtil.SmoothDamp(transform.rotation, Quaternion.Euler(rot.x, rot.y, 0), ref _rotCv,
            smoothTime, Time.deltaTime);
    }
}
