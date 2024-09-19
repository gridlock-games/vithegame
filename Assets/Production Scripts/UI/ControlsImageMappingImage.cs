using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Core.CombatAgents;

namespace Vi.UI
{
    [RequireComponent(typeof(Image))]
    public class ControlsImageMappingImage : MonoBehaviour
    {
        [SerializeField] private InputActionReference inputAction;

        private Image image;
        private void Start()
        {
            image = GetComponent<Image>();
            FindPlayerInput();
            UpdateImage();
        }

        private void Update()
        {
            FindPlayerInput();
            UpdateImage();
        }

        private string lastEvaluatedControlScheme;
        private void UpdateImage()
        {
            if (!playerInput) { return; }
            if (string.IsNullOrEmpty(playerInput.currentControlScheme)) { return; }
            if (playerInput.currentControlScheme == lastEvaluatedControlScheme) { return; }

            InputControlScheme controlScheme = playerInput.actions.FindControlScheme(playerInput.currentControlScheme).Value;

            var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { inputAction.action });
            image.sprite = result.releasedSprites.Count > 0 ? result.releasedSprites[0] : null;

            lastEvaluatedControlScheme = playerInput.currentControlScheme;
        }

        PlayerInput playerInput;
        private void FindPlayerInput()
        {
            if (playerInput)
            {
                if (!playerInput.gameObject.activeInHierarchy) { playerInput = null; }
            }

            if (playerInput) { return; }
            if (!PlayerDataManager.DoesExist()) { return; }
            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer) { playerInput = localPlayer.GetComponent<PlayerInput>(); }
        }
    }
}