using UnityEngine;
using TMPro;
using GameCreator.Melee;
using UnityEngine.UI;
using Unity.Netcode;
using LightPat.Core;
using System.Linq;

namespace LightPat.UI
{
    public class WorldSpaceLabel : MonoBehaviour
    {
        public TextMeshProUGUI nameDisplay;
        public TextMeshProUGUI nameBackground;
        public GameObject spectatorHotKeyInstance;

        public float rotationSpeed = 0.6f;
        public float animationSpeed = 0.1f;
        public float viewDistance = 5f;
        public float scale = 1;

        public Slider healthSlider;
        public Image healthImage;

        CharacterMelee melee;
        Player.TeamIndicator teamIndicator;
        Transform target;
        Vector3 positionOffset;

        private void OnEnable()
        {
            if (!transform.parent) return;

            target = transform.parent;
            nameDisplay.SetText(target.name);
            positionOffset = transform.localPosition;
            transform.SetParent(null, true);

            melee = target.GetComponent<CharacterMelee>();
            teamIndicator = target.GetComponent<Player.TeamIndicator>();
            healthSlider.gameObject.SetActive(melee != null);

            if (target.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.IsPlayerObject)
                {
                    if (ClientManager.Singleton)
                    {
                        string clientName = ClientManager.Singleton.GetClient(netObj.OwnerClientId).clientName;
                        nameDisplay.SetText(clientName);
                        target.name = clientName;
                    }
                    else
                    {
                        nameDisplay.SetText(netObj.OwnerClientId + " - " + target.name);
                    }
                }
                else
                {
                    nameDisplay.SetText(netObj.OwnerClientId + " - " + target.name);
                }
            }

            name = "World Space Label for " + target.name;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            if (teamIndicator)
            {
                if (teamIndicator.teamColor == Color.black)
                {
                    nameDisplay.color = Color.white;
                }
                else
                {
                    nameDisplay.color = Color.black;
                }
            }

            // Set world space label text to client name
            if (target.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.IsPlayerObject)
                {
                    if (ClientManager.Singleton)
                    {
                        string clientName = ClientManager.Singleton.GetClient(netObj.OwnerClientId).clientName;
                        nameDisplay.SetText(clientName);
                        nameBackground.SetText("<mark=#" + ColorUtility.ToHtmlStringRGBA(teamIndicator.teamColor) + "> " + clientName + " </mark>");
                        target.name = clientName;
                    }
                }
            }

            gameObject.SetActive(target.gameObject.activeInHierarchy);

            transform.position = target.position + positionOffset;

            Quaternion rotTarget = Quaternion.identity;
            if (Camera.main)
            {
                // If we have a spectator camera as the main camera, disable the health bar portion of the world space label
                if (Camera.main.GetComponent<SpectatorCamera>())
                {
                    GameObject[] localNetworkPlayers = ClientManager.Singleton.localNetworkPlayers.Values.ToArray();
                    for (int i = 0; i < localNetworkPlayers.Length; i++)
                    {
                        if (localNetworkPlayers[i] == melee.gameObject)
                        {
                            spectatorHotKeyInstance.GetComponentInChildren<Text>().text = (i + 1).ToString();
                            break;
                        }
                    }

                    spectatorHotKeyInstance.SetActive(true);
                    healthSlider.gameObject.SetActive(false);
                }
                else
                {
                    spectatorHotKeyInstance.SetActive(false);
                    healthSlider.gameObject.SetActive(true);
                }

                rotTarget = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - transform.position), rotationSpeed);
                transform.rotation = rotTarget;

                if (Vector3.Distance(Camera.main.transform.position, transform.position) > viewDistance)
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * animationSpeed);
                else
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * animationSpeed);
            }
            
            if (melee)
            {
                if (Camera.main)
                    healthSlider.transform.rotation = rotTarget;
                healthSlider.value = melee.GetHP() / (float)melee.maxHealth;
                healthImage.color = teamIndicator ? teamIndicator.teamsAreActive ? teamIndicator.teamColor : Color.red : Color.red;
            }
        }
    }
}