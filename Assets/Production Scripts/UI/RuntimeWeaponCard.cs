using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using Vi.Core;
using UnityEngine.InputSystem;

namespace Vi.UI
{
    public class RuntimeWeaponCard : MonoBehaviour
    {
        [SerializeField] private Image weaponIcon;
        [Header("With Binding String")]
        [SerializeField] private RectTransform withBindingParent;
        [SerializeField] private Text weaponBindingText;
        [SerializeField] private Image withBindingWeaponSlotTypeColor;
        [SerializeField] private RectTransform withBindingAmmoParent;
        [SerializeField] private Text withBindingAmmoText;
        [SerializeField] private Text withBindingMagSizeText;
        [Header("No Binding String")]
        [SerializeField] private RectTransform noBindingParent;
        [SerializeField] private Image noBindingWeaponSlotTypeColor;
        [SerializeField] private RectTransform noBindingAmmoParent;
        [SerializeField] private Text noBindingAmmoText;
        [SerializeField] private Text noBindingMagSizeText;

        private Dictionary<LoadoutManager.WeaponSlotType, Color> slotTypeColors = new Dictionary<LoadoutManager.WeaponSlotType, Color>()
        {
            { LoadoutManager.WeaponSlotType.Primary, new Color(128 / (float)255, 128 / (float)255, 128 / (float)255) },
            { LoadoutManager.WeaponSlotType.Secondary, new Color(124 / (float)255, 252 / (float)255, 0) }
        };

        private LoadoutManager loadoutManager;
        private Weapon weapon;
        private LoadoutManager.WeaponSlotType weaponSlotType = LoadoutManager.WeaponSlotType.Primary;
        private PlayerInput playerInput;
        private InputActionAsset inputActions;

        public void SetPreviewOn()
        {
            withBindingParent.gameObject.SetActive(false);
            noBindingParent.gameObject.SetActive(true);
        }

        public void Initialize(LoadoutManager loadoutManager, Weapon weapon, LoadoutManager.WeaponSlotType weaponSlotType, PlayerInput playerInput, InputActionAsset inputActions)
        {
            this.loadoutManager = loadoutManager;
            this.weapon = weapon;
            this.weaponSlotType = weaponSlotType;
            this.playerInput = playerInput;
            this.inputActions = inputActions;

            withBindingWeaponSlotTypeColor.color = slotTypeColors[weaponSlotType];
            noBindingWeaponSlotTypeColor.color = slotTypeColors[weaponSlotType];
            
            weaponIcon.sprite = System.Array.Find(PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions(), item => item.weapon.name == weapon.name.Replace("(Clone)", "")).weaponIcon;
            
            withBindingAmmoParent.gameObject.SetActive(weapon.ShouldUseAmmo());
            noBindingAmmoParent.gameObject.SetActive(weapon.ShouldUseAmmo());

            if (weapon.ShouldUseAmmo())
            {
                withBindingMagSizeText.text = weapon.GetMaxAmmoCount().ToString();
                noBindingMagSizeText.text = weapon.GetMaxAmmoCount().ToString();
            }
        }

        private Image backgroundImage;
        private Color originalBackgroundColor;
        private Color flashAttackColor = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);
        private const float backgroundColorTransitionSpeed = 16;

        private void Start()
        {
            backgroundImage = GetComponent<Image>();
            originalBackgroundColor = backgroundImage.color;
            flashAttackColor.a = originalBackgroundColor.a;
        }

        private string lastControlScheme;
        private bool forceRefreshThisFrame;

        private void Update()
        {
            if (!loadoutManager) { return; }

            if (weapon.ShouldUseAmmo())
            {
                withBindingAmmoText.text = loadoutManager.GetAmmoCount(weapon).ToString();
                noBindingAmmoText.text = loadoutManager.GetAmmoCount(weapon).ToString();
            }

            backgroundImage.color = Color.Lerp(backgroundImage.color, loadoutManager.WeaponNameThatCanFlashAttack == weapon.name ? flashAttackColor : originalBackgroundColor, Time.deltaTime * backgroundColorTransitionSpeed);

            if (playerInput.currentControlScheme != lastControlScheme | forceRefreshThisFrame)
            {
                UpdateControlSchemeText();
            }

            lastControlScheme = playerInput.currentControlScheme;
        }

        private void UpdateControlSchemeText()
        {
            InputControlScheme controlScheme = inputActions.FindControlScheme(playerInput.currentControlScheme).Value;

            foreach (InputBinding binding in playerInput.actions[weaponSlotType == LoadoutManager.WeaponSlotType.Primary ? "Weapon1" : "Weapon2"].bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
                    {
                        weaponBindingText.text = binding.ToDisplayString().Replace("Right ", "R").Replace("Left ", "L").Replace("Control", "Ctrl");
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak)
                {
                    withBindingParent.gameObject.SetActive(true);
                    noBindingParent.gameObject.SetActive(false);
                    break;
                }
                else // If we couldn't find an input binding string
                {
                    withBindingParent.gameObject.SetActive(false);
                    noBindingParent.gameObject.SetActive(true);
                    weaponBindingText.text = string.Empty;
                }
            }

            forceRefreshThisFrame = false;
        }

        public void OnRebinding()
        {
            forceRefreshThisFrame = true;
        }
    }
}