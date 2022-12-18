using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrimaryMenu : MonoBehaviour
{
    public Vector3 appearOffset;
    public Canvas menuCanvas;
    public float appearAlphaSpeed;
    public float appearScaleSpeed;
    public Transform primaryControllerTransform;

    public float cursorScaleNormal = 1f;
    public float cursorScaleHover = 0.3f;

    public Image cursorImage;

    public Effect hoverEffect;
    public Effect clickEffect;
    public Effect openMenuEffect;
    public Effect closeMenuEffect;


    private CanvasGroup _menuCanvasGroup;
    private Vector3 _menuOriginalScale;
    private Camera _main;
    private GraphicRaycaster _raycaster;
    
    
    private void Start()
    {
        _menuCanvasGroup = menuCanvas.GetComponent<CanvasGroup>();
        _raycaster = menuCanvas.GetComponent<GraphicRaycaster>();
        _menuCanvasGroup.alpha = 0;
        _menuOriginalScale = menuCanvas.transform.localScale;
        menuCanvas.transform.localScale = Vector3.zero;
        _main = Camera.main;
    }

    private List<RaycastResult> _raycastResults = new List<RaycastResult>(64);
    private PrimaryMenuButton _currentHover;
    
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
            openMenuEffect.PlayNew();
        }
        
        if (Input.GetButtonUp("XRI_Right_PrimaryButton"))
        {
            closeMenuEffect.PlayNew();
        }

        if (isMenuShown)
        {
            ProcessCursor();
            UpdateCursorScale();
        }
        else if (_currentHover != null)
        {
            _currentHover.OnCursorExit();
            _currentHover.OnCursorClick();
            clickEffect.PlayNew();
            _currentHover = null;
        }
    }

    private void ProcessCursor()
    {
        var canvasPlane = new Plane(menuCanvas.transform.forward, menuCanvas.transform.position);
        var closestPoint = canvasPlane.ClosestPointOnPlane(primaryControllerTransform.position +
                                                           menuCanvas.transform.rotation * appearOffset);
        cursorImage.transform.position = closestPoint;

        _raycastResults.Clear();
        _raycaster.Raycast(new PointerEventData(EventSystem.current)
        {
            pointerId = -1,
            position = _main.WorldToScreenPoint(closestPoint)
        }, _raycastResults);

        PrimaryMenuButton newButton = null;
        foreach (var obj in _raycastResults)
        {
            var btn = obj.gameObject.GetComponentInParent<PrimaryMenuButton>();
            if (btn == null) continue;
            newButton = btn;
            break;
        }

        if (_currentHover != null && newButton != _currentHover)
        {
            _currentHover.OnCursorExit();
        }

        if (newButton != _currentHover && newButton != null)
        {
            newButton.OnCursorEnter();
            hoverEffect.PlayNew();
        }
            
        _currentHover = newButton;
    }

    private float _cursorScaleCv;
    private void UpdateCursorScale()
    {
        cursorImage.transform.localScale = Mathf.SmoothDamp(cursorImage.transform.localScale.x,
            _currentHover != null ? cursorScaleHover : cursorScaleNormal, ref _cursorScaleCv, 0.1f) * Vector3.one;
    }
}
