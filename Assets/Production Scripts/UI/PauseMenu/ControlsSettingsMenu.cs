using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;

namespace Vi.UI
{
    public class ControlsSettingsMenu : Menu
    {
        [SerializeField] private Toggle invertLookToggle;
        [SerializeField] private InputField mouseXSensitivityInput;
        [SerializeField] private InputField mouseYSensitivityInput;
        [SerializeField] private InputField zoomMultiplierInput;
        [SerializeField] private TMP_Dropdown zoomModeDropdown;
        [Header("Key Rebinding")]
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private GridLayoutGroup scrollViewContentGrid;
        [SerializeField] private RectTransform rebindingElementParent;
        [SerializeField] private RebindingElement rebindingElementPrefab;
        [SerializeField] private GameObject rebindingSectionHeaderPrefab;
        [SerializeField] private RebindableAction[] rebindableActions;

        [System.Serializable]
        public struct RebindableAction
        {
            public string overrideActionName;
            public ActionGroup actionGroup;
            public InputActionReference[] inputActionReferences;
            public string[] excludedControlSchemes;
        }

        public enum ActionGroup
        {
            Navigation,
            Combat,
            Spectator,
            UI
        }

        private List<string> holdToggleOptions = new List<string>() { "HOLD", "TOGGLE" };

        private PlayerInput playerInput;

        private void Start()
        {
            invertLookToggle.isOn = bool.Parse(PlayerPrefs.GetString("InvertMouse"));
            mouseXSensitivityInput.text = PlayerPrefs.GetFloat("MouseXSensitivity").ToString();
            mouseYSensitivityInput.text = PlayerPrefs.GetFloat("MouseYSensitivity").ToString();
            zoomMultiplierInput.text = PlayerPrefs.GetFloat("ZoomSensitivityMultiplier").ToString();

            zoomModeDropdown.AddOptions(holdToggleOptions);
            zoomModeDropdown.value = holdToggleOptions.IndexOf(PlayerPrefs.GetString("ZoomMode"));

            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer) { playerInput = localPlayer.GetComponent<PlayerInput>(); }
            if (!playerInput) { playerInput = FindObjectOfType<PlayerInput>(); }

            originalRebindingParentSizeDelta = rebindingElementParent.sizeDelta;

            originalScrollViewGridLayoutSize = scrollViewContentGrid.cellSize;
        }

        private Vector2 originalScrollViewGridLayoutSize;
        private Vector2 originalRebindingParentSizeDelta;
        private void RegenerateInputBindingMenu()
        {
            foreach (Transform child in rebindingElementParent)
            {
                Destroy(child.gameObject);
            }

            rebindingElementParent.sizeDelta = new Vector2(originalRebindingParentSizeDelta.x, 0);
            scrollViewContentGrid.cellSize = originalScrollViewGridLayoutSize;

            InputControlScheme controlScheme = controlsAsset.FindControlScheme(playerInput.currentControlScheme).Value;

            foreach (ActionGroup actionGroup in System.Enum.GetValues(typeof(ActionGroup)))
            {
                RebindableAction[] rebindableActionGroup = System.Array.FindAll(rebindableActions, item => item.actionGroup == actionGroup & !item.excludedControlSchemes.Contains(playerInput.currentControlScheme));
                if (rebindableActionGroup.Length > 0)
                {
                    Instantiate(rebindingSectionHeaderPrefab, rebindingElementParent).GetComponentInChildren<Text>().text = actionGroup.ToString();

                    rebindingElementParent.sizeDelta = new Vector2(rebindingElementParent.sizeDelta.x, rebindingElementParent.sizeDelta.y + 150);
                    scrollViewContentGrid.cellSize = new Vector2(scrollViewContentGrid.cellSize.x, scrollViewContentGrid.cellSize.y + 150);

                    foreach (RebindableAction rebindableAction in rebindableActionGroup)
                    {
                        bool bindingFound = false;
                        bool shouldBreak = false;
                        for (int bindingIndex = 0; bindingIndex < rebindableAction.inputActionReferences[0].action.bindings.Count; bindingIndex++)
                        {
                            InputBinding binding = rebindableAction.inputActionReferences[0].action.bindings[bindingIndex];
                            foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                            {
                                string deviceName = device.name.ToLower();
                                deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                                if (binding.path.ToLower().Contains(deviceName.ToLower()))
                                {
                                    RebindingElement rebindingElement = Instantiate(rebindingElementPrefab, rebindingElementParent).GetComponent<RebindingElement>();
                                    rebindingElement.Initialize(playerInput, rebindableAction, controlScheme, bindingIndex);
                                    bindingFound = true;
                                    shouldBreak = !binding.isPartOfComposite;
                                    rebindingElementParent.sizeDelta = new Vector2(rebindingElementParent.sizeDelta.x, rebindingElementParent.sizeDelta.y + 125);
                                    scrollViewContentGrid.cellSize = new Vector2(scrollViewContentGrid.cellSize.x, scrollViewContentGrid.cellSize.y + 125);
                                    break;
                                }
                            }
                            if (shouldBreak) { break; }
                        }
                        if (!bindingFound) { Debug.LogError("No binding found for " + rebindableAction.inputActionReferences[0].action.name); }
                    }
                }
            }
        }

        public void ResetBindingsToDefaults()
        {
            InputControlScheme controlScheme = controlsAsset.FindControlScheme(playerInput.currentControlScheme).Value;
            RebindableAction[] rebindableControlGroup = System.Array.FindAll(rebindableActions, item => !item.excludedControlSchemes.Contains(playerInput.currentControlScheme));
            foreach (RebindableAction rebindableAction in rebindableControlGroup)
            {
                for (int i = 0; i < rebindableAction.inputActionReferences.Length; i++)
                {
                    for (int bindingIndex = 0; bindingIndex < rebindableAction.inputActionReferences[i].action.bindings.Count; bindingIndex++)
                    {
                        InputBinding binding = rebindableAction.inputActionReferences[i].action.bindings[bindingIndex];
                        foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                        {
                            if (binding.path.ToLower().Contains(device.name.ToLower()))
                            {
                                playerInput.actions.FindAction(rebindableAction.inputActionReferences[i].action.id).RemoveBindingOverride(bindingIndex);
                            }
                        }
                    }
                }
            }

            string rebinds = playerInput.actions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString("Rebinds", rebinds);

            RegenerateInputBindingMenu();

            PlayerUI playerUI = playerInput.GetComponentInChildren<PlayerUI>(true);
            if (playerUI) { playerUI.OnRebinding(); }
        }

        private string lastControlScheme;
        private void Update()
        {
            if (playerInput.currentControlScheme == null) { return; }

            if (playerInput.currentControlScheme != lastControlScheme)
            {
                RegenerateInputBindingMenu();
            }

            lastControlScheme = playerInput.currentControlScheme;
        }

        public void SetInvertMouse()
        {
            PlayerPrefs.SetString("InvertMouse", invertLookToggle.isOn.ToString());
        }

        public void ChangeMouseXSensitivity()
        {
            mouseXSensitivityInput.text = Regex.Replace(mouseXSensitivityInput.text, @"[^0-9|.]", "");
            if (float.TryParse(mouseXSensitivityInput.text, out float mouseXSens))
            {
                if (mouseXSens > 0) { PlayerPrefs.SetFloat("MouseXSensitivity", mouseXSens); }
            }
        }

        public void ChangeMouseYSensitivity()
        {
            mouseYSensitivityInput.text = Regex.Replace(mouseYSensitivityInput.text, @"[^0-9|.]", "");
            if (float.TryParse(mouseYSensitivityInput.text, out float mouseYSens))
            {
                if (mouseYSens > 0) { PlayerPrefs.SetFloat("MouseYSensitivity", mouseYSens); }
            }
        }

        public void ChangeZoomMultiplier()
        {
            zoomMultiplierInput.text = Regex.Replace(zoomMultiplierInput.text, @"[^0-9|.]", "");
            if (float.TryParse(zoomMultiplierInput.text, out float zoomMultiplier))
            {
                if (zoomMultiplier > 0) { PlayerPrefs.SetFloat("ZoomSensitivityMultiplier", zoomMultiplier); }
            }
        }

        public void ChangeZoomMode()
        {
            PlayerPrefs.SetString("ZoomMode", holdToggleOptions[zoomModeDropdown.value]);
        }
    }
}