using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class WorldSpaceLabel : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float rotationSpeed = 15;
        [SerializeField] private float scalingSpeed = 8;
        [SerializeField] private float scalingDistanceDivisor = 500;
        [SerializeField] private float viewDistance = 20;

        [Header("Object Assignments")]
        [SerializeField] private Text nameDisplay;
        [SerializeField] private Image nameBackground;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image interimHealthFillImage;

        [Header("Status UI")]
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;

        private Attributes attributes;
        private Renderer rendererToFollow;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        private void Start()
        {
            attributes = GetComponentInParent<Attributes>();

            transform.SetParent(null, true);

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

            transform.localScale = Vector3.zero;
        }

        private void RefreshRendererToFollow()
        {
            Renderer[] renderers = attributes.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { return; }
            Vector3 highestPoint = renderers[0].bounds.center;
            rendererToFollow = renderers[0];
            foreach (Renderer renderer in renderers)
            {
                if (renderer.GetType() != typeof(SkinnedMeshRenderer)) { continue; }
                if (renderer.bounds.center.y > highestPoint.y)
                {
                    rendererToFollow = renderer;
                    highestPoint = renderer.bounds.center;
                }
            }
        }

        private void LateUpdate()
        {
            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }
            if (!rendererToFollow) { RefreshRendererToFollow(); }
            if (!rendererToFollow) { Debug.LogWarning("No renderer to follow"); return; }

            //nameDisplay.text = "Ailment: " + attributes.GetAilment().ToString();
            nameDisplay.text = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character.name.ToString();
            //nameDisplay.text = GameLogicManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).team + " " + attributes.GetTeam();
            Color relativeTeamColor = attributes.GetRelativeTeamColor();
            nameBackground.color = relativeTeamColor;
            nameDisplay.color = relativeTeamColor == Color.black ? Color.white : Color.black;
            healthFillImage.color = relativeTeamColor == Color.black ? Color.red : relativeTeamColor;

            Vector3 localScaleTarget = Vector3.zero;
            if (Camera.main)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - transform.position), Time.deltaTime * rotationSpeed);

                float camDistance = Vector3.Distance(Camera.main.transform.position, transform.position);
                if (camDistance > viewDistance)
                {
                    Quaternion mouseOverRotation = Quaternion.LookRotation(Camera.main.transform.position - rendererToFollow.bounds.center);
                    float upAngle = Vector3.SignedAngle(Camera.main.transform.up, mouseOverRotation * Vector3.up, Camera.main.transform.right);
                    float horizontalAngle = Quaternion.Angle(Camera.main.transform.rotation, mouseOverRotation);
                    localScaleTarget = horizontalAngle > 178 & (upAngle > -4 & upAngle <= 0) ? Vector3.one * (camDistance / scalingDistanceDivisor) : Vector3.zero;
                }
                else
                {
                    localScaleTarget = Vector3.one * (camDistance / scalingDistanceDivisor);
                }

                Vector3 pos = rendererToFollow.bounds.center;
                pos.y += rendererToFollow.bounds.extents.y;
                //pos.y += rendererToFollow.bounds.extents.y * transform.localScale.x * scalingDistanceDivisor * 2;
                transform.position = pos;
            }
            else
            {
                //Debug.LogWarning("Can't find a main camera for world space labels!");
            }
            transform.localScale = Vector3.Lerp(transform.localScale, localScaleTarget, Time.deltaTime * scalingSpeed);

            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, attributes.GetHP() / attributes.GetMaxHP(), Time.deltaTime * PlayerCard.fillSpeed);

            foreach (StatusIcon statusIcon in statusIcons)
            {
                statusIcon.gameObject.SetActive(attributes.GetActiveStatuses().Contains(new ActionClip.StatusPayload(statusIcon.Status, 0, 0, 0)));
            }
        }
    }
}