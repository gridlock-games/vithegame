using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Player;
using Vi.Utility;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Core.CombatAgents;

namespace Vi.UI
{
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text overlayText;
        [SerializeField] private Text timerText;
        [SerializeField] private Image[] overlayImages;
        [SerializeField] private Image objectiveCompleteImage;
        [SerializeField] private HorizontalLayoutGroup imagesLayoutGroup;
        [SerializeField] private CanvasGroup mainGroup;
        [SerializeField] private Text[] endingMessages;

        PlayerInput playerInput;
        PlayerMovementHandler playerMovementHandler;
        Attributes attributes;
        WeaponHandler weaponHandler;
        AnimationHandler animationHandler;
        LoadoutManager loadoutManager;
        PlayerUI playerUI;

        private void FindPlayerInput()
        {
            if (playerInput)
            {
                if (!playerInput.gameObject.activeInHierarchy)
                {
                    playerInput = null;
                    playerMovementHandler = null;
                    attributes = null;
                    weaponHandler = null;
                    animationHandler = null;
                    loadoutManager = null;
                    playerUI = null;
                }
            }

            if (playerInput) { return; }
            if (!PlayerDataManager.DoesExist()) { return; }
            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer)
            {
                playerInput = localPlayer.GetComponent<PlayerInput>();
                playerMovementHandler = localPlayer.GetComponent<PlayerMovementHandler>();
                attributes = localPlayer;
                weaponHandler = localPlayer.WeaponHandler;
                animationHandler = localPlayer.AnimationHandler;
                loadoutManager = localPlayer.GetComponent<LoadoutManager>();
                playerUI = localPlayer.GetComponentInChildren<PlayerUI>();
                playerUI.GetTextButtonCanvas().gameObject.SetActive(false);

                foreach (RuntimeWeaponCard weaponCard in playerUI.GetWeaponCards())
                {
                    weaponCard.SetActive(false);
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.SetActive(false);
                }

                playerUI.GetSwitchWeaponButton().gameObject.SetActive(false);
                playerUI.GetBlockingButton().gameObject.SetActive(false);
                playerUI.GetDodgeButton().gameObject.SetActive(false);
                playerUI.GetMainPlayerCard().gameObject.SetActive(false);
                playerUI.GetOrbitalCameraButton().gameObject.SetActive(false);
                if (!weaponHandler.CanADS)
                {
                    playerUI.GetHeavyAttackButton().gameObject.SetActive(false);
                }

                playerUI.GetPauseMenuButton().gameObject.SetActive(false);
                playerUI.GetScoreboardButton().gameObject.SetActive(false);
                playerUI.GetLoadoutMenuButton().gameObject.SetActive(false);

                playerUI.GetHealthPotionCard().gameObject.SetActive(false);
                playerUI.GetStaminaPotionCard().gameObject.SetActive(false);
            }
        }

        private RectTransform layoutGroupRT;
        private Vector2 originalAnchoredPosition;

        private void Awake()
        {
            FindPlayerInput();
            FasterPlayerPrefs.Singleton.SetBool("DisableBots", true);
            FasterPlayerPrefs.Singleton.SetBool("BotsCanOnlyLightAttack", true);

            foreach (Image image in overlayImages)
            {
                image.gameObject.SetActive(false);
                image.preserveAspect = true;
            }

            layoutGroupRT = (RectTransform)imagesLayoutGroup.transform;
            originalAnchoredPosition = layoutGroupRT.anchoredPosition;

            objectiveCompleteImage.color = new Color(1, 1, 1, 0);

            backgroundImage.enabled = false;
            StartCoroutine(DisplayNextActionAfterPlayerInputFound());
        }

        private IEnumerator DisplayNextActionAfterPlayerInputFound()
        {
            yield return new WaitUntil(() => playerInput);
            yield return null;

            foreach (InputAction action in playerInput.actions)
            {
                if (!action.name.Contains("Look") & !action.name.Contains("Aim")) { playerInput.actions.FindAction(action.name).Disable(); }
            }

            yield return new WaitUntil(() => !attributes.GetComponent<PlayerMovementHandler>().IsCameraAnimating());
            StartCoroutine(DisplayNextAction());
        }

        private const float animationSpeed = 100;

        private float maxOffset = 100;

        private float positionOffset;
        private float directionMultiplier = -1;

        private int currentActionIndex = -1;

        private const float colorDistance = 0.001f;

        private const float forwardSpawnPosMultiplier = 2;

        private IEnumerator DisplayNextAction()
        {
            currentActionIndex += 1;

            currentOverlaySprites.Clear();
            currentOverlayMessage = "";

            shouldLockCameraOnBot = false;
            shouldAnimatePosition = false;
            timerEnabled = false;

            canProceed = false;
            canProceedCondition1 = false;

            onTaskCompleteBufferDuration = 3;
            checkmarkDuration = 1;
            bufferDurationBetweenActions = 3;

            onTaskCompleteStartTime = Mathf.NegativeInfinity;
            checkmarkStartTime = Mathf.NegativeInfinity;
            bufferStartTime = Mathf.NegativeInfinity;

            Time.timeScale = 1;

            if (locationPingInstance) { Destroy(locationPingInstance); }

            foreach (GameObject instance in UIElementHighlightInstances)
            {
                Destroy(instance);
            }

            InputControlScheme controlScheme = playerInput.actions.FindControlScheme(playerInput.currentControlScheme).Value;
            if (currentActionIndex == 0) // Look
            {
                shouldAnimatePosition = true;

                List<Sprite> controlSchemeSpriteList = PlayerDataManager.Singleton.GetControlsImageMapping().GetControlSchemeActionImages(controlScheme, playerInput.actions["Look"]);
                if (controlSchemeSpriteList.Count > 0)
                {
                    currentOverlaySprites = controlSchemeSpriteList;
                }
                else if (controlScheme.name != "Touchscreen")
                {
                    var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Look"] });
                    currentOverlaySprites = result.pressedSprites;
                }

                currentOverlayMessage = "Look Around.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (!action.name.Contains("Look") & !action.name.Contains("Aim")) { playerInput.actions.FindAction(action.name).Disable(); }
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetLookJoystickCenter(), true));
            }
            else if (currentActionIndex == 1) // Move
            {
                onTaskCompleteBufferDuration = 0;
                checkmarkDuration = 2;

                List<Sprite> controlSchemeSpriteList = PlayerDataManager.Singleton.GetControlsImageMapping().GetControlSchemeActionImages(controlScheme, playerInput.actions["Move"]);
                if (controlSchemeSpriteList.Count > 0)
                {
                    currentOverlaySprites = controlSchemeSpriteList;
                }
                else if (controlScheme.name != "Touchscreen")
                {
                    var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Move"] });
                    currentOverlaySprites = result.releasedSprites;
                }

                currentOverlayMessage = "Move To The Marked Location.";
                PlayerDataManager.Singleton.AddBotData(PlayerDataManager.Team.Competitor, true);
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Move")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                yield return new WaitUntil(() => ShouldCheckmarkBeDisplayed());

                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Move")) { playerInput.actions.FindAction(action.name).Disable(); }
                }
            }
            else if (currentActionIndex == 2) // Attack
            {
                List<Sprite> controlSchemeSpriteList = PlayerDataManager.Singleton.GetControlsImageMapping().GetControlSchemeActionImages(controlScheme, playerInput.actions["LightAttack"]);
                if (controlSchemeSpriteList.Count > 0)
                {
                    currentOverlaySprites = controlSchemeSpriteList;
                }
                else if (controlScheme.name != "Touchscreen")
                {
                    var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["LightAttack"] });
                    currentOverlaySprites = result.pressedSprites;
                }

                currentOverlayMessage = "Attack The Enemy.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("LightAttack") | action.name.Contains("Move") | action.name.Contains("Aim")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetLookJoystickCenter(), true));

                yield return new WaitUntil(() => IsTaskComplete());

                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Aim")) { continue; }
                    playerInput.actions.FindAction(action.name).Disable();
                }
            }
            else if (currentActionIndex == 3) // Combo
            {
                onTaskCompleteBufferDuration = 2;

                List<Sprite> controlSchemeSpriteList = PlayerDataManager.Singleton.GetControlsImageMapping().GetControlSchemeActionImages(controlScheme, playerInput.actions["LightAttack"]);
                if (controlSchemeSpriteList.Count > 0)
                {
                    currentOverlaySprites = controlSchemeSpriteList;
                    currentOverlaySprites.AddRange(controlSchemeSpriteList);
                    currentOverlaySprites.AddRange(controlSchemeSpriteList);
                }
                else if (controlScheme.name != "Touchscreen")
                {
                    var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["LightAttack"] });
                    currentOverlaySprites.AddRange(result.pressedSprites);
                    currentOverlaySprites.AddRange(result.pressedSprites);
                    currentOverlaySprites.AddRange(result.pressedSprites);
                }

                currentOverlayMessage = "Perform A Combo On The Enemy.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("LightAttack") | action.name.Contains("Move") | action.name.Contains("Look") | action.name.Contains("Aim")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
                attributes.ResetComboCounter();

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetLookJoystickCenter(), true));

                yield return new WaitUntil(() => IsTaskComplete());

                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Aim")) { continue; }
                    playerInput.actions.FindAction(action.name).Disable();
                }

                yield return new WaitUntil(() => !IsTaskComplete() & !ShouldCheckmarkBeDisplayed() & IsInBufferTime());
                bufferDurationBetweenActions = 6;
                playerUI.SetFadeToBlack(true);
                yield return new WaitUntil(() => Vector4.Distance(playerUI.GetFadeToBlackColor(), Color.black) < colorDistance);
                PlayerDataManager.Singleton.RespawnAllPlayers();
                yield return new WaitForSeconds(0.5f);
                playerMovementHandler.SetOrientation(botAttributes.transform.position + botAttributes.transform.forward * forwardSpawnPosMultiplier, playerMovementHandler.transform.rotation);
                playerUI.SetFadeToBlack(false);
            }
            else if (currentActionIndex == 4) // Ability 1, 2, or 3
            {
                shouldLockCameraOnBot = true;

                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Aim")) { continue; }
                    playerInput.actions.FindAction(action.name).Disable();
                }

                yield return new WaitForSeconds(2);

                currentOverlayMessage = "Use An Ability.";
                List<string> abilityNames = new List<string>() { "Ability1", "Ability2", "Ability3" };
                foreach (InputAction action in playerInput.actions)
                {
                    if (abilityNames.Contains(action.name) | action.name.Contains("Aim")) { playerInput.actions.FindAction(action.name).Enable(); }
                    else { playerInput.actions.FindAction(action.name).Disable(); }
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.SetActive(true);
                    abilityCard.transform.localScale = abilityNames.Contains(abilityCard.Ability.name) ? new Vector3(1.5f, 1.5f, 1.5f) : Vector3.one;
                    if (abilityNames.Contains(abilityCard.Ability.name)) { UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, abilityCard.transform, true)); }
                }

                yield return new WaitUntil(() => !IsTaskComplete() & ShouldCheckmarkBeDisplayed());

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.transform.localScale = Vector3.one;
                }
            }
            else if (currentActionIndex == 5) // Ability 4
            {
                shouldLockCameraOnBot = true;

                currentOverlayMessage = "Use Your Ultimate Ability.";
                List<string> abilityNames = new List<string>() { "Ability4" };
                foreach (InputAction action in playerInput.actions)
                {
                    if (abilityNames.Contains(action.name) | action.name.Contains("Aim")) { playerInput.actions.FindAction(action.name).Enable(); }
                    else { playerInput.actions.FindAction(action.name).Disable(); }
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.transform.localScale = abilityNames.Contains(abilityCard.Ability.name) ? new Vector3(1.5f, 1.5f, 1.5f) : Vector3.one;
                    if (abilityNames.Contains(abilityCard.Ability.name)) UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, abilityCard.transform, true));
                }

                yield return new WaitUntil(() => !IsTaskComplete() & ShouldCheckmarkBeDisplayed());

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.transform.localScale = Vector3.one;
                }

                yield return new WaitUntil(() => !IsTaskComplete() & !ShouldCheckmarkBeDisplayed() & IsInBufferTime());
                bufferDurationBetweenActions = 6;
                playerUI.SetFadeToBlack(true);
                yield return new WaitUntil(() => Vector4.Distance(playerUI.GetFadeToBlackColor(), Color.black) < colorDistance);
                PlayerDataManager.Singleton.RespawnAllPlayers();
                yield return new WaitForSeconds(0.5f);
                playerMovementHandler.SetOrientation(botAttributes.transform.position + botAttributes.transform.forward * forwardSpawnPosMultiplier, playerMovementHandler.transform.rotation);
                playerUI.SetFadeToBlack(false);
            }
            else if (currentActionIndex == 6) // Dodge
            {
                playerUI.GetDodgeButton().gameObject.SetActive(true);

                List<Sprite> controlSchemeSpriteList = PlayerDataManager.Singleton.GetControlsImageMapping().GetControlSchemeActionImages(controlScheme, playerInput.actions["Dodge"]);
                if (controlSchemeSpriteList.Count > 0)
                {
                    currentOverlaySprites = controlSchemeSpriteList;
                }
                else if (controlScheme.name != "Touchscreen")
                {
                    var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Dodge"] });
                    currentOverlaySprites = result.releasedSprites;
                }

                currentOverlayMessage = "Dodge.";
                FasterPlayerPrefs.Singleton.SetBool("DisableBots", false);
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Dodge") | action.name.Contains("Look")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetDodgeButton().transform, true));

                yield return new WaitUntil(() => ShouldCheckmarkBeDisplayed());

                FasterPlayerPrefs.Singleton.SetBool("DisableBots", true);
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Aim")) { continue; }
                    playerInput.actions.FindAction(action.name).Disable();
                }

                yield return new WaitUntil(() => !IsTaskComplete() & !ShouldCheckmarkBeDisplayed() & IsInBufferTime());
                bufferDurationBetweenActions = 6;
                playerUI.SetFadeToBlack(true);
                yield return new WaitUntil(() => Vector4.Distance(playerUI.GetFadeToBlackColor(), Color.black) < colorDistance);
                PlayerDataManager.Singleton.RespawnAllPlayers();
                yield return new WaitForSeconds(0.5f);
                playerMovementHandler.SetOrientation(botAttributes.transform.position + botAttributes.transform.forward * forwardSpawnPosMultiplier, playerMovementHandler.transform.rotation);
                playerUI.SetFadeToBlack(false);
            }
            else if (currentActionIndex == 7) // Player Card
            {
                botAttributes.ResetComboCounter();

                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Aim")) { continue; }
                    playerInput.actions.FindAction(action.name).Disable();
                }

                currentOverlayMessage = "Player Card.";
                playerUI.GetMainPlayerCard().gameObject.SetActive(true);
                yield return new WaitForSeconds(2);
                FasterPlayerPrefs.Singleton.SetBool("DisableBots", false);
                playerUI.GetMainPlayerCard().transform.localScale = new Vector3(3, 3, 3);

                yield return new WaitUntil(() => !IsTaskComplete() & !ShouldCheckmarkBeDisplayed() & IsInBufferTime());
                playerUI.GetMainPlayerCard().transform.localScale = Vector3.one;
                bufferDurationBetweenActions = 3;
                playerUI.SetFadeToBlack(true);
                yield return new WaitUntil(() => Vector4.Distance(playerUI.GetFadeToBlackColor(), Color.black) < colorDistance);
                PlayerDataManager.Singleton.RespawnAllPlayers();
                yield return new WaitForSeconds(0.5f);
                playerMovementHandler.SetOrientation(botAttributes.transform.position + Vector3.right * 6, playerMovementHandler.transform.rotation);
                playerUI.SetFadeToBlack(false);
            }
            else if (currentActionIndex == 8) // Swap Weapons
            {
                onTaskCompleteBufferDuration = 2;
                checkmarkDuration = 1;
                bufferDurationBetweenActions = 2;

                currentOverlayMessage = "Switch Weapons";

                List<Sprite> controlSchemeSpriteList = PlayerDataManager.Singleton.GetControlsImageMapping().GetControlSchemeActionImages(controlScheme, playerInput.actions["Weapon1"]);
                if (controlSchemeSpriteList.Count > 0)
                {
                    currentOverlaySprites = controlSchemeSpriteList;
                }
                else if (controlScheme.name != "Touchscreen")
                {
                    var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Weapon1"], playerInput.actions["Weapon2"] });
                    currentOverlaySprites = result.releasedSprites;
                }

                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Weapon1") | action.name.Contains("Weapon2") | action.name.Contains("SwitchWeapon")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                FasterPlayerPrefs.Singleton.SetBool("DisableBots", true);

                foreach (RuntimeWeaponCard weaponCard in playerUI.GetWeaponCards())
                {
                    weaponCard.SetActive(true);
                }

                playerUI.GetSwitchWeaponButton().gameObject.SetActive(true);

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetSwitchWeaponButton().transform, true));
            }
            else if (currentActionIndex == 9) // Prepare to fight with NPC
            {
                onTaskCompleteBufferDuration = 5;
                checkmarkDuration = 0;
                bufferDurationBetweenActions = 0;

                timerEnabled = true;
                currentOverlayMessage = "Prepare To Fight!";

                FasterPlayerPrefs.Singleton.SetBool("DisableBots", true);

                foreach (RuntimeWeaponCard weaponCard in playerUI.GetWeaponCards())
                {
                    weaponCard.SetActive(true);
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.SetActive(true);
                }

                playerUI.GetHeavyAttackButton().gameObject.SetActive(true);
                playerUI.RefreshOnScreenReloadButtonInteractability();
                playerUI.GetSwitchWeaponButton().gameObject.SetActive(true);

                playerUI.GetPauseMenuButton().gameObject.SetActive(true);
                playerUI.GetScoreboardButton().gameObject.SetActive(true);
                playerUI.GetLoadoutMenuButton().gameObject.SetActive(true);

                playerUI.GetHealthPotionCard().gameObject.SetActive(true);
                playerUI.GetStaminaPotionCard().gameObject.SetActive(true);
            }
            else if (currentActionIndex == 10) // Fight with NPC
            {
                onTaskCompleteBufferDuration = 1;
                checkmarkDuration = 1;
                bufferDurationBetweenActions = 0;

                FasterPlayerPrefs.Singleton.SetBool("DisableBots", false);
                FasterPlayerPrefs.Singleton.SetBool("BotsCanOnlyLightAttack", false);
                currentOverlayMessage = "Defeat The Enemy.";
                foreach (InputAction action in playerInput.actions)
                {
                    playerInput.actions.FindAction(action.name).Enable();
                }

                yield return new WaitForSeconds(2);

                currentOverlayMessage = "";

                yield return new WaitUntil(() => canProceed);

                currentOverlayMessage = "ENEMY KNOCKED OUT.";
            }
            else if (currentActionIndex == 11) // Display victory or defeat message
            {
                FasterPlayerPrefs.Singleton.SetBool("DisableBots", true);
                currentOverlayMessage = "ENEMY KNOCKED OUT.";

                bufferDurationBetweenActions = 3;
                playerUI.SetFadeToBlack(true);
                yield return new WaitUntil(() => Vector4.Distance(playerUI.GetFadeToBlackColor(), Color.black) < colorDistance);

                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Aim")) { continue; }
                    playerInput.actions.FindAction(action.name).Disable();
                }

                // Set messages active here
                while (mainGroup.alpha > colorDistance)
                {
                    mainGroup.alpha = Mathf.MoveTowards(mainGroup.alpha, 0, Time.deltaTime * endingMessageSpeed);
                    yield return null;
                }
                mainGroup.alpha = 0;

                foreach (Text endingMessage in endingMessages)
                {
                    endingMessage.color = new Color(endingMessage.color.r, endingMessage.color.g, endingMessage.color.b, 0);
                    endingMessage.gameObject.SetActive(true);

                    float elapsedTime = 0;
                    while (elapsedTime < endingMessageDisplayDuration)
                    {
                        yield return null;
                        elapsedTime += Time.deltaTime;
                        endingMessage.color = Vector4.MoveTowards(endingMessage.color, new Color(endingMessage.color.r, endingMessage.color.g, endingMessage.color.b, 1), Time.deltaTime * endingMessageSpeed);
                    }

                    elapsedTime = 0;
                    while (elapsedTime < endingMessageDisplayDuration)
                    {
                        yield return null;
                        elapsedTime += Time.deltaTime;
                        endingMessage.color = Vector4.MoveTowards(endingMessage.color, new Color(endingMessage.color.r, endingMessage.color.g, endingMessage.color.b, 0), Time.deltaTime * endingMessageSpeed);
                    }
                    endingMessage.color = new Color(endingMessage.color.r, endingMessage.color.g, endingMessage.color.b, 0);
                }

                FasterPlayerPrefs.Singleton.SetBool("TutorialCompleted", true);

                yield return new WaitForSeconds(2);

                // Return to char select
                PersistentLocalObjects.Singleton.StartCoroutine(ReturnToCharSelect());
            }
            else
            {
                Debug.LogError("Unsure how to handle current action index of " + currentActionIndex);
            }
        }

        private IEnumerator ReturnToCharSelect()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                PlayerDataManager.Singleton.WasDisconnectedByClient = true;
                NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
                yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }
            NetSceneManager.Singleton.LoadScene("Character Select");
        }

        private const float endingMessageSpeed = 1;
        private const float endingMessageDisplayDuration = 2;

        [SerializeField] private UIElementHighlight UIElementHighlightPrefab;
        private List<GameObject> UIElementHighlightInstances = new List<GameObject>();
        [SerializeField] private GameObject locationPingPrefab;
        private GameObject locationPingInstance;

        private void OnDestroy()
        {
            FasterPlayerPrefs.Singleton.SetBool("DisableBots", false);
            FasterPlayerPrefs.Singleton.SetBool("TutorialInProgress", false);
            FasterPlayerPrefs.Singleton.SetBool("BotsCanOnlyLightAttack", false);
            Time.timeScale = 1;
        }

        private string currentOverlayMessage;
        private List<Sprite> currentOverlaySprites = new List<Sprite>();

        private bool shouldLockCameraOnBot;
        private bool shouldAnimatePosition;
        private bool timerEnabled;

        private bool lastCanProceed;
        private bool lastIsInBufferTime;

        private Attributes botAttributes;

        private void FindBotAttributes()
        {
            if (botAttributes) { return; }
            if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => item.id < 0))
            {
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Find(item => item.id < 0);
                botAttributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerData.id);
            }
        }

        private void LateUpdate()
        {
            tutorialCanvas.enabled = currentActionIndex > -1;

            FindPlayerInput();
            FindBotAttributes();

            if (IsTaskComplete())
            {
                if (currentActionIndex == 11) { Time.timeScale = 0.5f; }

                overlayText.text = currentOverlayMessage;
                objectiveCompleteImage.color = new Color(1, 1, 1, 0);

                for (int i = 0; i < overlayImages.Length; i++)
                {
                    overlayImages[i].sprite = i < currentOverlaySprites.Count ? currentOverlaySprites[i] : null;
                }
            }
            else if (ShouldCheckmarkBeDisplayed())
            {
                if (currentActionIndex == 11) { Time.timeScale = 0.5f; }

                overlayText.text = currentOverlayMessage;
                objectiveCompleteImage.color = new Color(1, 1, 1, 1);

                for (int i = 0; i < overlayImages.Length; i++)
                {
                    overlayImages[i].sprite = i < currentOverlaySprites.Count ? currentOverlaySprites[i] : null;
                }

                FasterPlayerPrefs.Singleton.SetBool("DisableBots", true);

                foreach (GameObject instance in UIElementHighlightInstances)
                {
                    Destroy(instance);
                }
            }
            else if (IsInBufferTime())
            {
                overlayText.text = "";
                objectiveCompleteImage.color = new Color(1, 1, 1, 0);

                for (int i = 0; i < overlayImages.Length; i++)
                {
                    overlayImages[i].sprite = null;
                }
            }
            else if (lastIsInBufferTime & canProceed)
            {
                StartCoroutine(DisplayNextAction());
            }
            else
            {
                CheckTutorialActionStatus();
                overlayText.text = currentOverlayMessage;

                for (int i = 0; i < overlayImages.Length; i++)
                {
                    overlayImages[i].sprite = i < currentOverlaySprites.Count ? currentOverlaySprites[i] : null;
                }
            }

            backgroundImage.enabled = !string.IsNullOrWhiteSpace(overlayText.text) | objectiveCompleteImage.color.a > 0;

            if (playerUI)
            {
                if (currentActionIndex < 10) // Prepare to fight with NPC
                {
                    playerUI.GetScoreboardButton().gameObject.SetActive(false);
                }
            }

            foreach (Image image in overlayImages)
            {
                image.gameObject.SetActive(image.sprite);
            }

            float timerTextNum = onTaskCompleteBufferDuration - (Time.time - onTaskCompleteStartTime);
            timerText.text = timerEnabled & timerTextNum >= 0 ? timerTextNum.ToString("F0") : "";

            if (canProceed & !lastCanProceed)
            {
                Time.timeScale = 1;
                onTaskCompleteStartTime = Time.time;
                checkmarkStartTime = Time.time + onTaskCompleteBufferDuration;
                bufferStartTime = Time.time + onTaskCompleteBufferDuration + checkmarkDuration;
            }

            lastIsInBufferTime = IsInBufferTime();
            lastCanProceed = canProceed;

            if (shouldAnimatePosition)
            {
                if (Mathf.Abs(positionOffset) >= maxOffset) { directionMultiplier *= -1; }

                float amount = Time.deltaTime * animationSpeed * directionMultiplier;
                positionOffset = Mathf.Clamp(positionOffset + amount, -maxOffset, maxOffset);

                layoutGroupRT.anchoredPosition += new Vector2(amount, 0);
            }
            else
            {
                layoutGroupRT.anchoredPosition = originalAnchoredPosition;
            }

            if (shouldLockCameraOnBot)
            {
                if (botAttributes & playerMovementHandler)
                {
                    playerMovementHandler.LockOnTarget(botAttributes.transform);
                }
            }
            else if (playerMovementHandler)
            {
                playerMovementHandler.LockOnTarget(null);
            }
        }

        private bool IsTaskComplete() { return Time.time - onTaskCompleteStartTime <= onTaskCompleteBufferDuration; }
        private bool ShouldCheckmarkBeDisplayed() { return Time.time - checkmarkStartTime <= checkmarkDuration; }
        private bool IsInBufferTime() { return Time.time - bufferStartTime <= bufferDurationBetweenActions; }

        private float onTaskCompleteBufferDuration = 3;
        private float checkmarkDuration = 1;
        private float bufferDurationBetweenActions = 3;

        private float onTaskCompleteStartTime = Mathf.NegativeInfinity;
        private float checkmarkStartTime = Mathf.NegativeInfinity;
        private float bufferStartTime = Mathf.NegativeInfinity;

        private bool canProceed;
        private bool canProceedCondition1;
        private void CheckTutorialActionStatus()
        {
            if (currentActionIndex == -1)
            {
                return;
            }
            else if (currentActionIndex == 0) // Look
            {
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators().ToArray())
                {
                    if (playerData.id < 0) { PlayerDataManager.Singleton.KickPlayer(playerData.id); }
                }

                if (playerMovementHandler)
                {
                    canProceed = playerMovementHandler.GetLookInput() != Vector2.zero | canProceed;
                }
            }
            else if (currentActionIndex == 1) // Move
            {
                if (locationPingInstance)
                {
                    canProceed = Vector3.Distance(locationPingInstance.transform.position, playerInput.transform.position) < 1 | canProceed;
                }
                else
                {
                    if (botAttributes)
                    {
                        locationPingInstance = Instantiate(locationPingPrefab, botAttributes.transform.position + (botAttributes.transform.forward * forwardSpawnPosMultiplier) + (Vector3.up * 0.1f), botAttributes.transform.rotation, botAttributes.transform);
                    }
                }
            }
            else if (currentActionIndex == 2) // Attack
            {
                canProceed = attributes.GetComboCounter() > 0 | canProceed;
            }
            else if (currentActionIndex == 3) // Combo
            {
                if (botAttributes)
                {
                    canProceed = botAttributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Knockdown | attributes.GetComboCounter() >= 3 | canProceed;
                }
            }
            else if (currentActionIndex == 4) // Ability
            {
                canProceedCondition1 = weaponHandler.CurrentActionClip.name.Contains("Ability") | canProceedCondition1;
                canProceed = (canProceedCondition1 & !animationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip)) | canProceed;
            }
            else if (currentActionIndex == 5) // Ultimate Ability
            {
                canProceedCondition1 = weaponHandler.CurrentActionClip.name == "Ability4" | canProceedCondition1;
                canProceed = (canProceedCondition1 & !animationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip)) | canProceed;
            }
            else if (currentActionIndex == 6) // Dodge
            {
                if (botAttributes)
                {
                    WeaponHandler weaponHandler = botAttributes.WeaponHandler;
                    Time.timeScale = weaponHandler.IsInAnticipation | weaponHandler.IsAttacking ? 0.1f : 1;
                }
                canProceed = animationHandler.IsDodging() | canProceed;
            }
            else if (currentActionIndex == 7) // Player Card
            {
                if (botAttributes)
                {
                    canProceed = botAttributes.GetComboCounter() > 0 | canProceed;
                }
            }
            else if (currentActionIndex == 8) // Swap Weapons
            {
                canProceed = loadoutManager.GetEquippedSlotType() == LoadoutManager.WeaponSlotType.Secondary | canProceed;
            }
            else if (currentActionIndex == 9) // Prepare to fight with NPC
            {
                canProceed = true;
            }
            else if (currentActionIndex == 10) // Fight with NPC
            {
                bool botIsDead = false;
                if (botAttributes)
                {
                    botIsDead = botAttributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death;
                }
                canProceed = botIsDead | canProceed;
            }
            else if (currentActionIndex == 11) // Display victory or defeat message
            {

            }
            else
            {
                Debug.LogError("Unsure how to handle current action index of " + currentActionIndex);
            }
        }
    }
}