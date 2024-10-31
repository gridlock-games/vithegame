using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Vi.Utility;
using System.Linq;

namespace Vi.UI
{
    public class RebindingElement : MonoBehaviour
    {
        [SerializeField] private Text inputActionDisplayText;
        [SerializeField] private Text bindingDisplayText;

        private Button button;

        private PlayerInput playerInput;
        private InputAction pauseAction;
        private ControlsSettingsMenu.RebindableAction rebindableAction;
        private InputControlScheme controlScheme;
        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        private int bindingIndex = -1;

        public void Initialize(PlayerInput playerInput, ControlsSettingsMenu.RebindableAction rebindableAction, InputControlScheme controlScheme, int bindingIndex)
        {
            if (string.IsNullOrWhiteSpace(rebindableAction.overrideActionName))
            {
                inputActionDisplayText.text = rebindableAction.inputActionReferences[0].action.name.ToUpper();
            }
            else
            {
                inputActionDisplayText.text = rebindableAction.overrideActionName.ToUpper();
            }

            bindingDisplayText.text = "[NOT BOUND]";
            
            InputBinding binding = rebindableAction.inputActionReferences[0].action.bindings[bindingIndex];
            if (!string.IsNullOrWhiteSpace(binding.name))
            {
                switch (binding.name)
                {
                    case "up":
                        inputActionDisplayText.text += " FORWARD";
                        break;
                    case "down":
                        inputActionDisplayText.text += " BACKWARD";
                        break;
                    case "left":
                        inputActionDisplayText.text += " LEFT";
                        break;
                    case "right":
                        inputActionDisplayText.text += " RIGHT";
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
                    if (bindingDisplayText.text == "[NOT BOUND]")
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
            pauseAction = playerInput.actions.FindAction("Pause");

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
            FasterPlayerPrefs.Singleton.SetString("Rebinds", rebinds);

            Initialize(playerInput, rebindableAction, controlScheme, bindingIndex);

            PlayerUI playerUI = playerInput.GetComponentInChildren<PlayerUI>(true);
            if (playerUI) { playerUI.OnRebinding(); }
        }

        public void FilterCandidates()
        {
            if (rebindingOperation.scores.Count == 0) { return; }

            float maxScore = rebindingOperation.scores.Max();
            List<InputControl> candidatesToRemove = new List<InputControl>();
            List<InputControl> candidatesThatHaveMaxScore = new List<InputControl>();
            for (int i = 0; i < rebindingOperation.scores.Count; i++)
            {
                if (rebindingOperation.scores[i] == maxScore)
                {
                    candidatesThatHaveMaxScore.Add(rebindingOperation.candidates[i]);
                }
                else
                {
                    candidatesToRemove.Add(rebindingOperation.candidates[i]);
                }
            }

            int minChildren = candidatesThatHaveMaxScore.Min(item => item.children.Count);
            foreach (InputControl candidate in candidatesThatHaveMaxScore)
            {
                if (candidate.children.Count != minChildren)
                {
                    candidatesToRemove.Add(candidate);
                }
            }

            foreach (InputControl candidate in candidatesToRemove)
            {
                rebindingOperation.RemoveCandidate(candidate);
            }
        }

        public void CancelRebinding()
        {
            rebindingOperation.Dispose();
            button.interactable = true;

            Initialize(playerInput, rebindableAction, controlScheme, bindingIndex);
        }

        private void Awake()
        {
            button = GetComponentInChildren<Button>();
        }

        private void OnDestroy()
        {
            if (rebindingOperation != null) { rebindingOperation.Dispose(); }
        }

        private void StartRebind()
        {
            SetIsRebinding();

            InputControl cancelControl = null;
            if (pauseAction.controls.Count > 0) { cancelControl = pauseAction.controls[0]; }

            if (cancelControl == null)
            {
                rebindingOperation = rebindableAction.inputActionReferences[0].action.PerformInteractiveRebinding(bindingIndex)
                    .OnPotentialMatch((operation) => FilterCandidates())
                    .OnComplete(operation => SetFinishedRebinding())
                    .OnCancel(operation => CancelRebinding())
                    .WithTimeout(10)
                    .Start();
            }
            else
            {
                rebindingOperation = rebindableAction.inputActionReferences[0].action.PerformInteractiveRebinding(bindingIndex)
                    .OnPotentialMatch((operation) => FilterCandidates())
                    .OnComplete(operation => SetFinishedRebinding())
                    .WithCancelingThrough(cancelControl)
                    .OnCancel(operation => CancelRebinding())
                    .WithTimeout(10)
                    .Start();
            }
        }
    }
}