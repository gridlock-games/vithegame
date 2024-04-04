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

        public void Initialize(InputControlScheme controlScheme, InputAction inputAction)
        {
            inputActionDisplayText.text = inputAction.name;
            bindingDisplayText.text = "";

            foreach (InputBinding binding in inputAction.bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
                    {
                        bindingDisplayText.text = binding.ToDisplayString();
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }
        }

        private void Awake()
        {
            Button = GetComponentInChildren<Button>();
        }
    }
}