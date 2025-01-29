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
using Vi.Core.GameModeManagers;
using Vi.Core.CombatAgents;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        public Canvas GetTextButtonCanvas() { return textChatButtonCanvas; }

        public List<AbilityCard> GetAbilityCards() { return new List<AbilityCard>() { ability1, ability2, ability3, ability4 }; }

        public Image GetHeavyAttackButton() { return heavyAttackButton; }

        public RectTransform GetBlockingButton() { return blockingButton.rectTransform; }

        public RectTransform GetDodgeButton() { return dodgeButton.rectTransform; }

        public PlayerCard GetMainPlayerCard() { return playerCard; }

        public RectTransform GetLookJoystickCenter() { return lookJoystickCenter.rectTransform; }

        public RuntimeWeaponCard[] GetWeaponCards() { return GetComponentsInChildren<RuntimeWeaponCard>(true); }

        public RectTransform GetSwitchWeaponButton() { return switchWeaponButton; }

        public RectTransform GetOnScreenReloadButton() { return onScreenReloadButton; }

        public void RefreshOnScreenReloadButtonInteractability() { onScreenReloadButton.gameObject.SetActive(attributes.WeaponHandler.CanADS); }

        public Button GetPauseMenuButton() { return pauseMenuButton; }

        public Button GetLoadoutMenuButton() { return loadoutMenuButton; }

        public Button GetScoreboardButton() { return scoreboardButton; }

        public RectTransform GetOrbitalCameraButton() { return orbitalCameraButton; }

        public PotionCard GetHealthPotionCard() { return healthPotionCard; }
        public PotionCard GetStaminaPotionCard() { return staminaPotionCard; }

        private bool shouldFadeToBlack;
        public void SetFadeToBlack(bool shouldFade) { shouldFadeToBlack = shouldFade; }

        public Color GetFadeToBlackColor() { return fadeToBlackImage.color; }

        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private PlayerCard playerCard;
        [SerializeField] private PlayerCard[] teammatePlayerCards;
        [SerializeField] private Image tooltipImage;
        [SerializeField] private Image crosshairImage;
        [Header("Weapon Cards")]
        [SerializeField] private RuntimeWeaponCard primaryWeaponCard;
        [SerializeField] private RuntimeWeaponCard secondaryWeaponCard;
        [SerializeField] private RuntimeWeaponCard mobileWeaponCard;
        [Header("Ability Cards")]
        [SerializeField] private AbilityCard ability1;
        [SerializeField] private AbilityCard ability2;
        [SerializeField] private AbilityCard ability3;
        [SerializeField] private AbilityCard ability4;
        [SerializeField] private PotionCard healthPotionCard;
        [SerializeField] private PotionCard staminaPotionCard;
        [SerializeField] private Image dodgeButton;
        [SerializeField] private Text dodgeStackText;
        [SerializeField] private Image dodgeBackgroundTextImage;
        [Header("Status UI")]
        [SerializeField] private StatusIconLayoutGroup statusIconLayoutGroup;
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
        [SerializeField] private Image blockingButton;
        [SerializeField] private Image lookJoystickCenter;
        [SerializeField] private RectTransform switchWeaponButton;
        [SerializeField] private RectTransform onScreenReloadButton;
        [SerializeField] private RectTransform orbitalCameraButton;
        [SerializeField] private Image mobileInteractableImage;
        [Header("Text Chat")]
        [SerializeField] private Canvas textChatButtonCanvas;

        private InputAction pauseMenuAction;
        public void OpenPauseMenu()
        {
            if (!pauseMenuAction.enabled) { return; }
            actionMapHandler.OnPause();
        }

        private InputAction inventoryAction;
        public void OpenInventoryMenu()
        {
            if (!inventoryAction.enabled) { return; }
            actionMapHandler.OnInventory();
        }

        private InputAction scoreboardAction;
        public void OpenScoreboard()
        {
            if (!scoreboardAction.enabled) { return; }
            actionMapHandler.OpenScoreboard();
        }

        private InputAction switchWeaponAction;
        public void SwitchWeapon()
        {
            if (!switchWeaponAction.enabled) { return; }
            attributes.LoadoutManager.SwitchWeapon();
        }

        private InputAction lightAttackAction;
        public void StartLightAttack()
        {
            if (!lightAttackAction.enabled) { return; }
            attributes.WeaponHandler.LightAttack(true);
        }

        public void StopLightAttack() { attributes.WeaponHandler.LightAttack(false); }

        private InputAction heavyAttackAction;
        private InputAction aimAction;
        public void StartHeavyAttack()
        {
            if (attributes.WeaponHandler.CanADS)
            {
                if (!aimAction.enabled) { return; }
                attributes.WeaponHandler.AimDownSights(true);
            }
            else
            {
                if (!heavyAttackAction.enabled) { return; }
                attributes.WeaponHandler.HeavyAttack(true);
            }
        }

        public void StopHeavyAttack()
        {
            if (attributes.WeaponHandler.CanADS)
            {
                attributes.WeaponHandler.AimDownSights(false);
            }
            else
            {
                attributes.WeaponHandler.HeavyAttack(false);
            }
        }

        private InputAction ability1Action;
        public void Ability1(bool isPressed)
        {
            if (!ability1Action.enabled) { isPressed = false; }
            attributes.WeaponHandler.Ability1(isPressed);
        }

        private InputAction ability2Action;
        public void Ability2(bool isPressed)
        {
            if (!ability2Action.enabled) { isPressed = false; }
            attributes.WeaponHandler.Ability2(isPressed);
        }

        private InputAction ability3Action;
        public void Ability3(bool isPressed)
        {
            if (!ability3Action.enabled) { isPressed = false; }
            attributes.WeaponHandler.Ability3(isPressed);
        }

        private InputAction ability4Action;
        public void Ability4(bool isPressed)
        {
            if (!ability4Action.enabled) { isPressed = false; }
            attributes.WeaponHandler.Ability4(isPressed);
        }

        public void UpgradeAbility1()
        {
            if (attributes.SessionProgressionHandler.CanUpgradeAbility(attributes.WeaponHandler.GetWeapon().GetAbility1(), attributes.WeaponHandler.GetWeapon()))
            {
                attributes.SessionProgressionHandler.UpgradeAbility(attributes.WeaponHandler.GetWeapon(), attributes.WeaponHandler.GetWeapon().GetAbility1());
            }
        }

        public void UpgradeAbility2()
        {
            if (attributes.SessionProgressionHandler.CanUpgradeAbility(attributes.WeaponHandler.GetWeapon().GetAbility2(), attributes.WeaponHandler.GetWeapon()))
            {
                attributes.SessionProgressionHandler.UpgradeAbility(attributes.WeaponHandler.GetWeapon(), attributes.WeaponHandler.GetWeapon().GetAbility2());
            }
        }

        public void UpgradeAbility3()
        {
            if (attributes.SessionProgressionHandler.CanUpgradeAbility(attributes.WeaponHandler.GetWeapon().GetAbility3(), attributes.WeaponHandler.GetWeapon()))
            {
                attributes.SessionProgressionHandler.UpgradeAbility(attributes.WeaponHandler.GetWeapon(), attributes.WeaponHandler.GetWeapon().GetAbility3());
            }
        }

        public void UpgradeAbility4()
        {
            if (attributes.SessionProgressionHandler.CanUpgradeAbility(attributes.WeaponHandler.GetWeapon().GetAbility4(), attributes.WeaponHandler.GetWeapon()))
            {
                attributes.SessionProgressionHandler.UpgradeAbility(attributes.WeaponHandler.GetWeapon(), attributes.WeaponHandler.GetWeapon().GetAbility4());
            }
        }

        private InputAction reloadAction;
        public void Reload()
        {
            if (!reloadAction.enabled) { return; }
            attributes.WeaponHandler.Reload();
        }

        public void SetOrbitalCamState(bool isPressed)
        {
            if (!actionMapHandler) { return; }
            actionMapHandler.SetOrbitalCamState(isPressed);
        }

        private InputAction dodgeAction;
        public void Dodge()
        {
            if (!dodgeAction.enabled) { return; }
            playerMovementHandler.OnDodge();
        }

        public void Rage() { attributes.OnActivateRage(); }

        public void IncrementFollowPlayer() { playerMovementHandler.OnIncrementFollowPlayer(); }

        public void DecrementFollowPlayer() { playerMovementHandler.OnDecrementFollowPlayer(); }

        public void Interact()
        {
            playerMovementHandler.OnInteract();
        }

        private Attributes attributes;
        private PlayerMovementHandler playerMovementHandler;
        private ActionMapHandler actionMapHandler;
        private PlayerInput playerInput;

        [SerializeField] private Canvas[] aliveUIChildCanvases;
        [SerializeField] private Canvas[] deathUIChildCanvases;

        private Vector3 originalCrosshairScale;

        private void Awake()
        {
            attributes = GetComponentInParent<Attributes>();
            playerMovementHandler = attributes.GetComponent<PlayerMovementHandler>();
            actionMapHandler = attributes.GetComponent<ActionMapHandler>();

            playerInput = attributes.GetComponent<PlayerInput>();
            pauseMenuAction = playerInput.actions.FindAction("Pause");
            inventoryAction = playerInput.actions.FindAction("Inventory");
            scoreboardAction = playerInput.actions.FindAction("Scoreboard");
            switchWeaponAction = playerInput.actions.FindAction("SwitchWeapon");
            lightAttackAction = playerInput.actions.FindAction("LightAttack");
            heavyAttackAction = playerInput.actions.FindAction("HeavyAttack");
            aimAction = playerInput.actions.FindAction("Aim");
            ability1Action = playerInput.actions.FindAction("Ability1");
            ability2Action = playerInput.actions.FindAction("Ability2");
            ability3Action = playerInput.actions.FindAction("Ability3");
            ability4Action = playerInput.actions.FindAction("Ability4");
            reloadAction = playerInput.actions.FindAction("Reload");
            dodgeAction = playerInput.actions.FindAction("Dodge");

            originalCrosshairScale = crosshairImage.transform.localScale;

            RefreshStatus();

            GetOrbitalCameraButton().gameObject.SetActive(ActionMapHandler.CanUseOrbitalCamera());

            statusIconLayoutGroup.Initialize(attributes.StatusAgent);
        }

        private Vector2 equippedWeaponCardTargetAnchoredPosition;
        private Vector2 stowedWeaponCardTargetAnchoredPosition;

        private Vector2 moveJoystickOriginalAnchoredPosition;
        [SerializeField] private CanvasGroup[] canvasGroupsThatAffectOpacity;

        private bool canFadeIn;
        private void Start()
        {
            StartCoroutine(BeginFadeIn());

            equippedWeaponCardTargetAnchoredPosition = ((RectTransform)primaryWeaponCard.transform).anchoredPosition;
            stowedWeaponCardTargetAnchoredPosition = ((RectTransform)secondaryWeaponCard.transform).anchoredPosition;
            RefreshWeaponCardTargetPositions();

            RectTransform rt = (RectTransform)moveJoystick.transform.parent;
            moveJoystickOriginalAnchoredPosition = rt.anchoredPosition;

            fadeToWhiteImage.color = Color.black;

            playerCard.Initialize(GetComponentInParent<Attributes>());

            UpdateTeammateAttributesList();

            UpdateWeapon(false);
        }

        private IEnumerator BeginFadeIn()
        {
            yield return new WaitUntil(() => attributes.WeaponHandler.WeaponInitialized);
            yield return null;
            yield return null;
            canFadeIn = true;
        }

        private void RefreshStatus()
        {
            foreach (CanvasGroup canvasGroup in canvasGroupsThatAffectOpacity)
            {
                canvasGroup.alpha = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            }
            crosshairImage.color = FasterPlayerPrefs.Singleton.GetColor("CrosshairColor");
            crosshairImage.transform.localScale = originalCrosshairScale * FasterPlayerPrefs.Singleton.GetFloat("CrosshairSize");
            crosshairImage.sprite = FasterPlayerPrefs.Singleton.crosshairSprites[FasterPlayerPrefs.Singleton.GetInt("CrosshairStyle")].Result;
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
            if (!attributes.WeaponHandler.WeaponInitialized) { return; }

            if (!forceRefresh)
            {
                if (lastWeapon == attributes.WeaponHandler.GetWeapon())
                {
                    lastWeapon = attributes.WeaponHandler.GetWeapon();
                    return;
                }
            }

            InputControlScheme controlScheme = controlsAsset.FindControlScheme(playerInput.currentControlScheme).Value;

            List<ActionClip> abilities = attributes.WeaponHandler.GetWeapon().GetAbilities();
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
                        ability1.Initialize(abilities[0], binding.ToDisplayString());
                        ability1Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability1Initialized) { ability1.Initialize(abilities[0], ""); }

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
                        ability2.Initialize(abilities[1], binding.ToDisplayString());
                        ability2Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability2Initialized) { ability2.Initialize(abilities[1], ""); }

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
                        ability3.Initialize(abilities[2], binding.ToDisplayString());
                        ability3Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability3Initialized) { ability3.Initialize(abilities[2], ""); }

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
                        ability4.Initialize(abilities[3], binding.ToDisplayString());
                        ability4Initialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!ability4Initialized) { ability4.Initialize(abilities[3], ""); }

            bool healthPotionInitialized = false;
            foreach (InputBinding binding in playerInput.actions["HealthPotion"].bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    string deviceName = device.name.ToLower();
                    deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                    if (binding.path.ToLower().Contains(deviceName.ToLower()))
                    {
                        healthPotionCard.Initialize(binding.ToDisplayString());
                        healthPotionInitialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!healthPotionInitialized) { healthPotionCard.Initialize(""); }

            bool staminaPotionInitialized = false;
            foreach (InputBinding binding in playerInput.actions["StaminaPotion"].bindings)
            {
                bool shouldBreak = false;
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    string deviceName = device.name.ToLower();
                    deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;
                    if (binding.path.ToLower().Contains(deviceName.ToLower()))
                    {
                        staminaPotionCard.Initialize(binding.ToDisplayString());
                        staminaPotionInitialized = true;
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            if (!staminaPotionInitialized) { staminaPotionCard.Initialize(""); }

            lastWeapon = attributes.WeaponHandler.GetWeapon();

            if (primaryWeaponCard.isActiveAndEnabled) { primaryWeaponCard.Initialize(attributes.LoadoutManager, attributes.LoadoutManager.PrimaryWeaponOption.weapon, LoadoutManager.WeaponSlotType.Primary, playerInput, controlsAsset); }
            if (secondaryWeaponCard.isActiveAndEnabled) { secondaryWeaponCard.Initialize(attributes.LoadoutManager, attributes.LoadoutManager.SecondaryWeaponOption.weapon, LoadoutManager.WeaponSlotType.Secondary, playerInput, controlsAsset); }
            if (mobileWeaponCard.isActiveAndEnabled) { mobileWeaponCard.Initialize(attributes.LoadoutManager,
                attributes.LoadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Primary ? attributes.LoadoutManager.PrimaryWeaponOption.weapon : attributes.LoadoutManager.SecondaryWeaponOption.weapon,
                attributes.LoadoutManager.GetEquippedSlotType(), playerInput, controlsAsset); }

            onScreenReloadButton.gameObject.SetActive(attributes.WeaponHandler.ShouldUseAmmo());
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

        private Vector3 equippedWeaponCardTargetLocalPosition;
        private Vector3 stowedWeaponCardTargetLocalPosition;
        private int lastWidthEvaluated;
        private int lastHeightEvaluated;
        private void RefreshWeaponCardTargetPositions()
        {
            if (Screen.width == lastWidthEvaluated & Screen.height == lastHeightEvaluated) { return; }

            if (!primaryWeaponCard.isActiveAndEnabled | !secondaryWeaponCard.isActiveAndEnabled) { return; }

            ((RectTransform)primaryWeaponCard.transform).anchoredPosition = equippedWeaponCardTargetAnchoredPosition;
            ((RectTransform)secondaryWeaponCard.transform).anchoredPosition = stowedWeaponCardTargetAnchoredPosition;

            equippedWeaponCardTargetLocalPosition = primaryWeaponCard.transform.localPosition;
            stowedWeaponCardTargetLocalPosition = secondaryWeaponCard.transform.localPosition;

            lastWidthEvaluated = Screen.width;
            lastHeightEvaluated = Screen.height;
        }

        private const float weaponCardAnimationSpeed = 8;
        private void UpdateWeaponCardPositions()
        {
            if (!primaryWeaponCard.isActiveAndEnabled | !secondaryWeaponCard.isActiveAndEnabled) { return; }

            bool primaryIsEquipped = attributes.LoadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Primary;

            primaryWeaponCard.transform.localPosition = Vector3.Lerp(primaryWeaponCard.transform.localPosition, primaryIsEquipped ? equippedWeaponCardTargetLocalPosition : stowedWeaponCardTargetLocalPosition, Time.deltaTime * weaponCardAnimationSpeed);
            secondaryWeaponCard.transform.localPosition = Vector3.Lerp(secondaryWeaponCard.transform.localPosition, primaryIsEquipped ? stowedWeaponCardTargetLocalPosition : equippedWeaponCardTargetLocalPosition, Time.deltaTime * weaponCardAnimationSpeed);
        }

        private void OnEnable()
        {
            RefreshStatus();

            if (attributes.WeaponHandler.WeaponInitialized)
            {
                canFadeIn = true;
            }

            lastNumDodgesEvaluated = -1;

            tooltipImage.color = StringUtility.SetColorAlpha(tooltipImage.color, 0);
            mobileInteractableImage.color = StringUtility.SetColorAlpha(mobileInteractableImage.color, 0);
        }

        public const float alphaTransitionSpeed = 5;

        List<Attributes> teammateAttributes = new List<Attributes>();
        private string lastControlScheme;
        private int moveTouchId;
        private int lastNumDodgesEvaluated = -1;
        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }
            if (!attributes.WeaponHandler.WeaponInitialized) { return; }

            scoreboardButton.gameObject.SetActive(GameModeManager.Singleton);

            bool overrideDodgeColor = false;
            if (playerMovementHandler.TryGetNetworkInteractableInRange(out NetworkInteractable networkInteractable))
            {
                if (mobileInteractableImage.gameObject.activeInHierarchy)
                {
                    if (!mobileInteractableImage.raycastTarget) { mobileInteractableImage.raycastTarget = true; }
                    if (blockingButton.raycastTarget) { blockingButton.raycastTarget = false; }

                    ability1.CrossFadeOpacity(0);
                    ability2.CrossFadeOpacity(0);
                    ability3.CrossFadeOpacity(0);
                    ability4.CrossFadeOpacity(0);
                    healthPotionCard.CrossFadeOpacity(0);
                    staminaPotionCard.CrossFadeOpacity(0);
                    overrideDodgeColor = true;

                    float newBlockingButtonAlpha = Mathf.MoveTowards(blockingButton.color.a, 0, Time.deltaTime * alphaTransitionSpeed);
                    if (!Mathf.Approximately(blockingButton.color.a, newBlockingButtonAlpha))
                    {
                        blockingButton.color = StringUtility.SetColorAlpha(blockingButton.color, newBlockingButtonAlpha);
                    }
                    
                    float newLookJoystickAlpha = Mathf.MoveTowards(lookJoystickCenter.color.a, 0, Time.deltaTime * alphaTransitionSpeed);
                    if (!Mathf.Approximately(lookJoystickCenter.color.a, newLookJoystickAlpha))
                    {
                        lookJoystickCenter.color = StringUtility.SetColorAlpha(lookJoystickCenter.color, newLookJoystickAlpha);
                    }
                    else
                    {
                        float newInteractableImageAlpha = Mathf.MoveTowards(mobileInteractableImage.color.a, 1, Time.deltaTime * alphaTransitionSpeed);
                        if (!Mathf.Approximately(newInteractableImageAlpha, mobileInteractableImage.color.a))
                        {
                            mobileInteractableImage.color = StringUtility.SetColorAlpha(mobileInteractableImage.color, newInteractableImageAlpha);
                        }
                    }
                }
                else
                {
                    Color target = Vector4.MoveTowards(tooltipImage.color, new Color(1, 1, 1, 0.65f), Time.deltaTime * alphaTransitionSpeed);
                    if (tooltipImage.color != target)
                    {
                        tooltipImage.color = target;
                    }
                }
            }
            else
            {
                if (mobileInteractableImage.gameObject.activeInHierarchy)
                {
                    if (mobileInteractableImage.raycastTarget) { mobileInteractableImage.raycastTarget = false; }
                    if (!blockingButton.raycastTarget) { blockingButton.raycastTarget = true; }

                    float newInteractableImageAlpha = Mathf.MoveTowards(mobileInteractableImage.color.a, 0, Time.deltaTime * alphaTransitionSpeed);
                    if (!Mathf.Approximately(newInteractableImageAlpha, mobileInteractableImage.color.a))
                    {
                        mobileInteractableImage.color = StringUtility.SetColorAlpha(mobileInteractableImage.color,  newInteractableImageAlpha);
                        overrideDodgeColor = true;
                    }
                    else
                    {
                        ability1.CrossFadeOpacity(1);
                        ability2.CrossFadeOpacity(1);
                        ability3.CrossFadeOpacity(1);
                        ability4.CrossFadeOpacity(1);
                        healthPotionCard.CrossFadeOpacity(1);
                        staminaPotionCard.CrossFadeOpacity(1);

                        float newBlockingButtonAlpha = Mathf.MoveTowards(blockingButton.color.a, 1, Time.deltaTime * alphaTransitionSpeed);
                        if (!Mathf.Approximately(blockingButton.color.a, newBlockingButtonAlpha))
                        {
                            blockingButton.color = StringUtility.SetColorAlpha(blockingButton.color, newBlockingButtonAlpha);
                        }

                        float newLookJoystickAlpha = Mathf.MoveTowards(lookJoystickCenter.color.a, 1, Time.deltaTime * alphaTransitionSpeed);
                        if (!Mathf.Approximately(lookJoystickCenter.color.a, newLookJoystickAlpha))
                        {
                            lookJoystickCenter.color = StringUtility.SetColorAlpha(lookJoystickCenter.color, newLookJoystickAlpha);
                        }
                    }
                }
                else
                {
                    Color target = Vector4.MoveTowards(tooltipImage.color, Color.clear, Time.deltaTime * alphaTransitionSpeed);
                    if (tooltipImage.color != target)
                    {
                        tooltipImage.color = target;
                    }
                }
            }

            if (overrideDodgeColor)
            {
                if (dodgeButton.raycastTarget) { dodgeButton.raycastTarget = false; }
            }
            else
            {
                if (!dodgeButton.raycastTarget) { dodgeButton.raycastTarget = true; }
            }

            if (overrideDodgeColor)
            {
                float newAlpha = Mathf.MoveTowards(dodgeButton.color.a, 0, Time.deltaTime * alphaTransitionSpeed);
                if (!Mathf.Approximately(dodgeButton.color.a, newAlpha))
                {
                    dodgeButton.color = StringUtility.SetColorAlpha(dodgeButton.color, newAlpha);
                }
            }
            else if (!attributes.AnimationHandler.AreActionClipRequirementsMet(attributes.WeaponHandler.GetWeapon().GetDodgeClip(0)))
            {
                if (!Mathf.Approximately(dodgeButton.color.a, 0.15f))
                {
                    dodgeButton.color = StringUtility.SetColorAlpha(dodgeButton.color, 0.15f);
                }
            }
            else if (attributes.WeaponHandler.GetWeapon().IsDodgeOnCooldown())
            {
                float dodgeCooldownProgress = attributes.WeaponHandler.GetWeapon().GetDodgeCooldownProgress();
                float newAlpha = Mathf.Lerp(0.15f, 1, dodgeCooldownProgress);
                if (!Mathf.Approximately(dodgeButton.color.a, newAlpha))
                {
                    dodgeButton.color = StringUtility.SetColorAlpha(dodgeButton.color, newAlpha);
                }
            }
            else
            {
                if (!Mathf.Approximately(dodgeButton.color.a, 1))
                {
                    dodgeButton.color = StringUtility.SetColorAlpha(dodgeButton.color, 1);
                }
            }

            int numOfDodges = attributes.WeaponHandler.GetWeapon().GetNumberOfDodgesOffCooldown();
            if (numOfDodges != lastNumDodgesEvaluated)
            {
                dodgeStackText.text = numOfDodges.ToString();
                if (numOfDodges > 0)
                {
                    dodgeStackText.text += "x";
                }
            }
            lastNumDodgesEvaluated = numOfDodges;

            if (overrideDodgeColor)
            {
                float newAlpha = Mathf.MoveTowards(dodgeStackText.color.a, 0, Time.deltaTime * alphaTransitionSpeed);
                if (!Mathf.Approximately(dodgeStackText.color.a, newAlpha))
                {
                    dodgeStackText.color = StringUtility.SetColorAlpha(dodgeStackText.color, newAlpha);
                    dodgeBackgroundTextImage.color = StringUtility.SetColorAlpha(dodgeBackgroundTextImage.color, newAlpha);
                }
            }
            else
            {
                float newAlpha = Mathf.MoveTowards(dodgeStackText.color.a, 1, Time.deltaTime * alphaTransitionSpeed);
                if (!Mathf.Approximately(dodgeStackText.color.a, newAlpha))
                {
                    dodgeStackText.color = StringUtility.SetColorAlpha(dodgeStackText.color, newAlpha);
                    dodgeBackgroundTextImage.color = StringUtility.SetColorAlpha(dodgeBackgroundTextImage.color, newAlpha);
                }
            }

            if (attributes.GetAilment() != ActionClip.Ailment.Death)
            {
                if (!FasterPlayerPrefs.IsMobilePlatform)
                {
                    if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateTeammateAttributesList(); }

                    // Order player cards by distance
                    List<Attributes> orderedTeammateAttributes = teammateAttributes.Where(item => item).OrderBy(item => item.GetAilment() == ActionClip.Ailment.Death).ThenBy(x => Vector3.Distance(attributes.transform.position, x.transform.position)).Take(teammatePlayerCards.Length).ToList();
                    for (int i = 0; i < teammatePlayerCards.Length; i++)
                    {
                        if (i < orderedTeammateAttributes.Count)
                        {
                            teammatePlayerCards[i].Initialize(orderedTeammateAttributes[i]);
                        }
                        else
                        {
                            teammatePlayerCards[i].Initialize(null);
                        }
                    }
                }

                bool gameModeManagerShouldFadeToBlack = false;
                if (GameModeManager.Singleton)
                {
                    gameModeManagerShouldFadeToBlack = GameModeManager.Singleton.ShouldFadeToBlack();
                }

                if (canFadeIn & !SceneLoadingUI.IsDisplaying)
                {
                    if (shouldFadeToBlack | gameModeManagerShouldFadeToBlack)
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
            }
            else // Dead
            {
                CombatAgent killerCombatAgent = null;
                if (attributes.TryGetKiller(out NetworkObject killerNetObj)) { killerCombatAgent = killerNetObj.GetComponent<CombatAgent>(); }

                if (killerCombatAgent)
                {
                    killerCard.Initialize(killerCombatAgent);
                    killedByText.text = "KO'd by";
                }
                else
                {
                    killerCard.Initialize(null);
                    killedByText.text = "KO'd by " + (killerNetObj ? killerNetObj.name.Replace("(Clone)", "") : "Unknown");
                }

                bool isGameOver = false;
                bool alreadyFading = false;
                bool gameModeManagerShouldFadeToBlack = false;
                if (GameModeManager.Singleton)
                {
                    gameModeManagerShouldFadeToBlack = GameModeManager.Singleton.ShouldFadeToBlack();
                    isGameOver = GameModeManager.Singleton.IsGameOver();
                }

                respawnTimerText.text = attributes.IsRespawning & !isGameOver ? "Respawning in " + attributes.GetRespawnTime().ToString("F0") : "";

                if (shouldFadeToBlack | gameModeManagerShouldFadeToBlack)
                {
                    fadeToBlackImage.color = Vector4.MoveTowards(fadeToBlackImage.color, Color.black, Time.deltaTime);
                    fadeToWhiteImage.color = Vector4.MoveTowards(fadeToWhiteImage.color, Color.black, Time.deltaTime);
                    alreadyFading = true;
                }

                if (attributes.IsRespawning & !isGameOver)
                {
                    if (!alreadyFading)
                    {
                        fadeToBlackImage.color = Color.Lerp(Color.clear, Color.black, attributes.GetRespawnTimeAsPercentage());
                        fadeToWhiteImage.color = fadeToBlackImage.color;
                    }
                    
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
            RefreshWeaponCardTargetPositions();
            UpdateWeaponCardPositions();
            UpdateWeapon(playerInput.currentControlScheme != lastControlScheme);

            lastControlScheme = playerInput.currentControlScheme;

            heavyAttackButton.sprite = attributes.WeaponHandler.CanADS ? aimIcon : heavyAttackIcon;
        }
    }
}