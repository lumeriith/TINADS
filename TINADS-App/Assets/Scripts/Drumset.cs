using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drumset : SingletonBehaviour<Drumset>
{
    public float movementSpeed = 5f;
    public float rotationSpeed = 30f;
    public float scaleSpeed = 0.15f;
    public Vector2 scaleClampRange;
    public float metaphorMovementSensitivity = 2.5f;
    public Transform leftController;
    public Transform rightController;

    public bool invertMovementVertical;
    public bool invertMovementHorizontal;
    public bool invertRotation;
    public bool invertScaling;


    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        transform.SetPositionAndRotation(
            PreferencesManager.instance.drumSetPosition,
            PreferencesManager.instance.drumSetRotation
            );
    }

    private void Update()
    {
        if (GameManager.instance.currentTool != Tool.MoveDrumSet) return;
        ProcessPlacementByJoystick();
        ProcessPlacementByMetaphor();
        SaveDrumSetPlacement();
    }


    private Vector3 _leftLastPosition;
    private Quaternion _leftLastRotation;
    private Vector3 _rightLastPosition;
    private Quaternion _rightLastRotation;

    
    private void ProcessPlacementByMetaphor()
    {
        var isLeftHeld = Input.GetButton("XRI_Left_TriggerButton");
        var isRightHeld = Input.GetButton("XRI_Right_TriggerButton");

        if (isLeftHeld && isRightHeld)
            DoDoubleControllerControl();
        else if (isLeftHeld)
        {
            DoSingleControllerControl(leftController.position - _leftLastPosition, Quaternion.Inverse(_leftLastRotation) * leftController.rotation);
        }
        else if (isRightHeld)
        {
            DoSingleControllerControl(rightController.position - _rightLastPosition, Quaternion.Inverse(_rightLastRotation) * rightController.rotation);
        }
        
        _leftLastPosition = leftController.position;
        _leftLastRotation = leftController.rotation;
        _rightLastPosition = rightController.position;
        _rightLastRotation = rightController.rotation;
    }

    private void DoDoubleControllerControl()
    {
        var lastAvg = (_leftLastPosition + _rightLastPosition) / 2f;
        var currAvg = (leftController.position + rightController.position) / 2f;

        var avgDelta = currAvg - lastAvg;
        avgDelta.y = 0;
        avgDelta *= metaphorMovementSensitivity;
        transform.position += avgDelta;

        var lastVec = _leftLastPosition - _rightLastPosition;
        lastVec.y = 0;
        var currVec = leftController.position - rightController.position;
        currVec.y = 0;

        var angle = Vector3.SignedAngle(lastVec, currVec, Vector3.up);
        transform.rotation *= Quaternion.Euler(0, angle, 0);

        var lastDist = Vector3.Distance(_leftLastPosition, _rightLastPosition);
        var currDist = Vector3.Distance(leftController.position, rightController.position);
        transform.localScale = Mathf.Clamp(transform.localScale.x * currDist / lastDist, scaleClampRange.x,
            scaleClampRange.y) * Vector3.one;
    }

    private void DoSingleControllerControl(Vector3 deltaPos, Quaternion deltaRot)
    {
        var flatDelta = deltaPos;
        flatDelta.y = 0;
        flatDelta *= metaphorMovementSensitivity;
        transform.position += flatDelta;
    }

    private void ProcessPlacementByJoystick()
    {
        var leftInput = new Vector2(Input.GetAxis("XRI_Left_Primary2DAxis_Horizontal"),
            Input.GetAxis("XRI_Left_Primary2DAxis_Vertical"));
        var rightInput = new Vector2(Input.GetAxis("XRI_Right_Primary2DAxis_Horizontal"),
            Input.GetAxis("XRI_Right_Primary2DAxis_Vertical"));

        if (leftInput.sqrMagnitude < 0.01f && rightInput.sqrMagnitude < 0.01f) return;
        
        var flatForward = _mainCamera.transform.forward;
        var flatRight = _mainCamera.transform.right;
        flatForward.y = 0;
        flatRight.y = 0;
        flatForward.Normalize();
        flatRight.Normalize();
        
        transform.position += Time.deltaTime * movementSpeed * (flatRight * leftInput.x * (invertMovementHorizontal ? -1 : 1) + flatForward * leftInput.y * (invertMovementVertical ? -1 : 1));

        if (Mathf.Abs(rightInput.x) > 0.40f)
        {
            transform.Rotate(Vector3.up, rightInput.x * rotationSpeed * Time.deltaTime * (invertRotation ? -1 : 1));
        }

        if (Mathf.Abs(rightInput.y) > 0.40f)
        {
            var scale = transform.localScale.x;
            scale += Time.deltaTime * scaleSpeed * (invertScaling ? -1 : 1) * rightInput.y;
            scale = Mathf.Clamp(scale, scaleClampRange.x, scaleClampRange.y);
            transform.localScale = Vector3.one * scale;
        }
    }

    private void SaveDrumSetPlacement()
    {
        PreferencesManager.instance.drumSetPosition = transform.position;
        PreferencesManager.instance.drumSetRotation = transform.rotation;
        PreferencesManager.instance.drumSetScale = transform.localScale.x;
    }
}
