using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.InputSystem;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using System.Linq;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private PlayerCard playerCard;
        [SerializeField] private PlayerCard[] teammatePlayerCards;
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
        [SerializeField] private Image fadeToBlackImage;
        [SerializeField] private Image fadeToWhiteImage;
        [SerializeField] private GameObject deathUIParent;
        [SerializeField] private GameObject aliveUIParent;

        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        private void Start()
        {
            playerCard.Initialize(GetComponentInParent<Attributes>());
            weaponHandler = GetComponentInParent<WeaponHandler>();
            attributes = GetComponentInParent<Attributes>();

            List<ActionClip> abilities = weaponHandler.GetWeapon().GetAbilities();
            foreach (InputBinding inputBinding in controlsAsset.bindings)
            {
                if (inputBinding.action == "Ability1")
                {
                    ability1.UpdateCard(abilities[0], inputBinding.ToDisplayString());
                }
                else if (inputBinding.action == "Ability2")
                {
                    ability2.UpdateCard(abilities[1], inputBinding.ToDisplayString());
                }
                else if (inputBinding.action == "Ability3")
                {
                    ability3.UpdateCard(abilities[2], inputBinding.ToDisplayString());
                }
                else if (inputBinding.action == "Ability4")
                {
                    ability4.UpdateCard(abilities[3], inputBinding.ToDisplayString());
                }
            }

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

        private void UpdateActiveUIElements()
        {
            aliveUIParent.SetActive(attributes.GetAilment() != ActionClip.Ailment.Death);
            deathUIParent.SetActive(attributes.GetAilment() == ActionClip.Ailment.Death);
        }

        private void Update()
        {
            if (attributes.GetAilment() != ActionClip.Ailment.Death)
            {
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    statusIcon.gameObject.SetActive(attributes.GetActiveStatuses().Contains(new ActionClip.StatusPayload(statusIcon.Status, 0, 0, 0)));
                }

                // Order player cards by distance
                List<Attributes> teammateAttributes = PlayerDataManager.Singleton.GetPlayersOnTeam(attributes.GetTeam(), attributes).OrderBy(x => Vector3.Distance(attributes.transform.position, x.transform.position)).Take(teammatePlayerCards.Length).ToList();
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
                
                fadeToBlackImage.color = Color.clear;
                fadeToWhiteImage.color = Color.Lerp(fadeToWhiteImage.color, Color.clear, Time.deltaTime);
            }
            else
            {
                killerCard.Initialize(attributes.GetKiller());
                respawnTimerText.text = attributes.IsRespawning ? "Respawning in " + attributes.GetRespawnTime().ToString("F4") : "";

                fadeToBlackImage.color = Color.Lerp(Color.clear, Color.black, attributes.GetRespawnTimeAsPercentage());
                fadeToWhiteImage.color = fadeToBlackImage.color;
            }
            UpdateActiveUIElements();
        }
    }
}