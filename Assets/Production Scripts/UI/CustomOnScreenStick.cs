using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Vi.Core;
using UnityEngine.InputSystem;

namespace Vi.UI
{
    public class CustomOnScreenStick : UIDeadZoneElement
    {
        [SerializeField] private float movementRange = 125;
        [SerializeField] private bool shouldReposition;

        private Vector2 joystickParentOriginalAnchoredPosition;
        private Vector2 joystickOriginalAnchoredPosition;
        private void Start()
        {
            RectTransform rt = (RectTransform)transform.parent;
            joystickParentOriginalAnchoredPosition = rt.anchoredPosition;

            rt = (RectTransform)transform;
            joystickOriginalAnchoredPosition = rt.anchoredPosition;
        }

        private void OnEnable()
        {
            InputSystem.onAfterUpdate += UpdateTouches;
        }

        private void OnDisable()
        {
            InputSystem.onAfterUpdate -= UpdateTouches;
        }

        private int interactingTouchId = -1;
        private void UpdateTouches()
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
            }
        }

        private void OnTouchDown(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            RectTransform rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, touch.screenPosition, null, out Vector2 localPoint);
            rt.anchoredPosition = Vector2.ClampMagnitude(localPoint, movementRange);
        }

        private void OnTouchDrag(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            RectTransform rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, touch.screenPosition, null, out Vector2 localPoint);
            rt.anchoredPosition = Vector2.ClampMagnitude(localPoint, movementRange);
        }

        private void OnTouchUp()
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchoredPosition = joystickOriginalAnchoredPosition;
        }
    }
}