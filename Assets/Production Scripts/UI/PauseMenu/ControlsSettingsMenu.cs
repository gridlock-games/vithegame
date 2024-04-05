using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
        [SerializeField] private RebindableAction[] rebindableActions;

        [System.Serializable]
        public struct RebindableAction
        {
            public ActionGroup actionGroup;
            public InputActionReference inputActionReference;
        }

        public enum ActionGroup
        {
            Movement,
            Combat,
            Spectator,
            UI
        }

        private List<string> holdToggleOptions = new List<string>() { "HOLD", "TOGGLE" };

        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
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

            //foreach (InputAction inputAction in controlsAsset.FindActionMap("Base").actions)
            //{
            //    rebindingOperation = inputAction.PerformInteractiveRebinding()
            //        .WithControlsExcluding("Mouse")
            //        .OnMatchWaitForAnother(0.1f)
            //        .OnComplete(operation => OnRebindComplete(inputAction))
            //        .Start();
            //}
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

            foreach (RebindableAction rebindableAction in rebindableActions)
            {
                RebindingElement rebindingElement = Instantiate(rebindingElementPrefab, rebindingElementParent).GetComponent<RebindingElement>();
                rebindingElement.Initialize(rebindableAction, controlScheme);
                rebindingElement.Button.onClick.AddListener(delegate { StartRebind(rebindingElement, rebindableAction.inputActionReference.action); });

                rebindingElementParent.sizeDelta = new Vector2(rebindingElementParent.sizeDelta.x, rebindingElementParent.sizeDelta.y + 125);
                scrollViewContentGrid.cellSize = new Vector2(scrollViewContentGrid.cellSize.x, scrollViewContentGrid.cellSize.y + 125);
            }
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

        private void StartRebind(RebindingElement rebindingElement, InputAction inputAction)
        {

        }

        private void OnRebindComplete(InputAction inputAction)
        {
            InputControlPath.ToHumanReadableString(inputAction.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);

            rebindingOperation.Dispose();
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