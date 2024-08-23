using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;
using Vi.Utility;
using Vi.Core.CombatAgents;
using Vi.Player;

namespace Vi.UI
{
    public class ControlsSettingsMenu : Menu
    {
        [SerializeField] private Toggle invertLookToggle;
        [SerializeField] private InputField mouseXSensitivityInput;
        [SerializeField] private InputField mouseYSensitivityInput;
        [SerializeField] private InputField zoomMultiplierInput;
        [SerializeField] private TMP_Dropdown lightAttackModeDropdown;
        [SerializeField] private TMP_Dropdown zoomModeDropdown;
        [SerializeField] private TMP_Dropdown blockingModeDropdown;
        [SerializeField] private RectTransform mobileLookJoystickInputParent;
        [SerializeField] private InputField mobileLookJoystickSensitivityInput;
        [Header("Key Rebinding")]
        [SerializeField] private InputActionAsset controlsAsset;
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

        private PlayerInput playerInput;

        private void Awake()
        {
            invertLookToggle.isOn = FasterPlayerPrefs.Singleton.GetBool("InvertMouse");
            mouseXSensitivityInput.text = FasterPlayerPrefs.Singleton.GetFloat("MouseXSensitivity").ToString();
            mouseYSensitivityInput.text = FasterPlayerPrefs.Singleton.GetFloat("MouseYSensitivity").ToString();
            zoomMultiplierInput.text = FasterPlayerPrefs.Singleton.GetFloat("ZoomSensitivityMultiplier").ToString();
            mobileLookJoystickSensitivityInput.text = FasterPlayerPrefs.Singleton.GetFloat("MobileLookJoystickSensitivity").ToString();

            mobileLookJoystickInputParent.gameObject.SetActive(Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer);

            lightAttackModeDropdown.AddOptions(WeaponHandler.GetAttackModeOptions());
            lightAttackModeDropdown.value = WeaponHandler.GetAttackModeOptions().IndexOf(FasterPlayerPrefs.Singleton.GetString("LightAttackMode"));

            zoomModeDropdown.AddOptions(WeaponHandler.GetHoldToggleOptions());
            zoomModeDropdown.value = WeaponHandler.GetHoldToggleOptions().IndexOf(FasterPlayerPrefs.Singleton.GetString("ZoomMode"));

            blockingModeDropdown.AddOptions(WeaponHandler.GetHoldToggleOptions());
            blockingModeDropdown.value = WeaponHandler.GetHoldToggleOptions().IndexOf(FasterPlayerPrefs.Singleton.GetString("BlockingMode"));

            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer) { playerInput = localPlayer.GetComponent<PlayerInput>(); }
            if (!playerInput) { playerInput = FindObjectOfType<PlayerInput>(); }

            originalSizeDelta = rebindingElementParent.sizeDelta;
        }

        private const float elementSpacing = 100;
        private List<GameObject> rebindingElementObjects = new List<GameObject>();
        private Vector2 originalSizeDelta;
        private void RegenerateInputBindingMenu()
        {
            foreach (GameObject g in rebindingElementObjects)
            {
                Destroy(g);
            }
            rebindingElementParent.sizeDelta = originalSizeDelta;

            InputControlScheme controlScheme = controlsAsset.FindControlScheme(playerInput.currentControlScheme).Value;

            foreach (ActionGroup actionGroup in System.Enum.GetValues(typeof(ActionGroup)))
            {
                RebindableAction[] rebindableActionGroup = System.Array.FindAll(rebindableActions, item => item.actionGroup == actionGroup & !item.excludedControlSchemes.Contains(playerInput.currentControlScheme));
                if (rebindableActionGroup.Length > 0)
                {
                    rebindingElementObjects.Add(Instantiate(rebindingSectionHeaderPrefab, rebindingElementParent));
                    rebindingElementObjects[^1].GetComponentInChildren<Text>().text = actionGroup.ToString();

                    rebindingElementParent.sizeDelta += new Vector2(0, elementSpacing);

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
                                    rebindingElementObjects.Add(Instantiate(rebindingElementPrefab.gameObject, rebindingElementParent));
                                    RebindingElement rebindingElement = rebindingElementObjects[^1].GetComponent<RebindingElement>();
                                    rebindingElementParent.sizeDelta += new Vector2(0, elementSpacing);
                                    rebindingElement.Initialize(playerInput, rebindableAction, controlScheme, bindingIndex);
                                    bindingFound = true;
                                    shouldBreak = !binding.isPartOfComposite;
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
                    bool shouldBreak = false;
                    for (int bindingIndex = 0; bindingIndex < rebindableAction.inputActionReferences[i].action.bindings.Count; bindingIndex++)
                    {
                        InputBinding binding = rebindableAction.inputActionReferences[i].action.bindings[bindingIndex];
                        foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                        {
                            string deviceName = device.name.ToLower();
                            deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                            if (binding.path.ToLower().Contains(deviceName.ToLower()))
                            {
                                playerInput.actions.FindAction(rebindableAction.inputActionReferences[i].action.id).RemoveBindingOverride(bindingIndex);
                                rebindableAction.inputActionReferences[i].action.RemoveBindingOverride(bindingIndex);
                            }
                        }
                        if (shouldBreak) { break; }
                    }
                }
            }

            string rebinds = playerInput.actions.SaveBindingOverridesAsJson();
            FasterPlayerPrefs.Singleton.SetString("Rebinds", rebinds);

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
            FasterPlayerPrefs.Singleton.SetBool("InvertMouse", invertLookToggle.isOn);
        }

        public void ChangeMouseXSensitivity()
        {
            mouseXSensitivityInput.text = Regex.Replace(mouseXSensitivityInput.text, @"[^0-9|.]", "");
            if (float.TryParse(mouseXSensitivityInput.text, out float mouseXSens))
            {
                if (mouseXSens > 0) { FasterPlayerPrefs.Singleton.SetFloat("MouseXSensitivity", mouseXSens); }
            }
        }

        public void ChangeMouseYSensitivity()
        {
            mouseYSensitivityInput.text = Regex.Replace(mouseYSensitivityInput.text, @"[^0-9|.]", "");
            if (float.TryParse(mouseYSensitivityInput.text, out float mouseYSens))
            {
                if (mouseYSens > 0) { FasterPlayerPrefs.Singleton.SetFloat("MouseYSensitivity", mouseYSens); }
            }
        }

        public void ChangeZoomMultiplier()
        {
            zoomMultiplierInput.text = Regex.Replace(zoomMultiplierInput.text, @"[^0-9|.]", "");
            if (float.TryParse(zoomMultiplierInput.text, out float zoomMultiplier))
            {
                if (zoomMultiplier > 0) { FasterPlayerPrefs.Singleton.SetFloat("ZoomSensitivityMultiplier", zoomMultiplier); }
            }
        }

        public void ChangeLightAttackMode()
        {
            FasterPlayerPrefs.Singleton.SetString("LightAttackMode", WeaponHandler.GetAttackModeOptions()[lightAttackModeDropdown.value]);
        }

        public void ChangeZoomMode()
        {
            FasterPlayerPrefs.Singleton.SetString("ZoomMode", WeaponHandler.GetHoldToggleOptions()[zoomModeDropdown.value]);
        }

        public void ChangeBlockingMode()
        {
            FasterPlayerPrefs.Singleton.SetString("BlockingMode", WeaponHandler.GetHoldToggleOptions()[blockingModeDropdown.value]);
        }

        public void ChangeMobileLookJoystickSensitivity()
        {
            mobileLookJoystickSensitivityInput.text = Regex.Replace(mobileLookJoystickSensitivityInput.text, @"[^0-9|.]", "");
            if (float.TryParse(mobileLookJoystickSensitivityInput.text, out float sensitivity))
            {
                FasterPlayerPrefs.Singleton.SetFloat("MobileLookJoystickSensitivity", sensitivity);
            }
        }
    }
}