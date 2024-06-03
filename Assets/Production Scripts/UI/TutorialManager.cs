using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Player;
using Vi.Utility;
using UnityEngine.UI;

namespace Vi.UI
{
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private Text overlayText;
        [SerializeField] private Text timerText;
        [SerializeField] private Image[] overlayImages;
        [SerializeField] private Image objectiveCompleteImage;
        [SerializeField] private HorizontalLayoutGroup imagesLayoutGroup;

        PlayerInput playerInput;
        PlayerMovementHandler playerMovementHandler;
        Attributes attributes;
        WeaponHandler weaponHandler;
        AnimationHandler animationHandler;
        PlayerUI playerUI;
        private void FindPlayerInput()
        {
            if (playerInput) { return; }
            if (!PlayerDataManager.DoesExist()) { return; }
            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer)
            {
                playerInput = localPlayer.GetComponent<PlayerInput>();
                playerMovementHandler = localPlayer.GetComponent<PlayerMovementHandler>();
                attributes = localPlayer;
                weaponHandler = localPlayer.GetComponent<WeaponHandler>();
                animationHandler = localPlayer.GetComponent<AnimationHandler>();
                playerUI = localPlayer.GetComponentInChildren<PlayerUI>();
            }
        }

        private RectTransform layoutGroupRT;
        private Vector2 originalAnchoredPosition;

        private void Awake()
        {
            FindPlayerInput();
            FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());

            foreach (Image image in overlayImages)
            {
                image.gameObject.SetActive(false);
                image.preserveAspect = true;
            }

            layoutGroupRT = (RectTransform)imagesLayoutGroup.transform;
            originalAnchoredPosition = layoutGroupRT.anchoredPosition;

            objectiveCompleteImage.color = new Color(1, 1, 1, 0);

            StartCoroutine(DisplayNextActionAfterPlayerInputFound());
        }

        private IEnumerator DisplayNextActionAfterPlayerInputFound()
        {
            yield return new WaitUntil(() => playerInput);
            yield return null;
            StartCoroutine(DisplayNextAction());
        }

        private const float animationSpeed = 100;

        private float maxOffset = 100;

        private float positionOffset;
        private float directionMultiplier = -1;

        private int currentActionIndex = -1;

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
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Look"] });
                currentOverlaySprites = result.sprites;

                currentOverlayMessage = "Look Around.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (!action.name.Contains("Look")) { playerInput.actions.FindAction(action.name).Disable(); }
                }
            }
            else if (currentActionIndex == 1) // Move
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Move"] });
                currentOverlaySprites = result.sprites;

                currentOverlayMessage = "Move To The Marked Location.";
                PlayerDataManager.Singleton.AddBotData(PlayerDataManager.Team.Competitor);
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Move")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
            }
            else if (currentActionIndex == 2) // Attack
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["LightAttack"] });
                currentOverlaySprites = result.sprites;

                currentOverlayMessage = "Attack The Enemy.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Attack")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetLookJoystickCenter(), true));
            }
            else if (currentActionIndex == 3) // Combo
            {
                onTaskCompleteBufferDuration = 2;

                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["LightAttack"] });
                currentOverlaySprites.AddRange(result.sprites);
                currentOverlaySprites.AddRange(result.sprites);
                currentOverlaySprites.AddRange(result.sprites);

                currentOverlayMessage = "Perform A Combo On The Enemy.";
                attributes.ResetComboCounter();

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetLookJoystickCenter(), true));
                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetHeavyAttackButton().transform, true));
            }
            else if (currentActionIndex == 4) // Ability 1, 2, or 3
            {
                shouldLockCameraOnBot = true;

                foreach (InputAction action in playerInput.actions)
                {
                    playerInput.actions.FindAction(action.name).Disable();
                }

                yield return new WaitForSeconds(2);

                currentOverlayMessage = "Use An Ability.";
                List<string> abilityNames = new List<string>() { "Ability1", "Ability2", "Ability3" };
                foreach (InputAction action in playerInput.actions)
                {
                    if (abilityNames.Contains(action.name)) { playerInput.actions.FindAction(action.name).Enable(); }
                    else { playerInput.actions.FindAction(action.name).Disable(); }
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    if (!abilityNames.Contains(abilityCard.Ability.name)) { continue; }
                    abilityCard.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, abilityCard.transform, true));
                }
            }
            else if (currentActionIndex == 5) // Ability 4
            {
                shouldLockCameraOnBot = true;

                currentOverlayMessage = "Use Your Ultimate Ability.";
                List<string> abilityNames = new List<string>() { "Ability4" };
                foreach (InputAction action in playerInput.actions)
                {
                    if (abilityNames.Contains(action.name)) { playerInput.actions.FindAction(action.name).Enable(); }
                    else { playerInput.actions.FindAction(action.name).Disable(); }
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    if (!abilityNames.Contains(abilityCard.Ability.name)) { continue; }
                    abilityCard.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, abilityCard.transform, true));
                }
            }
            else if (currentActionIndex == 6) // Block
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Block"] });
                currentOverlaySprites = result.sprites;
                
                currentOverlayMessage = "Block An Attack.";
                FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Block")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.transform.localScale = Vector3.one;
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetBlockingButton().transform, true));
            }
            else if (currentActionIndex == 7) // Dodge
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, new InputAction[] { playerInput.actions["Dodge"] });
                currentOverlaySprites = result.sprites;

                currentOverlayMessage = "Dodge.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Dodge")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetDodgeButton().transform, true));
            }
            else if (currentActionIndex == 8) // Player Card
            {
                botAttributes.ResetComboCounter();

                currentOverlayMessage = "Player Card.";
                FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
                playerUI.GetMainPlayerCard().transform.localScale = new Vector3(3, 3, 3);
            }
            else if (currentActionIndex == 9) // Prepare to fight with NPC
            {
                timerEnabled = true;
                currentOverlayMessage = "Prepare To Fight!";
                foreach (InputAction action in playerInput.actions)
                {
                    playerInput.actions.FindAction(action.name).Enable();
                }

                playerUI.GetMainPlayerCard().transform.localScale = Vector3.one;

                FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
                PlayerDataManager.Singleton.RespawnAllPlayers();
            }
            else if (currentActionIndex == 10) // Fight with NPC
            {
                FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
                currentOverlayMessage = "Defeat The Enemy.";
            }
            else if (currentActionIndex == 11) // Display victory or defeat message
            {
                FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
                FasterPlayerPrefs.Singleton.SetString("TutorialCompleted", true.ToString());
                currentOverlayMessage = "MATCH COMPLETE.";
            }
            else
            {
                Debug.LogError("Unsure how to handle current action index of " + currentActionIndex);
            }
        }

        [SerializeField] private UIElementHighlight UIElementHighlightPrefab;
        private List<GameObject> UIElementHighlightInstances = new List<GameObject>();
        [SerializeField] private GameObject locationPingPrefab;
        private GameObject locationPingInstance;

        private void OnDestroy()
        {
            FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
            FasterPlayerPrefs.Singleton.SetString("TutorialInProgress", false.ToString());
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

        private void Update()
        {
            tutorialCanvas.enabled = currentActionIndex > -1;

            FindPlayerInput();
            FindBotAttributes();

            if (IsTaskComplete())
            {
                overlayText.text = currentOverlayMessage;
                objectiveCompleteImage.color = Color.Lerp(objectiveCompleteImage.color, new Color(1, 1, 1, 0), Time.deltaTime * animationSpeed);

                for (int i = 0; i < overlayImages.Length; i++)
                {
                    overlayImages[i].sprite = i < currentOverlaySprites.Count ? currentOverlaySprites[i] : null;
                }
            }
            else if (ShouldCheckmarkBeDisplayed())
            {
                overlayText.text = currentOverlayMessage;
                objectiveCompleteImage.color = Color.Lerp(objectiveCompleteImage.color, new Color(1, 1, 1, 1), Time.deltaTime * animationSpeed);

                for (int i = 0; i < overlayImages.Length; i++)
                {
                    overlayImages[i].sprite = i < currentOverlaySprites.Count ? currentOverlaySprites[i] : null;
                }
            }
            else if (IsInBufferTime())
            {
                overlayText.text = "";
                objectiveCompleteImage.color = Color.Lerp(objectiveCompleteImage.color, new Color(1, 1, 1, 0), Time.deltaTime * animationSpeed);

                for (int i = 0; i < overlayImages.Length; i++)
                {
                    overlayImages[i].sprite = null;
                }
            }
            else if (lastIsInBufferTime)
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

            foreach (Image image in overlayImages)
            {
                image.gameObject.SetActive(image.sprite);
            }

            timerText.text = timerEnabled ? (bufferDurationBetweenActions - (Time.time - bufferStartTime)).ToString("F0") : "";

            if (canProceed & !lastCanProceed)
            {
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
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators().ToArray())
                {
                    if (playerData.id < 0) { PlayerDataManager.Singleton.KickPlayer(playerData.id); }
                }
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
                    canProceed = Vector3.Distance(locationPingInstance.transform.position, playerInput.transform.position) < 1.7f | canProceed;
                }
                else
                {
                    if (botAttributes)
                    {
                        locationPingInstance = Instantiate(locationPingPrefab, botAttributes.transform.position + botAttributes.transform.forward, botAttributes.transform.rotation, botAttributes.transform);
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
                    canProceed = botAttributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Knockdown | canProceed;
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
            else if (currentActionIndex == 6) // Block
            {
                if (botAttributes)
                {
                    WeaponHandler weaponHandler = attributes.GetComponent<WeaponHandler>();
                    Time.timeScale = weaponHandler.IsInAnticipation | weaponHandler.IsAttacking ? 0.5f : 1;
                }
                canProceed = attributes.GlowRenderer.IsRenderingBlock() | canProceed;
            }
            else if (currentActionIndex == 7) // Dodge
            {
                if (botAttributes)
                {
                    WeaponHandler weaponHandler = attributes.GetComponent<WeaponHandler>();
                    Time.timeScale = weaponHandler.IsInAnticipation | weaponHandler.IsAttacking ? 0.5f : 1;
                }
                canProceed = animationHandler.IsDodging() | canProceed;
            }
            else if (currentActionIndex == 8) // Player Card
            {
                if (botAttributes)
                {
                    canProceed = attributes.GetComboCounter() > 0 | canProceed;
                }
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
                    botIsDead = attributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death;
                }
                canProceed = attributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death | botIsDead | canProceed;
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