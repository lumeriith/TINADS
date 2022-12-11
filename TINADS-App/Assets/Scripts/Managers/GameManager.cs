using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Tool
{
    DrumStick, MoveDrumSet, SetTempo
}

public class GameManager : SingletonBehaviour<GameManager>
{
    public Action<Tool> onToolChanged;

    public Tool currentTool { get; private set; }

    public void SetTool(Tool tool)
    {
        if (currentTool == tool) return;
        currentTool = tool;
        onToolChanged?.Invoke(tool);
    }
}
