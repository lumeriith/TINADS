using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreferencesManager : SingletonBehaviour<PreferencesManager>
{
    public Vector3 drumSetPosition
    {
        get => _drumSetPosition;
        set
        {
            _drumSetPosition = value;
            PlayerPrefsUtil.SetVector3("DrumSetPosition", value);
        }
    }
    private Vector3 _drumSetPosition;

    public Quaternion drumSetRotation
    {
        get => _drumSetRotation;
        set
        {
            _drumSetRotation = value;
            PlayerPrefsUtil.SetQuaternion("DrumSetRotation", value);
        }
    }
    private Quaternion _drumSetRotation;
    
    public float drumSetScale
    {
        get => _drumSetScale;
        set
        {
            _drumSetScale = value;
            PlayerPrefs.SetFloat("DrumSetScale", value);
        }
    }
    private float _drumSetScale;
    
    private void Awake()
    {
        _drumSetPosition = PlayerPrefsUtil.GetVector3("DrumSetPosition");
        _drumSetRotation = PlayerPrefsUtil.GetQuaternion("DrumSetRotation");
        _drumSetScale = PlayerPrefs.GetFloat("DrumSetScale");
    }

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}
