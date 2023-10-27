using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.InputSystem;
using Vi.ScriptableObjects;
using System.Linq;
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
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;
        [Header("Debug Elements")]
        [SerializeField] private TextMeshProUGUI fpsDisplay;
        [SerializeField] private TextMeshProUGUI pingDisplay;

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

            StartCoroutine(FPSCounter());
        }

        private void Update()
        {
            fpsDisplay.SetText("FPS: " + Mathf.RoundToInt(frameCount).ToString());

            foreach (StatusIcon statusIcon in statusIcons)
            {
                statusIcon.gameObject.SetActive(attributes.GetActiveStatuses().Contains(new Attributes.StatusPayload(statusIcon.Status, 0, 0, 0)));
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