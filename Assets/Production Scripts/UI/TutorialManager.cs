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

        PlayerInput playerInput;
        MovementHandler movementHandler;
        Attributes attributes;
        WeaponHandler weaponHandler;
        AnimationHandler animationHandler;
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

            StartCoroutine(EvaluateAfterPlayerInputFound());
        }

        private IEnumerator EvaluateAfterPlayerInputFound()
        {
            yield return new WaitUntil(() => playerInput);
            yield return null;

            if (currentActionIndex == 0) // Look
            {
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.actionMap.name != "Base") { continue; }
                    if (action.name == "Inventory" | action.name == "Pause" | action.name == "Scoreboard") { continue; }
                    if (!action.name.Contains("Look")) { playerInput.actions.FindAction(action.name).Disable(); }
                }
            }
            else if (currentActionIndex == 1) // Move
            {
                PlayerDataManager.Singleton.AddBotData(PlayerDataManager.Team.Competitor);
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.actionMap.name != "Base") { continue; }
                    if (action.name.Contains("Move")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
            }
            else if (currentActionIndex == 2) // Attack
            {
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.actionMap.name != "Base") { continue; }
                    if (action.name.Contains("Attack")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
            }
            else if (currentActionIndex == 3) // Combo
            {

            }
            else if (currentActionIndex == 4) // Ability
            {
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.actionMap.name != "Base") { continue; }
                    if (action.name.Contains("Ability")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
            }
            else if (currentActionIndex == 5) // Block
            {
                FasterPlayerPrefs.Singleton.SetString("DisableBots", false.ToString());
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.actionMap.name != "Base") { continue; }
                    if (action.name.Contains("Block")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
            }
            else if (currentActionIndex == 6) // Dodge
            {
                FasterPlayerPrefs.Singleton.SetString("DisableBots", true.ToString());
                foreach (InputAction action in playerInput.actions)
                {
                    if (action.actionMap.name != "Base") { continue; }
                    if (action.name.Contains("Dodge")) { playerInput.actions.FindAction(action.name).Enable(); }
                }
            }
            else if (currentActionIndex == 7) // Player Card
            {

            }
            else if (currentActionIndex == 8) // Fight with NPC
            {
                foreach (InputAction action in playerInput.actions)
                {
                    playerInput.actions.FindAction(action.name).Enable();
                }
            }
            else if (currentActionIndex == 9) // Display victory or defeat message
            {

            }
            else
            {
                Debug.LogError("Unsure how to handle current action index of " + currentActionIndex);
            }
        }

        private string currentOverlayMessage;
        private Sprite currentOverlaySprite;
        private bool shouldAnimate;

        private void Update()
        {
            FindPlayerInput();
            CheckTutorialActionStatus();

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
        }

        [SerializeField] private GameObject locationPingPrefab;
        private GameObject locationPingInstance;

        private const float minDisplayTime = 5;

        private bool canProceed;
        private float actionChangeTime;
        private void CheckTutorialActionStatus()
        {
            //InputControlScheme controlScheme = playerInput.actions.FindControlScheme(playerInput.currentControlScheme).Value;

            if (currentActionIndex == 0) // Look
            {
                currentOverlayMessage = "Look Around.";
                //var result = PlayerDataManager.Singleton.GetControlsImageMapping().GetActionSprite(controlScheme, playerInput.actions["Look"]);
                //currentOverlaySprite = result.sprite;

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
                currentOverlayMessage = "Move To The Marked Location.";

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
                currentOverlayMessage = "Attack The Enemy.";
                if (attributes.GetComboCounter() > 0) { DisplayNextAction(); }
            }
            else if (currentActionIndex == 3) // Combo
            {
                currentOverlayMessage = "Perform A Combo On The Enemy.";
                if (attributes.GetComboCounter() > 1) { DisplayNextAction(); }
            }
            else if (currentActionIndex == 4) // Ability
            {
                currentOverlayMessage = "Use An Ability.";
                if (weaponHandler.CurrentActionClip.name.Contains("Ability")) { DisplayNextAction(); }
            }
            else if (currentActionIndex == 5) // Block
            {
                currentOverlayMessage = "Block An Attack.";

                if (PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Exists(item => item.id < 0))
                {
                    PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Find(item => item.id < 0);
                    Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(playerData.id);
                    if (attributes)
                    {
                        WeaponHandler weaponHandler = attributes.GetComponent<WeaponHandler>();
                        Time.timeScale = weaponHandler.IsInAnticipation | weaponHandler.IsAttacking | weaponHandler.IsInRecovery ? 0.5f : 1;
                    }
                }
                
                if (attributes.GlowRenderer.IsRenderingBlock()) { Time.timeScale = 1; DisplayNextAction(); }
            }
            else if (currentActionIndex == 6) // Dodge
            {
                currentOverlayMessage = "Dodge.";
                if (animationHandler.IsDodging()) { DisplayNextAction(); }
            }
            else if (currentActionIndex == 7) // Player Card
            {
                currentOverlayMessage = "Player Card.";

            }
            else if (currentActionIndex == 8) // Fight with NPC
            {
                currentOverlayMessage = "Defeat The Enemy.";

            }
            else if (currentActionIndex == 9) // Display victory or defeat message
            {
                currentOverlayMessage = "MATCH COMPLETE.";

            }
            else
            {
                Debug.LogError("Unsure how to handle current action index of " + currentActionIndex);
            }
        }
    }
}