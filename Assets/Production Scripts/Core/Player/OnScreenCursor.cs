using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.LowLevel;

namespace Vi.Player
{
    [RequireComponent(typeof(Canvas))]
    public class OnScreenCursor : MonoBehaviour
    {
        [SerializeField] private RectTransform cursorTransform;
        private const float cursorSpeed = 1000;

        private RectTransform canvasRectTransform;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            canvasRectTransform = (RectTransform)transform;
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

            // TODO link to player input

            InputState.Change(virtualMouse.position, new Vector2(Screen.width / 2, Screen.height / 2));

            InputSystem.onAfterUpdate += UpdateMotion;
        }

        private void OnDisable()
        {
            InputSystem.RemoveDevice(virtualMouse);
            InputSystem.onAfterUpdate -= UpdateMotion;
        }

        private void UpdateMotion()
        {
            if (virtualMouse == null || Gamepad.current == null)
            {
                cursorTransform.gameObject.SetActive(false);
                return;
            }
            cursorTransform.gameObject.SetActive(true);

            // Delta position
            Vector2 deltaValue = Gamepad.current.leftStick.ReadValue();
            deltaValue *= cursorSpeed * Time.deltaTime;

            Vector2 currentPosition = virtualMouse.position.ReadValue();
            Vector2 newPosition = currentPosition + deltaValue;

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