using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrimaryMenu : MonoBehaviour
{
    public Vector3 appearOffset;
    public Canvas menuCanvas;
    public float appearAlphaSpeed;
    public float appearScaleSpeed;
    public Transform primaryControllerTransform;

    public Image cursorImage;
    
    private CanvasGroup _menuCanvasGroup;
    private Vector3 _menuOriginalScale;
    private Camera _main;
    
    
    private void Start()
    {
        _menuCanvasGroup = menuCanvas.GetComponent<CanvasGroup>();
        _menuCanvasGroup.alpha = 0;
        _menuOriginalScale = menuCanvas.transform.localScale;
        menuCanvas.transform.localScale = Vector3.zero;
        _main = Camera.main;
    }

    private void Update()
    {
        var isMenuShown = Input.GetButton("XRI_Right_PrimaryButton");
        _menuCanvasGroup.alpha = Mathf.MoveTowards(_menuCanvasGroup.alpha,
            isMenuShown ? 1 : 0, appearAlphaSpeed * Time.deltaTime);
        menuCanvas.transform.localScale = Vector3.MoveTowards(menuCanvas.transform.localScale, isMenuShown ? _menuOriginalScale : Vector3.zero,
            appearScaleSpeed * Time.deltaTime * _menuOriginalScale.magnitude);
        
        if (Input.GetButtonDown("XRI_Right_PrimaryButton"))
        {
            var delta = primaryControllerTransform.position - _main.transform.position;
            delta.y = 0;
            var rot = Quaternion.LookRotation(delta);
            menuCanvas.transform.SetPositionAndRotation(primaryControllerTransform.position + rot * appearOffset, rot);
        }

        if (isMenuShown)
        {
            var canvasPlane = new Plane(menuCanvas.transform.forward, menuCanvas.transform.position);
            var closestPoint = canvasPlane.ClosestPointOnPlane(primaryControllerTransform.position +
                                            menuCanvas.transform.rotation * appearOffset);
            cursorImage.transform.position = closestPoint;
        }
        
    }
}
