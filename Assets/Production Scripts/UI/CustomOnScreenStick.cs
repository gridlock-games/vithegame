using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Vi.Core;

namespace Vi.UI
{
    public class CustomOnScreenStick : UIDeadZoneElement
    {
        [SerializeField] private JoystickActionType joystickActionType;
        [SerializeField] private float movementRange = 125;
        [SerializeField] private bool shouldReposition;
        [SerializeField] private float joystickValueMultiplier = 1;
        [SerializeField] private RectTransform limits;

        public enum JoystickActionType
        {
            Move,
            Look
        }

        public Vector2 GetJoystickValue()
        {
            RectTransform rt = (RectTransform)transform;
            return Vector2.ClampMagnitude(new Vector2(rt.anchoredPosition.x / movementRange, rt.anchoredPosition.y / movementRange), 1) * joystickValueMultiplier;
        }

        private Vector2 joystickParentOriginalAnchoredPosition;
        private Vector2 joystickOriginalAnchoredPosition;
        private MovementHandler movementHandler;
        private void Start()
        {
            RectTransform rt = (RectTransform)transform.parent;
            joystickParentOriginalAnchoredPosition = rt.anchoredPosition;

            rt = (RectTransform)transform;
            joystickOriginalAnchoredPosition = rt.anchoredPosition;

            movementHandler = transform.root.GetComponent<MovementHandler>();
        }

        private void OnEnable()
        {
            InputSystem.onBeforeUpdate += UpdateJoystick;
        }

        private void OnDisable()
        {
            InputSystem.onBeforeUpdate -= UpdateJoystick;
        }

        private int interactingTouchId = -1;
        private void UpdateJoystick()
        {
            if (EnhancedTouchSupport.enabled)
            {
                bool joystickMoving = false;
                RectTransform rt = (RectTransform)transform.parent;
                foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (interactingTouchId != -1 & touch.touchId != interactingTouchId) { continue; }

                    if (shouldReposition)
                    {
                        if (touch.startScreenPosition.x > Screen.width / 2f) { continue; }

                        if (rt.anchoredPosition == joystickParentOriginalAnchoredPosition)
                        {
                            List<RaycastResult> raycastResults = new List<RaycastResult>();
                            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
                            pointerEventData.position = touch.screenPosition;

                            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
                            raycastResults.RemoveAll(item => item.gameObject.transform.IsChildOf(transform.parent));

                            if (raycastResults.Count == 0)
                            {
                                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, touch.startScreenPosition, null, out Vector2 localPoint);
                                rt.anchoredPosition = localPoint - new Vector2(movementRange / 2, movementRange / 2);
                                OnTouchDown(touch);
                                interactingTouchId = touch.touchId;
                            }
                        }
                        else
                        {
                            OnTouchDrag(touch);
                        }
                        joystickMoving = true;
                    }
                    else // should not reposition
                    {
                        if (interactingTouchId == -1)
                        {
                            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform.parent, touch.startScreenPosition))
                            {
                                OnTouchDown(touch);
                                interactingTouchId = touch.touchId;
                            }
                        }
                        else
                        {
                            OnTouchDrag(touch);
                        }
                        joystickMoving = true;
                    }
                }

                if (!joystickMoving)
                {
                    rt.anchoredPosition = joystickParentOriginalAnchoredPosition;
                    interactingTouchId = -1;
                    OnTouchUp();
                }

                switch (joystickActionType)
                {
                    case JoystickActionType.Move:
                        movementHandler.SetMoveInput(GetJoystickValue());
                        break;
                    case JoystickActionType.Look:
                        movementHandler.SetLookInput(GetJoystickValue());
                        break;
                    default:
                        Debug.LogError("Not sure how to handle joystick action type - " + joystickActionType);
                        break;
                }
            }
        }

        private void OnTouchDown(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            RectTransform rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(limits, touch.screenPosition, null, out Vector2 localPoint);
            rt.anchoredPosition = Vector2.ClampMagnitude(localPoint - limits.sizeDelta / 2, movementRange);
        }

        private void OnTouchDrag(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            RectTransform rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(limits, touch.screenPosition, null, out Vector2 localPoint);
            rt.anchoredPosition = Vector2.ClampMagnitude(localPoint - limits.sizeDelta / 2, movementRange);
        }

        private void OnTouchUp()
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchoredPosition = joystickOriginalAnchoredPosition;
        }
    }
}