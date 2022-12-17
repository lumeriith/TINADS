using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PrimaryMenuButton : MonoBehaviour
{
    public bool isHovering { get; private set; }

    public Transform hoverCircle;
    public TextMeshProUGUI nameText;
    public Color hoverTextColor;

    private Color _textDefaultColor;
    private float _cv;

    private void Awake()
    {
        _textDefaultColor = nameText.color;
    }

    public virtual void OnCursorEnter()
    {
        isHovering = true;
        nameText.color = hoverTextColor;
    }

    public virtual void OnCursorExit()
    {
        nameText.color = _textDefaultColor;
        isHovering = false;
    }

    public virtual void OnCursorClick()
    {
        
    }

    private void Update()
    {
        hoverCircle.localScale = Mathf.SmoothDamp(hoverCircle.localScale.x, isHovering ? 1 : 0, ref _cv, 0.1f) * Vector3.one;
    }
}
