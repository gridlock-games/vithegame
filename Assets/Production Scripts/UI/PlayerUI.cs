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
using UnityEngine.InputSystem.OnScreen;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private PlayerCard playerCard;
        [SerializeField] private PlayerCard[] teammatePlayerCards;
        [SerializeField] private Text ammoText;
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
        [SerializeField] private Image fadeToBlackImage;
        [SerializeField] private Image fadeToWhiteImage;
        [SerializeField] private GameObject deathUIParent;
        [SerializeField] private GameObject aliveUIParent;
        [Header("Mobile UI")]
        [SerializeField] private OnScreenButton lightAttackButton;
        [SerializeField] private OnScreenButton heavyAttackButton;
        [SerializeField] private Image lookJoystickImage;
        [SerializeField] private Image attackTypeToggleImage;
        [SerializeField] private Sprite lightAttackIcon;
        [SerializeField] private Sprite heavyAttackIcon;
        [SerializeField] private Sprite aimIcon;
        [SerializeField] private Image primaryWeaponButton;
        [SerializeField] private Image secondaryWeaponButton;
        [SerializeField] private Button switchAttackTypeButton;
        [SerializeField] private Image aimButton;

        [SerializeField] private PlatformUIDefinition[] platformUIDefinitions;

        [System.Serializable]
        public struct PlatformUIDefinition
        {
            public RuntimePlatform[] platforms;
            public GameObject[] gameObjectsToEnable;
            public GameObject[] gameObjectsToDestroy;
            public MoveUIDefinition[] objectsToMove;
        }

        [System.Serializable]
        public struct MoveUIDefinition
        {
            public GameObject gameObjectToMove;
            public Vector2 newAnchoredPosition;
        }

        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public void OpenPauseMenu()
        {
            attributes.GetComponent<ActionMapHandler>().OnPause();
        }

        public void OpenInventoryMenu()
        {
            attributes.GetComponent<ActionMapHandler>().OnInventory();
        }

        private Weapon.InputAttackType attackType = Weapon.InputAttackType.HeavyAttack;
        public void ToggleAttackType(bool isRefreshing)
        {
            if (isRefreshing) { attackType = Weapon.InputAttackType.HeavyAttack; }

            if (attackType == Weapon.InputAttackType.LightAttack)
            {
                attackType = Weapon.InputAttackType.HeavyAttack;
                attackTypeToggleImage.sprite = lightAttackIcon;
                lookJoystickImage.sprite = heavyAttackIcon;
                lightAttackButton.enabled = false;
                heavyAttackButton.enabled = true;
            }
            else if (attackType == Weapon.InputAttackType.HeavyAttack)
            {
                attackType = Weapon.InputAttackType.LightAttack;
                attackTypeToggleImage.sprite = heavyAttackIcon;
                lookJoystickImage.sprite = lightAttackIcon;
                lightAttackButton.enabled = true;
                heavyAttackButton.enabled = false;
            }
            else
            {
                Debug.LogError("Something's fucked up");
            }
        }

        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private LoadoutManager loadoutManager;
        private PlayerInput playerInput;

        private void Awake()
        {
            weaponHandler = GetComponentInParent<WeaponHandler>();
            loadoutManager = weaponHandler.GetComponent<LoadoutManager>();
            attributes = weaponHandler.GetComponent<Attributes>();
            playerInput = weaponHandler.GetComponent<PlayerInput>();
        }

        private void Start()
        {
            ToggleAttackType(false);
            fadeToWhiteImage.color = Color.black;
            foreach (PlatformUIDefinition platformUIDefinition in platformUIDefinitions)
            {
                foreach (GameObject g in platformUIDefinition.gameObjectsToEnable)
                {
                    g.SetActive(platformUIDefinition.platforms.Contains(Application.platform));
                }

                foreach (MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform))
                    {
                        moveUIDefinition.gameObjectToMove.GetComponent<RectTransform>().anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }

                foreach (GameObject g in platformUIDefinition.gameObjectsToDestroy)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform)) { Destroy(g); }
                }
            }

            playerCard.Initialize(GetComponentInParent<Attributes>());

            UpdateWeapon(false);

            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                GameObject statusIconGameObject = Instantiate(statusImagePrefab.gameObject, statusImageParent);
                if (statusIconGameObject.TryGetComponent(out StatusIcon statusIcon))
                {
                    statusIcon.InitializeStatusIcon(status);
                    statusIconGameObject.SetActive(false);
                    statusIcons.Add(statusIcon);
                }
            }
        }

        private Weapon lastWeapon;
        private void UpdateWeapon(bool forceRefresh)
        {
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
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
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
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
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
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
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
                    if (binding.path.ToLower().Contains(device.name.ToLower()))
                    {
                        ability4.UpdateCard(abilities[3], binding.ToDisplayString());
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak) { break; }
            }

            lastWeapon = weaponHandler.GetWeapon();

            ToggleAttackType(true);
            aimButton.gameObject.SetActive(weaponHandler.CanAim);
            switchAttackTypeButton.gameObject.SetActive(!weaponHandler.CanAim);

            primaryWeaponButton.sprite = loadoutManager.PrimaryWeaponOption.weaponIcon;
            secondaryWeaponButton.sprite = loadoutManager.SecondaryWeaponOption.weaponIcon;
        }

        private void UpdateActiveUIElements()
        {
            aliveUIParent.SetActive(attributes.GetAilment() != ActionClip.Ailment.Death);
            deathUIParent.SetActive(attributes.GetAilment() == ActionClip.Ailment.Death);
        }

        private string lastControlScheme;
        private void Update()
        {
            foreach (PlatformUIDefinition platformUIDefinition in platformUIDefinitions)
            {
                foreach (MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform))
                    {
                        moveUIDefinition.gameObjectToMove.GetComponent<RectTransform>().anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }
            }

            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }

            if (attributes.GetAilment() != ActionClip.Ailment.Death)
            {
                //rightMouseClickImage.sprite = weaponHandler.CanAim ? aimIcon : heavyAttackIcon;

                foreach (StatusIcon statusIcon in statusIcons)
                {
                    statusIcon.gameObject.SetActive(attributes.GetActiveStatuses().Contains(new ActionClip.StatusPayload(statusIcon.Status, 0, 0, 0)));
                }

                // Order player cards by distance
                List<Attributes> teammateAttributes = PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(attributes.GetTeam(), attributes).OrderBy(x => Vector3.Distance(attributes.transform.position, x.transform.position)).Take(teammatePlayerCards.Length).ToList();
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

                ammoText.transform.parent.gameObject.SetActive(weaponHandler.ShouldUseAmmo());
                ammoText.text = "Ammo: " + weaponHandler.GetAmmoCount().ToString() + " / " + weaponHandler.GetMaxAmmoCount().ToString();

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
                }
            }
            UpdateActiveUIElements();
            UpdateWeapon(playerInput.currentControlScheme != lastControlScheme);

            lastControlScheme = playerInput.currentControlScheme;
        }
    }
}