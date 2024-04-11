using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Vi.UI
{
    public class CustomOnScreenStick : MonoBehaviour
    {
        [SerializeField] private float movementRange = 125;
        [SerializeField] private bool shouldReposition;

        private PlayerInput playerInput;
        private Vector2 joystickOriginalAnchoredPosition;
        private void Start()
        {
            playerInput = transform.root.GetComponent<PlayerInput>();

            RectTransform rt = (RectTransform)transform.parent;
            joystickOriginalAnchoredPosition = rt.anchoredPosition;
        }

        private int interactingTouchId;
        private void Update()
        {
            if (EnhancedTouchSupport.enabled)
            {
                bool joystickMoving = false;
                RectTransform rt = (RectTransform)transform.parent;
                foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (touch.isTap) { continue; }

                    if (touch.startScreenPosition.x < Screen.width / 2f)
                    {
                        if (rt.anchoredPosition == joystickOriginalAnchoredPosition)
                        {
                            if (shouldReposition)
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
                            else // shouldn't reposition
                            {
                                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform.parent, touch.startScreenPosition))
                                {
                                    OnTouchDown(touch);
                                    interactingTouchId = touch.touchId;
                                }
                            }
                        }
                        else if (touch.touchId == interactingTouchId)
                        {
                            OnTouchDrag(touch);
                        }
                        joystickMoving = true;
                    }
                }
                if (!joystickMoving)
                {
                    rt.anchoredPosition = joystickOriginalAnchoredPosition;
                    OnTouchUp();
                }
            }
        }

        private void OnTouchDown(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            
        }

        private void OnTouchDrag(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {

        }

        private void OnTouchUp()
        {

        }
    }
}