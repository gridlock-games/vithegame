using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.EventSystems;
using Unity.Netcode;
using Vi.Core;

namespace Vi.Player
{
    [RequireComponent(typeof(Canvas))]
    public class OnScreenCursor : MonoBehaviour
    {
        [SerializeField] private RectTransform cursorTransform;
        private const float cursorSpeed = 1000;

        private RectTransform canvasRectTransform;
        private PlayerInput controllerCursorPlayerInput;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            canvasRectTransform = (RectTransform)transform;
            controllerCursorPlayerInput = GetComponent<PlayerInput>();
        }

        private Mouse virtualMouse;
        private bool previousMouseState;

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
        private void UpdateMotion()
        {
            PlayerInput mainPlayerInput = null;
            if (PlayerDataManager.Singleton)
            {
                var kvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                if (kvp.Value) { mainPlayerInput = kvp.Value.GetComponent<PlayerInput>(); }
            }

            if (mainPlayerInput != wasUsingMainPlayerInput)
            {
                controllerCursorPlayerInput.enabled = !mainPlayerInput;
                if (mainPlayerInput) { StartCoroutine(ReEnablePlayerInput(mainPlayerInput)); }
                Debug.LogWarning("Player input change - " + mainPlayerInput);

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
            if (isCursorOn)
            {
                if (Cursor.visible) { Cursor.visible = false; }

                deltaValue = Gamepad.current.leftStick.ReadValue();
                deltaValue *= cursorSpeed * Time.deltaTime;
                newPosition = virtualMouse.position.ReadValue() + deltaValue;
            }
            else // Center cursor if it's not on
            {
                if (!Cursor.visible & Cursor.lockState == CursorLockMode.None) { Cursor.visible = true; }

                newPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                deltaValue = newPosition - virtualMouse.position.ReadValue();
            }

            newPosition.x = Mathf.Clamp(newPosition.x, 0, Screen.width); // TODO - add padding
            newPosition.y = Mathf.Clamp(newPosition.y, 0, Screen.height);

            InputState.Change(virtualMouse.position, newPosition);
            InputState.Change(virtualMouse.delta, deltaValue);

            bool aButtonIsPressed = Gamepad.current.aButton.IsPressed();
            if (previousMouseState != aButtonIsPressed)
            {
                virtualMouse.CopyState(out MouseState mouseState);
                mouseState.WithButton(MouseButton.Left, aButtonIsPressed);
                InputState.Change(virtualMouse, mouseState);
                previousMouseState = aButtonIsPressed;
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