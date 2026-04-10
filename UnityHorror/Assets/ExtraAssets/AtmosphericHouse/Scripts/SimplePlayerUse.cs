using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FS_Atmo
{
    public class SimplePlayerUse : MonoBehaviour
    {
        public GameObject mainCamera;
        private GameObject objectClicked;
        public GameObject flashlight;
        public KeyCode OpenClose;
        public KeyCode Flashlight;

        void Start()
        {
        }

        void Update()
        {
            if (WasPressedThisFrame(OpenClose)) // Open and close action
                RaycastCheck();

            if (!WasPressedThisFrame(Flashlight)) // Toggle flashlight
                return;

            flashlight.SetActive(!flashlight.activeSelf);
        }

        void RaycastCheck()
        {
            RaycastHit hit;

            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward), out hit, 2.3f))
            {
                if (hit.collider.gameObject.GetComponent<SimpleOpenClose>())
                {
                    // Debug.Log("Object with SimpleOpenClose script found");
                    hit.collider.gameObject.BroadcastMessage("ObjectClicked");
                }
                else
                {
                    // Debug.Log("Object doesn't have script SimpleOpenClose attached");
                }
                // Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                // Debug.Log("Did Hit");
            }
            else
            {
                // Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                //   Debug.Log("Did not Hit");
            }
        }

        bool WasPressedThisFrame(KeyCode keyCode)
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            switch (keyCode)
            {
                case KeyCode.Mouse0:
                    return mouse != null && mouse.leftButton.wasPressedThisFrame;
                case KeyCode.Mouse1:
                    return mouse != null && mouse.rightButton.wasPressedThisFrame;
                case KeyCode.Mouse2:
                    return mouse != null && mouse.middleButton.wasPressedThisFrame;
                case KeyCode.Space:
                    return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    return keyboard != null && keyboard.enterKey.wasPressedThisFrame;
                case KeyCode.Tab:
                    return keyboard != null && keyboard.tabKey.wasPressedThisFrame;
                case KeyCode.Escape:
                    return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
                case KeyCode.BackQuote:
                    return keyboard != null && keyboard.backquoteKey.wasPressedThisFrame;
                case KeyCode.LeftShift:
                    return keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame;
                case KeyCode.RightShift:
                    return keyboard != null && keyboard.rightShiftKey.wasPressedThisFrame;
                case KeyCode.LeftControl:
                    return keyboard != null && keyboard.leftCtrlKey.wasPressedThisFrame;
                case KeyCode.RightControl:
                    return keyboard != null && keyboard.rightCtrlKey.wasPressedThisFrame;
                case KeyCode.LeftAlt:
                    return keyboard != null && keyboard.leftAltKey.wasPressedThisFrame;
                case KeyCode.RightAlt:
                    return keyboard != null && keyboard.rightAltKey.wasPressedThisFrame;
                case KeyCode.UpArrow:
                    return keyboard != null && keyboard.upArrowKey.wasPressedThisFrame;
                case KeyCode.DownArrow:
                    return keyboard != null && keyboard.downArrowKey.wasPressedThisFrame;
                case KeyCode.LeftArrow:
                    return keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame;
                case KeyCode.RightArrow:
                    return keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame;
            }

            if (keyboard == null)
                return false;

            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                string keyName = ((char)('A' + (keyCode - KeyCode.A))).ToString();
                if (System.Enum.TryParse(keyName, out Key key))
                    return keyboard[key].wasPressedThisFrame;
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                string keyName = "Digit" + (keyCode - KeyCode.Alpha0);
                if (System.Enum.TryParse(keyName, out Key key))
                    return keyboard[key].wasPressedThisFrame;
            }

            return false;
        }
    }
}
