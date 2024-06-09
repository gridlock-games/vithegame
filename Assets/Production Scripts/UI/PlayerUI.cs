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
using Vi.Utility;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        public List<AbilityCard> GetAbilityCards() { return new List<AbilityCard>() { ability1, ability2, ability3, ability4 }; }

        public Image GetHeavyAttackButton() { return heavyAttackButton; }

        public RectTransform GetBlockingButton() { return blockingButton; }

        public RectTransform GetDodgeButton() { return dodgeButton; }

        public PlayerCard GetMainPlayerCard() { return playerCard; }

        public RectTransform GetLookJoystickCenter() { return lookJoystickCenter; }

        public RuntimeWeaponCard[] GetWeaponCards() { return GetComponentsInChildren<RuntimeWeaponCard>(true); }

        public RectTransform GetSwitchWeaponButton() { return switchWeaponButton; }

        public RectTransform GetOnScreenReloadButton() { return onScreenReloadButton; }

        public Button GetPauseMenuButton() { return pauseMenuButton; }

        public Button GetLoadoutMenuButton() { return loadoutMenuButton; }

        public Button GetScoreboardButton() { return scoreboardButton; }

        private bool shouldFadeToBlack;
        public void SetFadeToBlack(bool shouldFade) { shouldFadeToBlack = shouldFade; }

        public Color GetFadeToBlackColor() { return fadeToBlackImage.color; }

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
        [SerializeField] private Image dodgeIconImageOnPC;
        [SerializeField] private Image dodgeCooldownImage;
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
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button loadoutMenuButton;
        [SerializeField] private Button scoreboardButton;
        [SerializeField] private Image heavyAttackButton;
        [SerializeField] private Sprite aimIcon;
        [SerializeField] private Sprite heavyAttackIcon;
        [SerializeField] private RectTransform blockingButton;
        [SerializeField] private RectTransform dodgeButton;
        [SerializeField] private RectTransform lookJoystickCenter;
        [SerializeField] private RectTransform switchWeaponButton;
        [SerializeField] private RectTransform onScreenReloadButton;
        [SerializeField] private Image mobileDodgeCooldownImage;

        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public void OpenPauseMenu()
        {
            if (!playerInput.actions.FindAction("Pause").enabled) { return; }
            attributes.GetComponent<ActionMapHandler>().OnPause();
        }

        public void OpenInventoryMenu()
        {
            if (!playerInput.actions.FindAction("Inventory").enabled) { return; }
            attributes.GetComponent<ActionMapHandler>().OnInventory();
        }

        public void OpenScoreboard()
        {
            if (!playerInput.actions.FindAction("Scoreboard").enabled) { return; }
            attributes.GetComponent<ActionMapHandler>().OpenScoreboard();
        }

        public void SwitchWeapon()
        {
            if (!playerInput.actions.FindAction("SwitchWeapon").enabled) { return; }
            loadoutManager.SwitchWeapon();
        }

        public void StartLightAttack()
        {
            if (!playerInput.actions.FindAction("LightAttack").enabled) { return; }
            weaponHandler.LightAttackHold(true);
        }

        public void StopLightAttack() { weaponHandler.LightAttackHold(false); }

        public void StartHeavyAttack()
        {
            if (!playerInput.actions.FindAction("HeavyAttack").enabled) { return; }
            weaponHandler.HeavyAttackHold(true);
        }

        public void StopHeavyAttack()
        {
            weaponHandler.HeavyAttackHold(false);
        }

        public void Ability1(bool isPressed)
        {
            if (!playerInput.actions.FindAction("Ability1").enabled) { isPressed = false; }
            weaponHandler.Ability1(isPressed);
        }

        public void Ability2(bool isPressed)
        {
            if (!playerInput.actions.FindAction("Ability2").enabled) { isPressed = false; }
            weaponHandler.Ability2(isPressed);
        }

        public void Ability3(bool isPressed)
        {
            if (!playerInput.actions.FindAction("Ability3").enabled) { isPressed = false; }
            weaponHandler.Ability3(isPressed);
        }

        public void Ability4(bool isPressed)
        {
            if (!playerInput.actions.FindAction("Ability4").enabled) { isPressed = false; }
            weaponHandler.Ability4(isPressed);
        }

        public void Reload()
        {
            if (!playerInput.actions.FindAction("Reload").enabled) { return; }
            weaponHandler.Reload();
        }

        public void Dodge()
        {
            if (!playerInput.actions.FindAction("Dodge").enabled) { return; }
            playerMovementHandler.OnDodge();
        }

        public void Block(bool isPressed) { weaponHandler.Block(isPressed); }

        public void Rage() { attributes.OnActivateRage(); }

        public void IncrementFollowPlayer() { playerMovementHandler.OnIncrementFollowPlayer(); }

        public void DecrementFollowPlayer() { playerMovementHandler.OnDecrementFollowPlayer(); }

        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private LoadoutManager loadoutManager;
        private PlayerInput playerInput;
        private PlayerMovementHandler playerMovementHandler;

        [SerializeField] private Canvas[] aliveUIChildCanvases;
        [SerializeField] private Canvas[] deathUIChildCanvases;

        private void Awake()
        {
            weaponHandler = GetComponentInParent<WeaponHandler>();
            attributes = weaponHandler.GetComponent<Attributes>();
            playerInput = weaponHandler.GetComponent<PlayerInput>();
            loadoutManager = weaponHandler.GetComponent<LoadoutManager>();
            playerMovementHandler = weaponHandler.GetComponent<PlayerMovementHandler>();

            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            RefreshStatus();
        }

        private Vector3 equippedWeaponCardAnchoredPosition;
        private Vector3 stowedWeaponCardAnchoredPosition;

        private Vector2 moveJoystickOriginalAnchoredPosition;
        private CanvasGroup[] canvasGroups;
        private void Start()
        {
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

        private void RefreshStatus()
        {
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            }
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
            bool ability1Initialized = false;
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
                        ability1Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability1Initialized) { ability1.UpdateCard(abilities[0], ""); }

            bool ability2Initialized = false;
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
                        ability2Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability2Initialized) { ability2.UpdateCard(abilities[1], ""); }

            bool ability3Initialized = false;
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
                        ability3Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability3Initialized) { ability3.UpdateCard(abilities[2], ""); }

            bool ability4Initialized = false;
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
                        ability4Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability4Initialized) { ability4.UpdateCard(abilities[3], ""); }

            lastWeapon = weaponHandler.GetWeapon();

            if (primaryWeaponCard.isActiveAndEnabled) { primaryWeaponCard.Initialize(loadoutManager, loadoutManager.PrimaryWeaponOption.weapon, LoadoutManager.WeaponSlotType.Primary, playerInput, controlsAsset); }
            if (secondaryWeaponCard.isActiveAndEnabled) { secondaryWeaponCard.Initialize(loadoutManager, loadoutManager.SecondaryWeaponOption.weapon, LoadoutManager.WeaponSlotType.Secondary, playerInput, controlsAsset); }
            if (mobileWeaponCard.isActiveAndEnabled) { mobileWeaponCard.Initialize(loadoutManager,
                loadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Primary ? loadoutManager.PrimaryWeaponOption.weapon : loadoutManager.SecondaryWeaponOption.weapon,
                loadoutManager.GetEquippedSlotType(), playerInput, controlsAsset); }
        }

        private void UpdateActiveUIElements()
        {
            bool isDead = attributes.GetAilment() == ActionClip.Ailment.Death;
            foreach (Canvas canvas in aliveUIChildCanvases)
            {
                canvas.enabled = !isDead;
            }

            foreach (Canvas canvas in deathUIChildCanvases)
            {
                canvas.enabled = isDead;
            }
        }

        private const float weaponCardAnimationSpeed = 8;
        private void UpdateWeaponCardPositions()
        {
            if (!primaryWeaponCard.isActiveAndEnabled | !secondaryWeaponCard.isActiveAndEnabled) { return; }

            bool primaryIsEquipped = loadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Primary;

            primaryWeaponCard.transform.localPosition = Vector3.Lerp(primaryWeaponCard.transform.localPosition, primaryIsEquipped ? equippedWeaponCardAnchoredPosition : stowedWeaponCardAnchoredPosition, Time.deltaTime * weaponCardAnimationSpeed);
            secondaryWeaponCard.transform.localPosition = Vector3.Lerp(secondaryWeaponCard.transform.localPosition, primaryIsEquipped ? stowedWeaponCardAnchoredPosition : equippedWeaponCardAnchoredPosition, Time.deltaTime * weaponCardAnimationSpeed);
        }

        private void OnEnable()
        {
            RefreshStatus();
        }

        List<Attributes> teammateAttributes = new List<Attributes>();
        private string lastControlScheme;
        private int moveTouchId;
        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }
            if (!weaponHandler.WeaponInitialized) { return; }

            scoreboardButton.gameObject.SetActive(Core.GameModeManagers.GameModeManager.Singleton);

            float dodgeCooldownProgress = weaponHandler.GetWeapon().GetDodgeCooldownProgress();
            mobileDodgeCooldownImage.fillAmount = 1 - dodgeCooldownProgress;
            dodgeCooldownImage.fillAmount = 1 - dodgeCooldownProgress;

            bool dodgeIsOnCooldown = !Mathf.Approximately(dodgeCooldownProgress, 1);
            dodgeIconImageOnPC.enabled = dodgeIsOnCooldown;
            dodgeCooldownImage.enabled = dodgeIsOnCooldown;
            mobileDodgeCooldownImage.enabled = dodgeIsOnCooldown;

            if (attributes.GetAilment() != ActionClip.Ailment.Death)
            {
                if (attributes.ActiveStatusesWasUpdatedThisFrame)
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

                if (shouldFadeToBlack)
                {
                    fadeToBlackImage.color = Vector4.MoveTowards(fadeToBlackImage.color, Color.black, Time.deltaTime);
                    fadeToWhiteImage.color = Vector4.MoveTowards(fadeToWhiteImage.color, Color.black, Time.deltaTime);
                }
                else
                {
                    fadeToBlackImage.color = Color.clear;
                    fadeToWhiteImage.color = Vector4.MoveTowards(fadeToWhiteImage.color, Color.clear, Time.deltaTime);
                }
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