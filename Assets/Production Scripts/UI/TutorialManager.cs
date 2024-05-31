using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Utility;
using UnityEngine.UI;

namespace Vi.UI
{
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] private Text overlayText;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Image objectiveCompleteImage;

        PlayerInput playerInput;
        MovementHandler movementHandler;
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
                movementHandler = localPlayer.GetComponent<MovementHandler>();
                attributes = localPlayer;
                weaponHandler = localPlayer.GetComponent<WeaponHandler>();
                animationHandler = localPlayer.GetComponent<AnimationHandler>();
                playerUI = localPlayer.GetComponentInChildren<PlayerUI>();
            }
        }

        RectTransform overlayImageRT;
        private Vector2 originalAnchoredPosition;
        private void Awake()
        {
            FindPlayerInput();
            FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
            originalAnchoredPosition = ((RectTransform)overlayImage.transform).anchoredPosition;
            overlayImageRT = (RectTransform)overlayImage.transform;
            DisplayNextAction();
        }

        private const float animationSpeed = 100;

        private float maxOffset = 100;

        private float positionOffset;
        private float directionMultiplier = -1;

        private int currentActionIndex = -1;
        private void DisplayNextAction()
        {
            currentActionIndex += 1;
            overlayImageRT.anchoredPosition = originalAnchoredPosition;

            canProceed = false;
            actionChangeTime = Time.time;

            if (locationPingInstance) { Destroy(locationPingInstance); }

            foreach (GameObject instance in UIElementHighlightInstances)
            {
                Destroy(instance);
            }

            StartCoroutine(EvaluateAfterPlayerInputFound());
        }

        [SerializeField] private UIElementHighlight UIElementHighlightPrefab;
        private List<GameObject> UIElementHighlightInstances = new List<GameObject>();
        [SerializeField] private GameObject locationPingPrefab;
        private GameObject locationPingInstance;

        private IEnumerator EvaluateAfterPlayerInputFound()
        {
            if (!playerInput)
            {
                yield return new WaitUntil(() => playerInput);
                yield return null;
            }

            InputControlScheme controlScheme = playerInput.actions.FindControlScheme(playerInput.currentControlScheme).Value;
            if (currentActionIndex == 0) // Look
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, playerInput.actions["Look"]);
                currentOverlaySprite = result.sprite;

                currentOverlayMessage = "Look Around.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name == "Inventory" | action.name == "Pause" | action.name == "Scoreboard") { continue; }
                    if (!action.name.Contains("Look")) { playerInput.actions.FindAction(action.name).Disable(); }
                }
            }
            else if (currentActionIndex == 1) // Move
            {
                currentOverlayMessage = "Move To The Marked Location.";
                PlayerDataManager.Singleton.AddBotData(PlayerDataManager.Team.Competitor);
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Move")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
            }
            else if (currentActionIndex == 2) // Attack
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, playerInput.actions["LightAttack"]);
                currentOverlaySprite = result.sprite;

                currentOverlayMessage = "Attack The Enemy.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Attack")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetLookJoystickCenter(), true));
            }
            else if (currentActionIndex == 3) // Combo
            {
                var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, playerInput.actions["LightAttack"]);
                currentOverlaySprite = result.sprite;

                currentOverlayMessage = "Perform A Combo On The Enemy.";
                attributes.ResetComboCounter();

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetLookJoystickCenter(), true));
                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetHeavyAttackButton().transform, true));
            }
            else if (currentActionIndex == 4) // Ability
            {
                currentOverlaySprite = null;

                currentOverlayMessage = "Use An Ability.";
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Ability")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                foreach (AbilityCard abilityCard in playerUI.GetAbilityCards())
                {
                    abilityCard.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, abilityCard.transform, true));
                }
            }
            else if (currentActionIndex == 5) // Block
            {
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
            else if (currentActionIndex == 6) // Dodge
            {
                currentOverlayMessage = "Dodge.";
                FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.name.Contains("Dodge")) { playerInput.actions.FindAction(action.name).Enable(); }
                }

                UIElementHighlightInstances.Add(Instantiate(UIElementHighlightPrefab.gameObject, playerUI.GetDodgeButton().transform, true));
            }
            else if (currentActionIndex == 7) // Player Card
            {
                if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => item.id < 0))
                {
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Find(item => item.id < 0);
                    Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerData.id);
                    if (attributes)
                    {
                        WeaponHandler weaponHandler = attributes.GetComponent<WeaponHandler>();
                        attributes.ResetComboCounter();
                    }
                }

                currentOverlayMessage = "Player Card.";
                FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
                playerUI.GetMainPlayerCard().transform.localScale = new Vector3(3, 3, 3);
            }
            else if (currentActionIndex == 8) // Prepare to fight with NPC
            {
                currentOverlayMessage = "Prepare To Fight!";
                foreach (InputAction action in playerInput.actions)
                {
                    playerInput.actions.FindAction(action.name).Enable();
                }

                playerUI.GetMainPlayerCard().transform.localScale = Vector3.one;

                FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
                PlayerDataManager.Singleton.RespawnAllPlayers();
            }
            else if (currentActionIndex == 9) // Fight with NPC
            {
                FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
                currentOverlayMessage = "Defeat The Enemy.";
            }
            else if (currentActionIndex == 10) // Display victory or defeat message
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

        private void OnDestroy()
        {
            FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
            FasterPlayerPrefs.Singleton.SetString("TutorialInProgress", false.ToString());
        }

        private string currentOverlayMessage;
        private Sprite currentOverlaySprite;
        private bool shouldAnimate;

        private bool lastCanProceed;

        private void Update()
        {
            overlayImage.enabled = overlayImage.sprite;

            FindPlayerInput();
            CheckTutorialActionStatus();

            objectiveCompleteImage.color = Color.Lerp(objectiveCompleteImage.color, canProceed ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0), Time.deltaTime * animationSpeed);

            if (!playerInput) { return; }
            if (string.IsNullOrWhiteSpace(playerInput.currentControlScheme)) { return; }

            overlayText.text = currentOverlayMessage;

            overlayImage.sprite = currentOverlaySprite;
            overlayImage.preserveAspect = true;

            if (shouldAnimate)
            {
                if (Mathf.Abs(positionOffset) >= maxOffset) { directionMultiplier *= -1; }

                float amount = Time.deltaTime * animationSpeed * directionMultiplier;
                positionOffset = Mathf.Clamp(positionOffset + amount, -maxOffset, maxOffset);

                overlayImageRT.anchoredPosition += new Vector2(amount, 0);
            }
            else
            {
                overlayImageRT.anchoredPosition = originalAnchoredPosition;
            }

            if (canProceed & !lastCanProceed) { actionChangeTime = Time.time; }
            lastCanProceed = canProceed;
        }

        private const float minDisplayTime = 3;

        private bool canProceed;
        private float actionChangeTime;
        private void CheckTutorialActionStatus()
        {
            if (currentActionIndex == 0) // Look
            {
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators().ToArray())
                {
                    if (playerData.id < 0) { PlayerDataManager.Singleton.KickPlayer(playerData.id); }
                }

                if (movementHandler)
                {
                    canProceed = movementHandler.GetLookInput() != Vector2.zero | canProceed;
                    if (canProceed)
                    {
                        if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                    }
                }
            }
            else if (currentActionIndex == 1) // Move
            {
                if (locationPingInstance)
                {
                    if (Vector3.Distance(locationPingInstance.transform.position, playerInput.transform.position) < 1.7f) { DisplayNextAction(); }
                }
                else
                {
                    if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => item.id < 0))
                    {
                        PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Find(item => item.id < 0);
                        Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerData.id);
                        if (attributes)
                        {
                            locationPingInstance = Instantiate(locationPingPrefab, attributes.transform.position + attributes.transform.forward, attributes.transform.rotation);
                        }
                    }
                }
            }
            else if (currentActionIndex == 2) // Attack
            {
                canProceed = attributes.GetComboCounter() > 0 | canProceed;
                if (canProceed)
                {
                    if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                }
            }
            else if (currentActionIndex == 3) // Combo
            {
                canProceed = attributes.GetComboCounter() > 1 | canProceed;
                if (canProceed)
                {
                    if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                }
            }
            else if (currentActionIndex == 4) // Ability
            {
                canProceed = weaponHandler.CurrentActionClip.name.Contains("Ability") | canProceed;
                if (canProceed)
                {
                    if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                }
            }
            else if (currentActionIndex == 5) // Block
            {
                if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => item.id < 0))
                {
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Find(item => item.id < 0);
                    Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerData.id);
                    if (attributes)
                    {
                        WeaponHandler weaponHandler = attributes.GetComponent<WeaponHandler>();
                        Time.timeScale = weaponHandler.IsInAnticipation | weaponHandler.IsAttacking ? 0.5f : 1;
                    }
                }
                
                if (attributes.GlowRenderer.IsRenderingBlock()) { Time.timeScale = 1; DisplayNextAction(); }
            }
            else if (currentActionIndex == 6) // Dodge
            {
                canProceed = animationHandler.IsDodging() | canProceed;
                if (canProceed)
                {
                    if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                }
            }
            else if (currentActionIndex == 7) // Player Card
            {
                if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => item.id < 0))
                {
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Find(item => item.id < 0);
                    Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerData.id);
                    if (attributes)
                    {
                        WeaponHandler weaponHandler = attributes.GetComponent<WeaponHandler>();
                        canProceed = attributes.GetComboCounter() > 0 | canProceed;
                    }
                }

                if (canProceed)
                {
                    if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                }
            }
            else if (currentActionIndex == 8) // Prepare to fight with NPC
            {
                canProceed = true;
                if (canProceed)
                {
                    if (Time.time - actionChangeTime > minDisplayTime) { DisplayNextAction(); }
                }
            }
            else if (currentActionIndex == 9) // Fight with NPC
            {
                bool botIsDead = false;
                if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => item.id < 0))
                {
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Find(item => item.id < 0);
                    Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerData.id);
                    if (attributes)
                    {
                        botIsDead = attributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death;
                        WeaponHandler weaponHandler = attributes.GetComponent<WeaponHandler>();
                    }
                }

                canProceed = attributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death | botIsDead | canProceed;
                if (canProceed) { DisplayNextAction(); }
            }
            else if (currentActionIndex == 10) // Display victory or defeat message
            {

            }
            else
            {
                Debug.LogError("Unsure how to handle current action index of " + currentActionIndex);
            }
        }
    }
}