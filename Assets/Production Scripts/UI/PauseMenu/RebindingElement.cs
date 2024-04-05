using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Text.RegularExpressions;

namespace Vi.UI
{
    public class RebindingElement : MonoBehaviour
    {
        [SerializeField] private Text inputActionDisplayText;
        [SerializeField] private Text bindingDisplayText;

        public Button Button { get; private set; }

        private ControlsSettingsMenu.RebindableAction rebindableAction;
        private InputControlScheme controlScheme;
        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        private int bindingIndex = -1;

        public void Initialize(ControlsSettingsMenu.RebindableAction rebindableAction, InputControlScheme controlScheme, int bindingIndex)
        {
            if (string.IsNullOrWhiteSpace(rebindableAction.overrideActionName))
            {
                inputActionDisplayText.text = rebindableAction.inputActionReference[0].action.name;
            }
            else
            {
                inputActionDisplayText.text = rebindableAction.overrideActionName;
            }

            bindingDisplayText.text = "Not Bound";

            InputBinding binding = rebindableAction.inputActionReference[0].action.bindings[bindingIndex];
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
                if (binding.path.ToLower().Contains(device.name.ToLower()))
                {
                    if (bindingDisplayText.text == "Not Bound")
                    {
                        bindingDisplayText.text += binding.ToDisplayString();
                    }
                    else
                    {
                        bindingDisplayText.text += " - " + binding.ToDisplayString();
                    }
                    break;
                }
            }

            this.rebindableAction = rebindableAction;
            this.controlScheme = controlScheme;
            this.bindingIndex = bindingIndex;

            Button.onClick.AddListener(delegate { StartRebind(); });
        }

        public void SetIsRebinding()
        {
            Button.interactable = false;
            bindingDisplayText.text = "[Waiting For Input]";
        }

        public void SetFinishedRebinding()
        {
            Button.interactable = true;
            Initialize(rebindableAction, controlScheme, bindingIndex);

            rebindingOperation.Dispose();
        }

        private void Awake()
        {
            Button = GetComponentInChildren<Button>();
        }

        private void StartRebind()
        {
            SetIsRebinding();

            rebindingOperation = rebindableAction.inputActionReference[0].action.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(operation => SetFinishedRebinding())
                .Start();
        }
    }
}