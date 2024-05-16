using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Vi.Core;

namespace Vi.UI
{
    public class RebindingElement : MonoBehaviour
    {
        [SerializeField] private Text inputActionDisplayText;
        [SerializeField] private Text bindingDisplayText;

        private Button button;

        private PlayerInput playerInput;
        private ControlsSettingsMenu.RebindableAction rebindableAction;
        private InputControlScheme controlScheme;
        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        private int bindingIndex = -1;

        public void Initialize(PlayerInput playerInput, ControlsSettingsMenu.RebindableAction rebindableAction, InputControlScheme controlScheme, int bindingIndex)
        {
            if (string.IsNullOrWhiteSpace(rebindableAction.overrideActionName))
            {
                inputActionDisplayText.text = rebindableAction.inputActionReferences[0].action.name;
            }
            else
            {
                inputActionDisplayText.text = rebindableAction.overrideActionName;
            }

            bindingDisplayText.text = "[Not Bound]";
            
            InputBinding binding = rebindableAction.inputActionReferences[0].action.bindings[bindingIndex];
            if (!string.IsNullOrWhiteSpace(binding.name))
            {
                switch (binding.name)
                {
                    case "up":
                        inputActionDisplayText.text += " Forward";
                        break;
                    case "down":
                        inputActionDisplayText.text += " Backward";
                        break;
                    case "left":
                        inputActionDisplayText.text += " Left";
                        break;
                    case "right":
                        inputActionDisplayText.text += " Right";
                        break;
                    default:
                        Debug.Log("Unsure what to display for binding name " + binding.name);
                        break;
                }
            }

            foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
            {
                string deviceName = device.name.ToLower();
                deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                if (binding.path.ToLower().Contains(deviceName.ToLower()))
                {
                    if (bindingDisplayText.text == "[Not Bound]")
                    {
                        bindingDisplayText.text = binding.ToDisplayString();
                    }
                    else
                    {
                        bindingDisplayText.text += " - " + binding.ToDisplayString();
                    }
                    break;
                }
            }

            this.playerInput = playerInput;
            this.rebindableAction = rebindableAction;
            this.controlScheme = controlScheme;
            this.bindingIndex = bindingIndex;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { StartRebind(); });
        }

        public void SetIsRebinding()
        {
            button.interactable = false;
            bindingDisplayText.text = "[Waiting For Input]";
        }

        public void SetFinishedRebinding()
        {
            rebindingOperation.Dispose();
            button.interactable = true;

            string originalPath = rebindableAction.inputActionReferences[0].action.bindings[bindingIndex].path;
            string overridePath = rebindableAction.inputActionReferences[0].action.bindings[bindingIndex].overridePath;
            for (int i = 0; i < rebindableAction.inputActionReferences.Length; i++)
            {
                for (int bindingIndex = 0; bindingIndex < rebindableAction.inputActionReferences[i].action.bindings.Count; bindingIndex++)
                {
                    InputBinding binding = rebindableAction.inputActionReferences[i].action.bindings[bindingIndex];
                    if (binding.path == originalPath)
                    {
                        playerInput.actions.FindAction(rebindableAction.inputActionReferences[i].action.id).ApplyBindingOverride(bindingIndex, overridePath);
                        rebindableAction.inputActionReferences[i].action.ApplyBindingOverride(bindingIndex, overridePath);
                    }
                }
            }

            string rebinds = playerInput.actions.SaveBindingOverridesAsJson();
            PersistentLocalObjects.Singleton.SetString("Rebinds", rebinds);

            Initialize(playerInput, rebindableAction, controlScheme, bindingIndex);

            PlayerUI playerUI = playerInput.GetComponentInChildren<PlayerUI>(true);
            if (playerUI) { playerUI.OnRebinding(); }
        }

        private void Awake()
        {
            button = GetComponentInChildren<Button>();
        }

        private void StartRebind()
        {
            SetIsRebinding();

            rebindingOperation = rebindableAction.inputActionReferences[0].action.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(operation => SetFinishedRebinding())
                .OnCancel(operation => SetFinishedRebinding())
                .Start();
        }
    }
}