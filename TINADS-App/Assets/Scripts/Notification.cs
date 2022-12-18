using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notification : MonoBehaviour
{
    public Vector3 initPos;
    public float lifeTime;
    public float floatSpeed;
    public AnimationCurve alpha;

    private CanvasGroup _cg;
    private float _startTime;
    
    private void Start()
    {
        transform.localPosition = initPos;
        transform.localRotation = Quaternion.identity;
        _startTime = Time.time;
        _cg = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        transform.localPosition += Vector3.up * (Time.deltaTime * floatSpeed);
        _cg.alpha = alpha.Evaluate(Time.time - _startTime);
        if (Time.time - _startTime > lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
