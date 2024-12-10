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
        [SerializeField] private Sprite defaultSprite;

        private Image image;
        private void Start()
        {
            image = GetComponent<Image>();
            FindPlayerInput();
            UpdateImage(false);
        }

        private void Update()
        {
            FindPlayerInput();
            UpdateImage(false);
        }

        private void OnEnable()
        {
            UpdateImage(true);
        }

        private string lastEvaluatedControlScheme;
        private void UpdateImage(bool force)
        {
            if (!playerInput) { return; }
            if (string.IsNullOrEmpty(playerInput.currentControlScheme)) { return; }

            if (!force)
            {
                if (playerInput.currentControlScheme == lastEvaluatedControlScheme) { return; }
            }
            
            InputControlScheme controlScheme = playerInput.actions.FindControlScheme(playerInput.currentControlScheme).Value;

            List<Sprite> controlSchemeSpriteList = PlayerDataManager.Singleton.GetControlsImageMapping().GetControlSchemeActionImages(controlScheme, inputAction.action);
            image.sprite = null;
            foreach (Sprite sprite in controlSchemeSpriteList)
            {
                if (sprite)
                {
                    image.sprite = sprite;
                    break;
                }
            }
            
            if (!image.sprite)
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { inputAction.action });
                image.sprite = defaultSprite;
                foreach (Sprite sprite in result.releasedSprites)
                {
                    if (sprite)
                    {
                        image.sprite = sprite;
                        break;
                    }
                }
            }

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