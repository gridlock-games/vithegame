using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Vi.Core;
using Vi.Utility;
using Vi.Player;
using Vi.Core.MovementHandlers;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CustomOnScreenStick : UIDeadZoneElement
    {
        [SerializeField] private JoystickActionType joystickActionType;
        [SerializeField] private float movementRange = 125;
        [SerializeField] private string shouldRepositionPlayerPref;
        [SerializeField] private bool shouldReposition;
        [SerializeField] private string joystickValueMultiplierPlayerPref;
        [SerializeField] private float joystickValueMultiplier = 1;
        [SerializeField] private string actLikeButtonPlayerPref;
        [SerializeField] private RectTransform limits;
        [SerializeField] private Image limitsImage;
        [SerializeField] private Image stickImage;

        private bool shouldRepositionPlayerPrefValue;

        private bool ShouldReposition { get { return shouldReposition & shouldRepositionPlayerPrefValue; } }

        private bool actLikeButtonPlayerPrefValue;

        private bool ShouldActLikeButton { get { return actLikeButtonPlayerPrefValue; } }

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
        private PlayerInput playerInput;
        private CombatAgent combatAgent;
        private Canvas canvas;
        private void Start()
        {
            movementHandler = transform.root.GetComponent<MovementHandler>();
            playerInput = movementHandler.GetComponent<PlayerInput>();
            combatAgent = movementHandler.GetComponent<CombatAgent>();
            canvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        private void RefreshStatus()
        {
            // Need to check has in case it is the pref name is null
            if (FasterPlayerPrefs.Singleton.HasFloat(joystickValueMultiplierPlayerPref)) { joystickValueMultiplier = FasterPlayerPrefs.Singleton.GetFloat(joystickValueMultiplierPlayerPref); }
            if (FasterPlayerPrefs.Singleton.HasBool(shouldRepositionPlayerPref)) { shouldRepositionPlayerPrefValue = FasterPlayerPrefs.Singleton.GetBool(shouldRepositionPlayerPref); }

            bool wasChanged = false;
            if (FasterPlayerPrefs.Singleton.HasBool(actLikeButtonPlayerPref))
            {
                bool newVal = FasterPlayerPrefs.Singleton.GetBool(actLikeButtonPlayerPref);
                if (actLikeButtonPlayerPrefValue != newVal)
                {
                    wasChanged = true;
                }
                actLikeButtonPlayerPrefValue = newVal;
            }
        
            if (wasChanged)
            {
                if (actLikeButtonPlayerPrefValue)
                {
                    stickImage.raycastTarget = true;
                    limitsImage.raycastTarget = false;
                    limitsImage.enabled = false;
                    stickImage.transform.SetParent(limits, true);
                    stickImage.rectTransform.anchoredPosition = Vector2.zero;
                    IsDeadZone = false;
                }
                else
                {
                    stickImage.raycastTarget = false;
                    limitsImage.raycastTarget = true;
                    limitsImage.enabled = true;
                    stickImage.transform.SetParent(limits.parent, true);
                    stickImage.rectTransform.anchoredPosition = Vector2.zero;
                    IsDeadZone = true;
                }
            }
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
        }

        private void OnEnable()
        {
            RectTransform rt = (RectTransform)transform.parent;
            joystickParentOriginalAnchoredPosition = rt.anchoredPosition;

            rt = (RectTransform)transform;
            joystickOriginalAnchoredPosition = rt.anchoredPosition;

            RefreshStatus();
            InputSystem.onBeforeUpdate += UpdateJoystick;
        }

        private void OnDisable()
        {
            RectTransform rt = (RectTransform)transform.parent;
            rt.anchoredPosition = joystickParentOriginalAnchoredPosition;

            rt = (RectTransform)transform;
            rt.anchoredPosition = joystickOriginalAnchoredPosition;

            InputSystem.onBeforeUpdate -= UpdateJoystick;
        }

        private int interactingTouchId = -1;
        private void UpdateJoystick()
        {
            if (actLikeButtonPlayerPrefValue) { return; }

            if (EnhancedTouchSupport.enabled)
            {
                bool joystickMoving = false;
                RectTransform rt = (RectTransform)transform.parent;
                foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (interactingTouchId != -1 & touch.touchId != interactingTouchId) { continue; }

                    if (ShouldReposition)
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
                                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, touch.startScreenPosition, canvas.worldCamera, out Vector2 localPoint);
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
                            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform.parent, touch.startScreenPosition, canvas.worldCamera))
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

                if (playerInput.currentControlScheme == "Touchscreen")
                {
                    switch (joystickActionType)
                    {
                        case JoystickActionType.Move:
                            movementHandler.SetMoveInput(GetJoystickValue());
                            break;
                        case JoystickActionType.Look:
                            movementHandler.SetLookInput(GetJoystickValue() * (combatAgent ? (combatAgent.StatusAgent.IsFeared() ? -1 : 1) : 1));
                            break;
                        default:
                            Debug.LogError("Not sure how to handle joystick action type - " + joystickActionType);
                            break;
                    }
                }
            }
        }

        private void OnTouchDown(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            RectTransform rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(limits, touch.screenPosition, canvas.worldCamera, out Vector2 localPoint);
            rt.anchoredPosition = Vector2.ClampMagnitude(localPoint - limits.sizeDelta / 2, movementRange);
        }

        private void OnTouchDrag(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            RectTransform rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(limits, touch.screenPosition, canvas.worldCamera, out Vector2 localPoint);
            rt.anchoredPosition = Vector2.ClampMagnitude(localPoint - limits.sizeDelta / 2, movementRange);
        }

        private void OnTouchUp()
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchoredPosition = joystickOriginalAnchoredPosition;
        }
    }
}