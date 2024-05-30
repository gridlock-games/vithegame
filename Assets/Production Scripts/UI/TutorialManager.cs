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
        [SerializeField] private Image overlayImage;
        [SerializeField] private TutorialAction[] tutorialActions;

        [System.Serializable]
        private struct TutorialAction
        {
            public string actionName;
            public List<string> controlSchemesToAnimateOn;
        }

        PlayerInput playerInput;
        private void FindPlayerInput()
        {
            if (playerInput) { return; }
            if (!PlayerDataManager.DoesExist()) { return; }
            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer) { playerInput = localPlayer.GetComponent<PlayerInput>(); }
        }

        RectTransform overlayImageRT;
        private Vector2 originalAnchoredPosition;
        private void Awake()
        {
            FindPlayerInput();
            FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
            originalAnchoredPosition = ((RectTransform)overlayImage.transform).anchoredPosition;
            DisplayNextAction();
            overlayImageRT = (RectTransform)overlayImage.transform;
        }

        private const float minDisplayTime = 5;

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

        private void Update()
        {
            FindPlayerInput();

            if (!playerInput) { return; }
            if (string.IsNullOrWhiteSpace(playerInput.currentControlScheme)) { return; }

            InputControlScheme controlScheme = playerInput.actions.FindControlScheme(playerInput.currentControlScheme).Value;
            var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, playerInput.actions[tutorialActions[currentActionIndex].actionName]);

            overlayImage.sprite = result.sprite;
            overlayImage.preserveAspect = true;

            if (tutorialActions[currentActionIndex].controlSchemesToAnimateOn.Contains(playerInput.currentControlScheme))
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
    }
}