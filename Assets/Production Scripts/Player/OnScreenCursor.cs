using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Vi.Utility;
using Unity.Netcode;

namespace Vi.Player
{
    [RequireComponent(typeof(Canvas))]
    public class OnScreenCursor : MonoBehaviour
    {
        [SerializeField] private RectTransform cursorTransform;
        private const float cursorSpeed = 1000;
        private const float scrollSpeed = 1000;

        private RectTransform canvasRectTransform;
        private PlayerInput controllerCursorPlayerInput;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            canvasRectTransform = (RectTransform)transform;
            controllerCursorPlayerInput = GetComponent<PlayerInput>();

            string rebinds = FasterPlayerPrefs.Singleton.GetString("Rebinds");
            controllerCursorPlayerInput.actions.LoadBindingOverridesFromJson(rebinds);
        }

        private Mouse virtualMouse;

        private void OnEnable()
        {
            if (virtualMouse == null)
            {
                virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
            }
            else if (!virtualMouse.added)
            {
                InputSystem.AddDevice(virtualMouse);
            }

            InputState.Change(virtualMouse.position, new Vector2(Screen.width / 2, Screen.height / 2));

            InputSystem.onAfterUpdate += UpdateMotion;
        }

        private void OnDisable()
        {
            InputSystem.onAfterUpdate -= UpdateMotion;
            InputSystem.RemoveDevice(virtualMouse);
        }

        private IEnumerator ReEnablePlayerInput(PlayerInput playerInput)
        {
            playerInput.enabled = false;
            yield return null;
            playerInput.enabled = true;
        }

        private bool wasUsingMainPlayerInput;
        private bool lastIsPressingButtonSouth;
        private void UpdateMotion()
        {
            PlayerInput mainPlayerInput = null;
            if (NetworkManager.Singleton)
            {
                NetworkObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (playerObject)
                {
                    mainPlayerInput = playerObject.GetComponent<PlayerInput>();
                }
            }

            if (mainPlayerInput != wasUsingMainPlayerInput)
            {
                controllerCursorPlayerInput.enabled = !mainPlayerInput;
                if (mainPlayerInput) { StartCoroutine(ReEnablePlayerInput(mainPlayerInput)); }
                wasUsingMainPlayerInput = mainPlayerInput;
            }

            bool isCursorOn = controllerCursorPlayerInput.currentControlScheme == "Gamepad";
            if (mainPlayerInput)
            {
                if (mainPlayerInput.currentActionMap == null)
                {
                    isCursorOn = false;
                }
                else
                {
                    isCursorOn = mainPlayerInput.currentControlScheme == "Gamepad" & mainPlayerInput.currentActionMap.name == "UI";
                }
            }
            cursorTransform.gameObject.SetActive(isCursorOn);

            Vector2 deltaValue;
            Vector2 newPosition;
            Vector2 deltaScroll;
            if (isCursorOn)
            {
                if (Cursor.visible) { Cursor.visible = false; }

                deltaValue = Gamepad.current.leftStick.ReadValue();
                deltaValue *= cursorSpeed * Time.deltaTime;
                newPosition = virtualMouse.position.ReadValue() + deltaValue;

                deltaScroll = Gamepad.current.rightStick.ReadValue();
                deltaScroll *= scrollSpeed * Time.deltaTime;
            }
            else // Center cursor if it's not on
            {
                if (!Cursor.visible & Cursor.lockState == CursorLockMode.None) { Cursor.visible = true; }

                newPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                deltaValue = newPosition - virtualMouse.position.ReadValue();

                deltaScroll = Vector2.zero;
            }

            newPosition.x = Mathf.Clamp(newPosition.x, 0, Screen.width); // TODO - add padding
            newPosition.y = Mathf.Clamp(newPosition.y, 0, Screen.height);

            InputState.Change(virtualMouse.position, newPosition);
            InputState.Change(virtualMouse.delta, deltaValue);
            InputState.Change(virtualMouse.scroll, deltaScroll);

            if (isCursorOn)
            {
                bool isPressingButtonSouth = Gamepad.current.buttonSouth.IsPressed();
                if (isPressingButtonSouth != lastIsPressingButtonSouth)
                {
                    virtualMouse.CopyState(out MouseState mouseState);
                    mouseState.WithButton(MouseButton.Left, isPressingButtonSouth);
                    InputState.Change(virtualMouse, mouseState);
                    lastIsPressingButtonSouth = isPressingButtonSouth;
                }
            }
            
            AnchorCursor(newPosition);
        }

        private void AnchorCursor(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, position, null, out Vector2 anchoredPosition);
            cursorTransform.anchoredPosition = anchoredPosition;
        }
    }
}