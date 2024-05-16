using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.InputSystem;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using System.Linq;
using Unity.Netcode;
using Vi.Player;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private PlayerCard playerCard;
        [SerializeField] private PlayerCard[] teammatePlayerCards;
        [Header("Weapon Cards")]
        [SerializeField] private RuntimeWeaponCard primaryWeaponCard;
        [SerializeField] private RuntimeWeaponCard secondaryWeaponCard;
        [SerializeField] private RuntimeWeaponCard mobileWeaponCard;
        [Header("Ability Cards")]
        [SerializeField] private AbilityCard ability1;
        [SerializeField] private AbilityCard ability2;
        [SerializeField] private AbilityCard ability3;
        [SerializeField] private AbilityCard ability4;
        [Header("Status UI")]
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;
        [Header("Death UI")]
        [SerializeField] private PlayerCard killerCard;
        [SerializeField] private Text respawnTimerText;
        [SerializeField] private Text killedByText;
        [SerializeField] private Text waitingToFindViableSpawnPointText;
        [SerializeField] private Image fadeToBlackImage;
        [SerializeField] private Image fadeToWhiteImage;
        [SerializeField] private GameObject deathUIParent;
        [SerializeField] private GameObject aliveUIParent;
        [Header("Mobile UI")]
        [SerializeField] private CustomOnScreenStick moveJoystick;
        [SerializeField] private Button scoreboardButton;
        [SerializeField] private Image heavyAttackButton;
        [SerializeField] private Sprite aimIcon;
        [SerializeField] private Sprite heavyAttackIcon;

        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public void OpenPauseMenu()
        {
            attributes.GetComponent<ActionMapHandler>().OnPause();
        }

        public void OpenInventoryMenu()
        {
            attributes.GetComponent<ActionMapHandler>().OnInventory();
        }

        public void OpenScoreboard()
        {
            attributes.GetComponent<ActionMapHandler>().OpenScoreboard();
        }

        public void SwitchWeapon()
        {
            loadoutManager.SwitchWeapon();
        }

        public void StartLightAttack()
        {
            weaponHandler.LightAttackHold(true);
        }

        public void StopLightAttack()
        {
            weaponHandler.LightAttackHold(false);
        }

        public void StartHeavyAttack()
        {
            weaponHandler.HeavyAttackHold(true);
        }

        public void StopHeavyAttack()
        {
            weaponHandler.HeavyAttackHold(false);
        }

        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private LoadoutManager loadoutManager;
        private PlayerInput playerInput;

        private void Awake()
        {
            weaponHandler = GetComponentInParent<WeaponHandler>();
            attributes = weaponHandler.GetComponent<Attributes>();
            playerInput = weaponHandler.GetComponent<PlayerInput>();
            loadoutManager = weaponHandler.GetComponent<LoadoutManager>();
        }

        private Vector3 equippedWeaponCardAnchoredPosition;
        private Vector3 stowedWeaponCardAnchoredPosition;

        private Vector2 moveJoystickOriginalAnchoredPosition;
        private CanvasGroup[] canvasGroups;
        private void Start()
        {
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PersistentLocalObjects.Singleton.GetFloat("UIOpacity");
            }

            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, statusImageParent).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
            }

            equippedWeaponCardAnchoredPosition = primaryWeaponCard.transform.localPosition;
            stowedWeaponCardAnchoredPosition = secondaryWeaponCard.transform.localPosition;

            RectTransform rt = (RectTransform)moveJoystick.transform.parent;
            moveJoystickOriginalAnchoredPosition = rt.anchoredPosition;

            fadeToWhiteImage.color = Color.black;

            playerCard.Initialize(GetComponentInParent<Attributes>());

            UpdateTeammateAttributesList();

            UpdateWeapon(false);
        }

        private void UpdateTeammateAttributesList()
        {
            teammateAttributes = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(attributes.GetTeam(), attributes);
        }

        public void OnRebinding()
        {
            UpdateWeapon(true);
            primaryWeaponCard.OnRebinding();
            secondaryWeaponCard.OnRebinding();
            mobileWeaponCard.OnRebinding();
        }

        private Weapon lastWeapon;
        private void UpdateWeapon(bool forceRefresh)
        {
            if (playerInput.currentControlScheme == null) { return; }
            if (!weaponHandler.WeaponInitialized) { return; }

            if (!forceRefresh)
            {
                if (lastWeapon == weaponHandler.GetWeapon())
                {
                    lastWeapon = weaponHandler.GetWeapon();
                    return;
                }
            }

            InputControlScheme controlScheme = controlsAsset.FindControlScheme(playerInput.currentControlScheme).Value;

            List<ActionClip> abilities = weaponHandler.GetWeapon().GetAbilities();
            foreach (InputBinding binding in playerInput.actions["Ability1"].bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    string deviceName = device.name.ToLower();
                    deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                    if (binding.path.ToLower().Contains(deviceName.ToLower()))
                    {
                        ability1.UpdateCard(abilities[0], binding.ToDisplayString());
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            foreach (InputBinding binding in playerInput.actions["Ability2"].bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    string deviceName = device.name.ToLower();
                    deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                    if (binding.path.ToLower().Contains(deviceName.ToLower()))
                    {
                        ability2.UpdateCard(abilities[1], binding.ToDisplayString());
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            foreach (InputBinding binding in playerInput.actions["Ability3"].bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    string deviceName = device.name.ToLower();
                    deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                    if (binding.path.ToLower().Contains(deviceName.ToLower()))
                    {
                        ability3.UpdateCard(abilities[2], binding.ToDisplayString());
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            foreach (InputBinding binding in playerInput.actions["Ability4"].bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    string deviceName = device.name.ToLower();
                    deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                    if (binding.path.ToLower().Contains(deviceName.ToLower()))
                    {
                        ability4.UpdateCard(abilities[3], binding.ToDisplayString());
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            lastWeapon = weaponHandler.GetWeapon();

            if (primaryWeaponCard.isActiveAndEnabled) { primaryWeaponCard.Initialize(loadoutManager, loadoutManager.PrimaryWeaponOption.weapon, LoadoutManager.WeaponSlotType.Primary, playerInput, controlsAsset); }
            if (secondaryWeaponCard.isActiveAndEnabled) { secondaryWeaponCard.Initialize(loadoutManager, loadoutManager.SecondaryWeaponOption.weapon, LoadoutManager.WeaponSlotType.Secondary, playerInput, controlsAsset); }
            if (mobileWeaponCard.isActiveAndEnabled) { mobileWeaponCard.Initialize(loadoutManager,
                loadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Primary ? loadoutManager.PrimaryWeaponOption.weapon : loadoutManager.SecondaryWeaponOption.weapon,
                loadoutManager.GetEquippedSlotType(), playerInput, controlsAsset); }
        }

        private void UpdateActiveUIElements()
        {
            aliveUIParent.SetActive(attributes.GetAilment() != ActionClip.Ailment.Death);
            deathUIParent.SetActive(attributes.GetAilment() == ActionClip.Ailment.Death);
        }

        private const float weaponCardAnimationSpeed = 8;
        private void UpdateWeaponCardPositions()
        {
            if (!primaryWeaponCard.isActiveAndEnabled | !secondaryWeaponCard.isActiveAndEnabled) { return; }

            bool primaryIsEquipped = loadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Primary;

            primaryWeaponCard.transform.localPosition = Vector3.Lerp(primaryWeaponCard.transform.localPosition, primaryIsEquipped ? equippedWeaponCardAnchoredPosition : stowedWeaponCardAnchoredPosition, Time.deltaTime * weaponCardAnimationSpeed);
            secondaryWeaponCard.transform.localPosition = Vector3.Lerp(secondaryWeaponCard.transform.localPosition, primaryIsEquipped ? stowedWeaponCardAnchoredPosition : equippedWeaponCardAnchoredPosition, Time.deltaTime * weaponCardAnimationSpeed);
        }

        List<Attributes> teammateAttributes = new List<Attributes>();
        private string lastControlScheme;
        private int moveTouchId;
        private void Update()
        {
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PersistentLocalObjects.Singleton.GetFloat("UIOpacity");
            }

            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }
            if (!weaponHandler.WeaponInitialized) { return; }

            scoreboardButton.gameObject.SetActive(Core.GameModeManagers.GameModeManager.Singleton);

            if (attributes.GetAilment() != ActionClip.Ailment.Death)
            {
                List<ActionClip.Status> activeStatuses = attributes.GetActiveStatuses();
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    if (activeStatuses.Contains(statusIcon.Status))
                    {
                        statusIcon.SetActive(true);
                        statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                    }
                    else
                    {
                        statusIcon.SetActive(false);
                    }
                }

                if (Application.platform != RuntimePlatform.Android & Application.platform != RuntimePlatform.IPhonePlayer)
                {
                    if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateTeammateAttributesList(); }

                    // Order player cards by distance
                    teammateAttributes.Where(item => item.GetAilment() != ActionClip.Ailment.Death).OrderBy(x => Vector3.Distance(attributes.transform.position, x.transform.position)).Take(teammatePlayerCards.Length).ToList();
                    for (int i = 0; i < teammatePlayerCards.Length; i++)
                    {
                        if (i < teammateAttributes.Count)
                        {
                            teammatePlayerCards[i].Initialize(teammateAttributes[i]);
                        }
                        else
                        {
                            teammatePlayerCards[i].Initialize(null);
                        }
                    }
                }

                fadeToBlackImage.color = Color.clear;
                fadeToWhiteImage.color = Color.Lerp(fadeToWhiteImage.color, Color.clear, Time.deltaTime);
            }
            else
            {
                NetworkObject killerNetObj = attributes.GetKiller();
                Attributes killerAttributes = null;
                if (killerNetObj) { killerAttributes = killerNetObj.GetComponent<Attributes>(); }

                if (killerAttributes)
                {
                    killerCard.Initialize(killerAttributes);
                    killedByText.text = "Killed by";
                }
                else
                {
                    killerCard.Initialize(null);
                    killedByText.text = "Killed by " + (killerNetObj ? killerNetObj.name : "Unknown");
                }

                respawnTimerText.text = attributes.IsRespawning ? "Respawning in " + attributes.GetRespawnTime().ToString("F4") : "";

                if (attributes.IsRespawning)
                {
                    fadeToBlackImage.color = Color.Lerp(Color.clear, Color.black, attributes.GetRespawnTimeAsPercentage());
                    fadeToWhiteImage.color = fadeToBlackImage.color;

                    if (attributes.isWaitingForSpawnPoint)
                    {
                        if (waitingToFindViableSpawnPointText.text == "") { waitingToFindViableSpawnPointText.text = "Waiting for viable spawn point..."; }
                    }
                    else
                    {
                        waitingToFindViableSpawnPointText.text = "";
                    }
                }
                else
                {
                    waitingToFindViableSpawnPointText.text = "";
                }
            }
            UpdateActiveUIElements();
            UpdateWeaponCardPositions();
            UpdateWeapon(playerInput.currentControlScheme != lastControlScheme);

            lastControlScheme = playerInput.currentControlScheme;

            heavyAttackButton.sprite = weaponHandler.CanAim ? aimIcon : heavyAttackIcon;
        }
    }
}