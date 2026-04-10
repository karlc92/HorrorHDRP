using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FS_Atmo
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        public Camera playerCamera;
        public float walkSpeed = 1.15f;
        public float runSpeed = 4.0f;
        public float lookSpeed = 2.0f;
        public float lookXLimit = 60.0f;
        public float gravity = 150.0f;

        CharacterController characterController;
        Vector3 moveDirection = Vector3.zero;
        float rotationX = 0;
        private bool canMove = true;

        void Start()
        {
            characterController = GetComponent<CharacterController>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            bool isRunning = keyboard != null && keyboard.leftShiftKey.isPressed;
            float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * GetVerticalInput(keyboard) : 0f;
            float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * GetHorizontalInput(keyboard) : 0f;
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);
            moveDirection.y = movementDirectionY;

            if (!characterController.isGrounded)
                moveDirection.y -= gravity * Time.deltaTime;

            characterController.Move(moveDirection * Time.deltaTime);

            if (!canMove)
                return;

            Vector2 mouseDelta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
            rotationX += -mouseDelta.y * lookSpeed * Time.deltaTime;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, mouseDelta.x * lookSpeed * Time.deltaTime, 0);
        }

        float GetVerticalInput(Keyboard keyboard)
        {
            if (keyboard == null)
                return 0f;

            float input = 0f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                input += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                input -= 1f;

            return input;
        }

        float GetHorizontalInput(Keyboard keyboard)
        {
            if (keyboard == null)
                return 0f;

            float input = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                input += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                input -= 1f;

            return input;
        }
    }
}
