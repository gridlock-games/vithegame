using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.InputSystem;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using TMPro;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private PlayerCard playerCard;
        [Header("Ability Cards")]
        [SerializeField] private AbilityCard ability1;
        [SerializeField] private AbilityCard ability2;
        [SerializeField] private AbilityCard ability3;
        [SerializeField] private AbilityCard ability4;
        [Header("Status UI")]
        [SerializeField] private StatusImageReference statusImageReference;
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;
        [Header("Debug Elements")]
        [SerializeField] private TextMeshProUGUI fpsDisplay;
        [SerializeField] private TextMeshProUGUI pingDisplay;

        private WeaponHandler weaponHandler;
        private Attributes attributes;

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

            StartCoroutine(FPSCounter());
        }

        private void Update()
        {
            fpsDisplay.SetText("FPS: " + Mathf.RoundToInt(frameCount).ToString());

            //UpdateStatusUI();
        }

        void UpdateStatusUI()
        {
            foreach (Transform child in statusImageParent)
            {
                Destroy(child.gameObject);
            }

            foreach (Attributes.StatusPayload statusPayload in attributes.GetActiveStatuses())
            {
                GameObject statusImage = Instantiate(statusImagePrefab.gameObject, statusImageParent);
                statusImage.GetComponent<StatusIcon>().UpdateStatusIcon(statusImageReference.GetStatusIcon(statusPayload.status), statusPayload.duration, statusPayload.delay);
            }
        }

        private float frameCount;
        private IEnumerator FPSCounter()
        {
            GUI.depth = 2;
            while (true)
            {
                frameCount = 1f / Time.unscaledDeltaTime;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}