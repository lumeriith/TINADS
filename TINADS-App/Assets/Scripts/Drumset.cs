using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drumset : MonoBehaviour
{
    public float movementSpeed = 5f;
    public float rotationSpeed = 30f;
    public bool invertMovementVertical;
    public bool invertMovementHorizontal;
    public bool invertRotation;


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
        transform.Rotate(Vector3.up, rightInput.x * rotationSpeed * Time.deltaTime * (invertRotation ? -1 : 1));
        PreferencesManager.instance.drumSetPosition = transform.position;
        PreferencesManager.instance.drumSetRotation = transform.rotation;
    }
}
