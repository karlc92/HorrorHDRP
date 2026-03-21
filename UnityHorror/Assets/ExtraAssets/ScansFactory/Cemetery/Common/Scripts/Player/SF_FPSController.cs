using UnityEngine;
using UnityEngine.InputSystem;

namespace ScansFactory
{
    [RequireComponent(typeof(CharacterController))]
    public class SF_FPSController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float WalkSpeed;
        public float RunSpeed;
        public float FlySpeed;
        public float JumpSpeed;
        public float Gravity;

        [Header("Mouse Look Settings")]
        public float LookSpeed;
        public float LookXLimit;

        [Header("Extra")]
        public GameObject Flashlight;

        private CharacterController characterController;
        private Vector3 moveDirection = Vector3.zero;
        private float rotationX = 0;
        private bool noClip = false;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (Keyboard.current.capsLockKey.wasPressedThisFrame)
            {
                noClip = !noClip;
                characterController.enabled = !noClip;
            }

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                Flashlight.SetActive(!Flashlight.activeSelf);
            }

            MouseLook();

            if (noClip)
                FlyMovement();
            else
                Movement();
        }

        void Movement()
        {
            float speed = Keyboard.current.leftShiftKey.isPressed ? RunSpeed : WalkSpeed;

            Vector2 input = Vector2.zero;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.wKey.isPressed) input.y += 1;

            Vector3 move = transform.right * input.x + transform.forward * input.y;
            move = move.normalized * speed;

            if (characterController.isGrounded)
            {
                moveDirection = move;

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    moveDirection.y = JumpSpeed;
                else
                    moveDirection.y = -Gravity * Time.deltaTime;
            }
            else
            {
                moveDirection.x = move.x;
                moveDirection.z = move.z;
                moveDirection.y -= Gravity * Time.deltaTime;
            }

            characterController.Move(moveDirection * Time.deltaTime);
        }

        void FlyMovement()
        {
            float speed = FlySpeed * Time.deltaTime;

            float inputX = 0;
            float inputY = 0;
            float inputZ = 0;

            if (Keyboard.current.aKey.isPressed) inputX -= 1;
            if (Keyboard.current.dKey.isPressed) inputX += 1;
            if (Keyboard.current.sKey.isPressed) inputY -= 1;
            if (Keyboard.current.wKey.isPressed) inputY += 1;

            inputX *= speed;
            inputY *= speed;

            if (Keyboard.current.spaceKey.isPressed || Keyboard.current.eKey.isPressed)
                inputZ = speed;
            else if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.qKey.isPressed)
                inputZ = -speed;

            Vector3 move = transform.right * inputX + transform.forward * inputY + transform.up * inputZ;
            transform.position += move;
        }

        void MouseLook()
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            rotationX += -mouseDelta.y * LookSpeed * Time.deltaTime;
            rotationX = Mathf.Clamp(rotationX, -LookXLimit, LookXLimit);

            Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, mouseDelta.x * LookSpeed * Time.deltaTime, 0);
        }
    }
}