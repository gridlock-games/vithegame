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

        public RectTransform GetBlockingButton() { return blockingButton; }

        public RectTransform GetDodgeButton() { return dodgeButton; }

        public PlayerCard GetMainPlayerCard() { return playerCard; }

        public RectTransform GetLookJoystickCenter() { return lookJoystickCenter; }

        public RuntimeWeaponCard[] GetWeaponCards() { return GetComponentsInChildren<RuntimeWeaponCard>(true); }

        public RectTransform GetSwitchWeaponButton() { return switchWeaponButton; }

        public RectTransform GetOnScreenReloadButton() { return onScreenReloadButton; }

        public void RefreshOnScreenReloadButtonInteractability() { onScreenReloadButton.gameObject.SetActive(attributes.WeaponHandler.CanADS); }

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
        [Header("Text Chat")]
        [SerializeField] private Canvas textChatButtonCanvas;
        [SerializeField] private Canvas textChatParentCanvas;
        [SerializeField] private Scrollbar chatScrollbar;
        [SerializeField] private RectTransform textChatElementParent;
        [SerializeField] private InputField textChatInputField;
        [SerializeField] private GameObject textChatElementPrefab;
        [SerializeField] private Button openTextChatButton;
        [SerializeField] private Text textChatMessageNumberText;

        private List<StatusIcon> statusIcons = new List<StatusIcon>();

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

        public void OpenTextChat()
        {
            actionMapHandler.OnTextChat();
        }

        private InputAction textChatAction;
        private int unreadMessageCount;
        private void OnTextChat()
        {
            if (!textChatAction.enabled & playerInput.currentActionMap.name == playerInput.defaultActionMap) { return; }

            textChatParentCanvas.enabled = !textChatParentCanvas.enabled;
            if (textChatParentCanvas.enabled)
            {
                ScrollToBottomOfTextChat();
                actionMapHandler.OnTextChatOpen();
                if (Application.platform != RuntimePlatform.Android & Application.platform != RuntimePlatform.IPhonePlayer) { textChatInputField.ActivateInputField(); }
                unreadMessageCount = 0;
                textChatMessageNumberText.text = "";
            }
            else
            {
                actionMapHandler.OnTextChatClose();
            }
            textChatButtonCanvas.enabled = Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer ? !textChatParentCanvas.enabled : !textChatParentCanvas.enabled & unreadMessageCount > 0;
        }

        public void CloseTextChat()
        {
            textChatParentCanvas.enabled = false;
            if (Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer)
            {
                textChatButtonCanvas.enabled = true;
            }
            else
            {
                textChatButtonCanvas.enabled = unreadMessageCount > 0;
            }
            actionMapHandler.OnTextChatClose();
        }

        public void SendTextChat()
        {
            textChat.SendTextChat(PlayerDataManager.Singleton.LocalPlayerData.character.name.ToString(), PlayerDataManager.Singleton.LocalPlayerData.team, textChatInputField.text);
            textChatInputField.text = "";
            if (Application.platform != RuntimePlatform.Android & Application.platform != RuntimePlatform.IPhonePlayer) { textChatInputField.ActivateInputField(); }
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

        private InputAction dodgeAction;
        public void Dodge()
        {
            if (!dodgeAction.enabled) { return; }
            playerMovementHandler.OnDodge();
        }

        public void Block(bool isPressed) { attributes.WeaponHandler.Block(isPressed); }

        public void Rage() { attributes.OnActivateRage(); }

        public void IncrementFollowPlayer() { playerMovementHandler.OnIncrementFollowPlayer(); }

        public void DecrementFollowPlayer() { playerMovementHandler.OnDecrementFollowPlayer(); }

        private Attributes attributes;
        private PlayerMovementHandler playerMovementHandler;
        private TextChat textChat;
        private ActionMapHandler actionMapHandler;
        private PlayerInput playerInput;

        [SerializeField] private Canvas[] aliveUIChildCanvases;
        [SerializeField] private Canvas[] deathUIChildCanvases;

        private void Awake()
        {
            attributes = GetComponentInParent<Attributes>();
            playerMovementHandler = attributes.GetComponent<PlayerMovementHandler>();
            actionMapHandler = attributes.GetComponent<ActionMapHandler>();
            textChat = attributes.GetComponent<TextChat>();

            playerInput = attributes.GetComponent<PlayerInput>();
            pauseMenuAction = playerInput.actions.FindAction("Pause");
            inventoryAction = playerInput.actions.FindAction("Inventory");
            scoreboardAction = playerInput.actions.FindAction("Scoreboard");
            textChatAction = playerInput.actions.FindAction("TextChat");
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

            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            RefreshStatus();

            textChatParentCanvas.enabled = false;
            if (Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer)
            {
                textChatButtonCanvas.enabled = true;
            }
            else
            {
                textChatButtonCanvas.enabled = unreadMessageCount > 0;
            }
        }

        private Vector2 equippedWeaponCardTargetAnchoredPosition;
        private Vector2 stowedWeaponCardTargetAnchoredPosition;

        private Vector2 moveJoystickOriginalAnchoredPosition;
        private CanvasGroup[] canvasGroups;
        private void Start()
        {
            GetComponent<Canvas>().enabled = false;
            StartCoroutine(EnableCanvas());

            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, statusImageParent).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
            }

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

        private IEnumerator EnableCanvas()
        {
            yield return new WaitUntil(() => attributes.WeaponHandler.WeaponInitialized);
            yield return null;
            yield return null;
            GetComponent<Canvas>().enabled = true;
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
            teammateAttributes = PlayerDataManager.Singleton.GetCombatAgentsOnTeam(attributes.GetTeam(), attributes);
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

            List<ActionClip.Status> activeStatuses = attributes.StatusAgent.GetActiveStatuses();
            foreach (StatusIcon statusIcon in statusIcons)
            {
                if (activeStatuses.Contains(statusIcon.Status))
                {
                    statusIcon.SetActive(true);
                    statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 4);
                }
                else
                {
                    statusIcon.SetActive(false);
                }
            }
        }

        public void ScrollToBottomOfTextChat()
        {
            chatScrollbar.value = 0;
            StartCoroutine(ScrollToBottomOfTextChatAfterOneFrame());
        }

        private IEnumerator ScrollToBottomOfTextChatAfterOneFrame()
        {
            yield return null;
            yield return null;
            chatScrollbar.value = 0;
        }

        public void ScrollToTopOfTextChat() { chatScrollbar.value = 1; }
        public void ScrollALittleDownTextChat() { chatScrollbar.value -= 0.1f; }
        public void ScrollALittleUpTextChat() { chatScrollbar.value += 0.1f; }

        public void DisplayNextTextElement(TextChat.TextChatElement textChatElement)
        {
            Text text = Instantiate(textChatElementPrefab, textChatElementParent).GetComponent<Text>();
            text.text = textChatElement.GetMessageUIValue();
            if (textChatParentCanvas.enabled)
            {
                ScrollToBottomOfTextChat();
            }
            else
            {
                unreadMessageCount++;
                if (unreadMessageCount == 0)
                {
                    textChatMessageNumberText.text = "";
                }
                else if (unreadMessageCount > 99)
                {
                    textChatMessageNumberText.text = "99+";
                }
                else
                {
                    textChatMessageNumberText.text = unreadMessageCount.ToString();
                }

                if (Application.platform != RuntimePlatform.Android & Application.platform != RuntimePlatform.IPhonePlayer)
                {
                    textChatButtonCanvas.enabled = unreadMessageCount > 0;
                }
            }
        }

        public void DisplayConnectionMessage(string connectionMessage)
        {
            Text text = Instantiate(textChatElementPrefab, textChatElementParent).GetComponent<Text>();
            text.text = connectionMessage;
            if (textChatParentCanvas.enabled)
            {
                ScrollToBottomOfTextChat();
            }
        }

        List<CombatAgent> teammateAttributes = new List<CombatAgent>();
        private string lastControlScheme;
        private int moveTouchId;
        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }
            if (!attributes.WeaponHandler.WeaponInitialized) { return; }

            scoreboardButton.gameObject.SetActive(GameModeManager.Singleton);

            float dodgeCooldownProgress = attributes.WeaponHandler.GetWeapon().GetDodgeCooldownProgress();
            mobileDodgeCooldownImage.fillAmount = 1 - dodgeCooldownProgress;
            dodgeCooldownImage.fillAmount = 1 - dodgeCooldownProgress;

            bool dodgeIsOnCooldown = !Mathf.Approximately(dodgeCooldownProgress, 1);
            dodgeIconImageOnPC.enabled = dodgeIsOnCooldown;
            dodgeCooldownImage.enabled = dodgeIsOnCooldown;
            mobileDodgeCooldownImage.enabled = dodgeIsOnCooldown;

            if (attributes.StatusAgent.ActiveStatusesWasUpdatedThisFrame)
            {
                List<ActionClip.Status> activeStatuses = attributes.StatusAgent.GetActiveStatuses();
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    if (activeStatuses.Contains(statusIcon.Status))
                    {
                        statusIcon.SetActive(true);
                        statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 4);
                    }
                    else
                    {
                        statusIcon.SetActive(false);
                    }
                }
            }

            if (attributes.GetAilment() != ActionClip.Ailment.Death)
            {
                if (Application.platform != RuntimePlatform.Android & Application.platform != RuntimePlatform.IPhonePlayer)
                {
                    if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateTeammateAttributesList(); }

                    // Order player cards by distance
                    List<CombatAgent> orderedTeammateAttributes = teammateAttributes.OrderBy(item => item.GetAilment() == ActionClip.Ailment.Death).ThenBy(x => Vector3.Distance(attributes.transform.position, x.transform.position)).Take(teammatePlayerCards.Length).ToList();
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
            else
            {
                NetworkObject killerNetObj = attributes.GetKiller();
                CombatAgent killerCombatAgent = null;
                if (killerNetObj) { killerCombatAgent = killerNetObj.GetComponent<CombatAgent>(); }

                if (killerCombatAgent)
                {
                    killerCard.Initialize(killerCombatAgent);
                    killedByText.text = "Killed by";
                }
                else
                {
                    killerCard.Initialize(null);
                    killedByText.text = "Killed by " + (killerNetObj ? killerNetObj.name.Replace("(Clone)", "") : "Unknown");
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