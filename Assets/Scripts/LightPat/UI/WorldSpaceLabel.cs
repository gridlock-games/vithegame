using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GameCreator.Melee;
using UnityEngine.UI;
using Unity.Netcode;
using LightPat.Core;

namespace LightPat.UI
{
    public class WorldSpaceLabel : MonoBehaviour
    {
        public TextMeshPro nameDisplay;

        public float rotationSpeed = 0.6f;
        public float animationSpeed = 0.1f;
        public float viewDistance = 5f;

        public Slider healthSlider;

        CharacterMelee melee;
        Transform target;
        Vector3 positionOffset;
        Vector3 originalScale;

        private void OnEnable()
        {
            if (!transform.parent) return;

            target = transform.parent;
            nameDisplay.SetText(target.name);
            positionOffset = transform.localPosition;
            transform.SetParent(null, true);

            melee = target.GetComponent<CharacterMelee>();
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
                }
            }

            name = "World Space Label for " + target.name;

            originalScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(target.gameObject.activeInHierarchy);

            transform.position = target.position + positionOffset;

            if (!Camera.main) { return; }
            Quaternion rotTarget = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - transform.position), rotationSpeed);
            transform.rotation = rotTarget;

            if (Vector3.Distance(Camera.main.transform.position, transform.position) > viewDistance)
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * animationSpeed);
            else
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * animationSpeed);

            if (melee)
            {
                healthSlider.transform.rotation = rotTarget;
                healthSlider.value = melee.GetHP() / (float)melee.maxHealth;
            }
        }
    }
}