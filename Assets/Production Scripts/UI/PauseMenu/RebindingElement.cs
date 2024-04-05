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

        public void Initialize(ControlsSettingsMenu.RebindableAction rebindableAction, InputControlScheme controlScheme)
        {
            inputActionDisplayText.text = rebindableAction.inputActionReference.action.name;
            bindingDisplayText.text = "";
            //rebindableAction.inputActionReference.action.expectedControlType;
            foreach (InputBinding binding in rebindableAction.inputActionReference.action.bindings)
            {
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
                    {
                        if (bindingDisplayText.text == "")
                            bindingDisplayText.text += binding.ToDisplayString();
                        else
                            bindingDisplayText.text += " - " + binding.ToDisplayString();
                        break;
                    }
                }
            }

            this.rebindableAction = rebindableAction;
            this.controlScheme = controlScheme;
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
    }
}