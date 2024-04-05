using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

        public void Initialize(ControlsSettingsMenu.RebindableAction rebindableAction, InputControlScheme controlScheme)
        {
            inputActionDisplayText.text = rebindableAction.inputActionReference.action.name;
            bindingDisplayText.text = "";
            //rebindableAction.inputActionReference.action.expectedControlType;
            for (int bindingIndex = 0; bindingIndex < rebindableAction.inputActionReference.action.bindings.Count; bindingIndex++)
            {
                InputBinding binding = rebindableAction.inputActionReference.action.bindings[bindingIndex];
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
                    {
                        if (bindingDisplayText.text == "")
                        {
                            this.bindingIndex = bindingIndex;
                            bindingDisplayText.text += binding.ToDisplayString();
                        }
                        else
                        {
                            bindingDisplayText.text += " - " + binding.ToDisplayString();
                        }
                        break;
                    }
                }
            }

            this.rebindableAction = rebindableAction;
            this.controlScheme = controlScheme;

            Button.onClick.AddListener(delegate { StartRebind(rebindableAction, controlScheme); });
        }

        public void SetIsRebinding()
        {
            Button.interactable = false;
            bindingDisplayText.text = "[Waiting For Input]";
        }

        public void SetFinishedRebinding()
        {
            Button.interactable = true;
            Initialize(rebindableAction, controlScheme);
        }

        private void Awake()
        {
            Button = GetComponentInChildren<Button>();
        }

        private void StartRebind(ControlsSettingsMenu.RebindableAction rebindableAction, InputControlScheme controlScheme)
        {
            SetIsRebinding();

            rebindingOperation = rebindableAction.inputActionReference.action.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(operation => SetFinishedRebinding())
                .Start();
        }
    }
}