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
        [SerializeField] private TMP_Dropdown lightAttackModeDropdown;
        [SerializeField] private TMP_Dropdown zoomModeDropdown;
        [SerializeField] private TMP_Dropdown blockingModeDropdown;
        [SerializeField] private TMP_Dropdown orbitalCameraModeDropdown;
        [SerializeField] private RectTransform mobileLookJoystickActLikeButtonParent;
        [SerializeField] private Toggle mobileLookJoystickActLikeButtonToggle;
        [SerializeField] private RectTransform mobileLookJoystickSensitivityParent;
        [SerializeField] private Toggle shouldRepositionMoveJoystick;
        [Header("Key Rebinding")]
        [SerializeField] private RectTransform resetBindingsButtonParent;
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

        protected override void Awake()
        {
            base.Awake();
            resetBindingsButtonParent.gameObject.SetActive(false);

            mobileLookJoystickActLikeButtonToggle.onValueChanged.AddListener(delegate { mobileLookJoystickSensitivityParent.gameObject.SetActive(!mobileLookJoystickActLikeButtonToggle.isOn); });

            mobileLookJoystickSensitivityParent.gameObject.SetActive(FasterPlayerPrefs.IsMobilePlatform & !mobileLookJoystickActLikeButtonToggle.isOn);

            lightAttackModeDropdown.AddOptions(WeaponHandler.GetAttackModeOptions());
            lightAttackModeDropdown.value = WeaponHandler.GetAttackModeOptions().IndexOf(FasterPlayerPrefs.Singleton.GetString("LightAttackMode"));

            zoomModeDropdown.AddOptions(WeaponHandler.GetHoldToggleOptions());
            zoomModeDropdown.value = WeaponHandler.GetHoldToggleOptions().IndexOf(FasterPlayerPrefs.Singleton.GetString("ZoomMode"));

            blockingModeDropdown.AddOptions(WeaponHandler.GetHoldToggleOptions());
            blockingModeDropdown.value = WeaponHandler.GetHoldToggleOptions().IndexOf(FasterPlayerPrefs.Singleton.GetString("BlockingMode"));

            orbitalCameraModeDropdown.transform.parent.parent.gameObject.SetActive(!FasterPlayerPrefs.IsMobilePlatform);
            orbitalCameraModeDropdown.AddOptions(WeaponHandler.GetHoldToggleOptions());
            orbitalCameraModeDropdown.value = WeaponHandler.GetHoldToggleOptions().IndexOf(FasterPlayerPrefs.Singleton.GetString("OrbitalCameraMode"));

            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer) { playerInput = localPlayer.GetComponent<PlayerInput>(); }

            if (!playerInput)
            {
                foreach (PlayerInput pi in FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (pi.name.Contains("Controller"))
                    {
                        playerInput = pi;
                        break;
                    }
                }
            }
        }

        private List<GameObject> rebindingElementObjects = new List<GameObject>();
        private void RegenerateInputBindingMenu()
        {
            foreach (GameObject g in rebindingElementObjects)
            {
                Destroy(g);
            }
            rebindingElementObjects.Clear();

            InputControlScheme controlScheme = controlsAsset.FindControlScheme(playerInput.currentControlScheme).Value;

            foreach (ActionGroup actionGroup in System.Enum.GetValues(typeof(ActionGroup)))
            {
                RebindableAction[] rebindableActionGroup = System.Array.FindAll(rebindableActions, item => item.actionGroup == actionGroup & !item.excludedControlSchemes.Contains(playerInput.currentControlScheme));
                if (rebindableActionGroup.Length > 0)
                {
                    rebindingElementObjects.Add(Instantiate(rebindingSectionHeaderPrefab, rebindingElementParent));
                    rebindingElementObjects[^1].GetComponentInChildren<Text>().text = actionGroup == ActionGroup.UI ? "USER INTERFACE" : actionGroup.ToString().ToUpper();

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

            resetBindingsButtonParent.gameObject.SetActive(rebindingElementObjects.Count > 0);
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

        private string lastControlScheme = "PLACEHOLDER";
        private void Update()
        {
            if (!playerInput) { return; }
            if (playerInput.currentControlScheme == null) { return; }

            if (playerInput.currentControlScheme != lastControlScheme)
            {
                RegenerateInputBindingMenu();
            }

            lastControlScheme = playerInput.currentControlScheme;
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

        public void ChangeOrbitalCameraMode()
        {
            FasterPlayerPrefs.Singleton.SetString("OrbitalCameraMode", WeaponHandler.GetHoldToggleOptions()[orbitalCameraModeDropdown.value]);
        }
    }
}