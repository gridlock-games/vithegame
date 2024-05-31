using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Utility;
using UnityEngine.UI;

namespace Vi.UI
{
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] private Text overlayText;
        [SerializeField] private Image overlayImage;

        PlayerInput playerInput;
        MovementHandler movementHandler;
        private void FindPlayerInput()
        {
            if (playerInput) { return; }
            if (!PlayerDataManager.DoesExist()) { return; }
            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer)
            {
                playerInput = localPlayer.GetComponent<PlayerInput>();
                movementHandler = localPlayer.GetComponent<MovementHandler>();
            }
        }

        RectTransform overlayImageRT;
        private Vector2 originalAnchoredPosition;
        private void Awake()
        {
            FindPlayerInput();
            FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
            originalAnchoredPosition = ((RectTransform)overlayImage.transform).anchoredPosition;
            overlayImageRT = (RectTransform)overlayImage.transform;
            DisplayNextAction();
        }

        private const float animationSpeed = 100;

        private float maxOffset = 100;

        private float positionOffset;
        private float directionMultiplier = -1;

        private int currentActionIndex = -1;
        private void DisplayNextAction()
        {
            currentActionIndex += 1;
            overlayImageRT.anchoredPosition = originalAnchoredPosition;
        }

        private string currentOverlayMessage;
        private Sprite currentOverlaySprite;
        private bool shouldAnimate;

        private void Update()
        {
            FindPlayerInput();
            CheckTutorialActionStatus();

            if (!playerInput) { return; }
            if (string.IsNullOrWhiteSpace(playerInput.currentControlScheme)) { return; }

            overlayText.text = currentOverlayMessage;

            overlayImage.sprite = currentOverlaySprite;
            overlayImage.preserveAspect = true;

            if (shouldAnimate)
            {
                if (Mathf.Abs(positionOffset) >= maxOffset) { directionMultiplier *= -1; }

                float amount = Time.deltaTime * animationSpeed * directionMultiplier;
                positionOffset = Mathf.Clamp(positionOffset + amount, -maxOffset, maxOffset);

                overlayImageRT.anchoredPosition += new Vector2(amount, 0);
            }
            else
            {
                overlayImageRT.anchoredPosition = originalAnchoredPosition;
            }
        }

        private const float minDisplayTime = 5;

        private bool canProceed;
        private float actionChangeTime;
        private int lastActionIndex = -1;
        private void CheckTutorialActionStatus()
        {
            //InputControlScheme controlScheme = playerInput.actions.FindControlScheme(playerInput.currentControlScheme).Value;

            if (lastActionIndex != currentActionIndex)
            {
                canProceed = false;
                actionChangeTime = Time.time;
            }

            if (currentActionIndex == 0) // Look
            {
                currentOverlayMessage = "Look Around.";
                //var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, playerInput.actions["Look"]);
                //currentOverlaySprite = result.sprite;

                if (movementHandler)
                {
                    canProceed = movementHandler.GetLookInput() != Vector2.zero | canProceed;

                    if (canProceed)
                    {
                        if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                    }
                }
            }
            else if (currentActionIndex == 1) // Move
            {
                currentOverlayMessage = "Move To The Enemy.";
            }
            else
            {
                Debug.LogError("Unsure how to handle current action index of " + currentActionIndex);
            }
            lastActionIndex = currentActionIndex;
        }
    }
}