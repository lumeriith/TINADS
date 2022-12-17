using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModeSpecificObject : MonoBehaviour
{
    public List<Tool> targetTools;
    public bool invert;
    
    private void Start()
    {
        GameManager.instance.onToolChanged += OnToolChanged;
        OnToolChanged(GameManager.instance.currentTool);
    }

    private void OnToolChanged(Tool obj)
    {
        gameObject.SetActive(invert ? !targetTools.Contains(obj) : targetTools.Contains(obj));
    }
}
