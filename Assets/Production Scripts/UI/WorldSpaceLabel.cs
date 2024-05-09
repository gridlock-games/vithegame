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
        [SerializeField] private float viewDistance = 20;
        [SerializeField] private float healthBarViewDistance = 5;
        [Header("Fixed Scale Settings")]
        [SerializeField] private bool shouldUseFixedScale;
        [SerializeField] private float fixedScaleValue = 0.01f;
        [Header("Relative Scaling Settings")]
        [SerializeField] private float scalingSpeed = 8;
        [SerializeField] private float scalingDistanceDivisor = 500;

        [Header("Object Assignments")]
        [SerializeField] private Text nameDisplay;
        [SerializeField] private RectTransform healthBarParent;
        [SerializeField] private Image nameBackground;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image interimHealthFillImage;

        [Header("Status UI")]
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;

        private Attributes attributes;
        private Renderer rendererToFollow;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();
        private CanvasGroup[] canvasGroups;

        private void Start()
        {
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PlayerPrefs.GetFloat("UIOpacity");
            }

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
            healthBarParent.localScale = Vector3.zero;

            PlayerDataManager.Singleton.SubscribeDataListCallback(delegate { UpdateNameTextAndColors(); });
            UpdateNameTextAndColors();
        }

        private void OnDestroy()
        {
            PlayerDataManager.Singleton.UnsubscribeDataListCallback(delegate { UpdateNameTextAndColors(); });
        }

        private void UpdateNameTextAndColors()
        {
            nameDisplay.text = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character.name.ToString();
            Color relativeTeamColor = attributes.GetRelativeTeamColor();
            nameBackground.color = relativeTeamColor;
            nameDisplay.color = relativeTeamColor == Color.black ? Color.white : Color.black;
            healthFillImage.color = relativeTeamColor == Color.black ? Color.red : relativeTeamColor;
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
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = PlayerPrefs.GetFloat("UIOpacity");
            }

            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }
            if (!rendererToFollow) { RefreshRendererToFollow(); }
            if (!rendererToFollow) { Debug.LogWarning("No renderer to follow"); return; }

            Vector3 localScaleTarget = Vector3.zero;
            Vector3 healthBarLocalScaleTarget = Vector3.zero;
            if (Camera.main)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - transform.position), Time.deltaTime * rotationSpeed);

                float camDistance = Vector3.Distance(Camera.main.transform.position, transform.position);
                if (camDistance > viewDistance)
                {
                    Quaternion mouseOverRotation = Quaternion.LookRotation(Camera.main.transform.position - rendererToFollow.bounds.center);
                    float upAngle = Vector3.SignedAngle(Camera.main.transform.up, mouseOverRotation * Vector3.up, Camera.main.transform.right);
                    float horizontalAngle = Quaternion.Angle(Camera.main.transform.rotation, mouseOverRotation);

                    if (shouldUseFixedScale)
                        localScaleTarget = horizontalAngle > 178 & upAngle > -4 & upAngle <= 0 ? new Vector3(fixedScaleValue, fixedScaleValue, fixedScaleValue) : Vector3.zero;
                    else
                        localScaleTarget = horizontalAngle > 178 & upAngle > -4 & upAngle <= 0 ? Vector3.one * (camDistance / scalingDistanceDivisor) : Vector3.zero;
                }
                else // Cam distance is less than view distance
                {
                    if (shouldUseFixedScale)
                        localScaleTarget = new Vector3(fixedScaleValue, fixedScaleValue, fixedScaleValue);
                    else
                        localScaleTarget = Vector3.one * (camDistance / scalingDistanceDivisor);

                    if (camDistance < healthBarViewDistance) { healthBarLocalScaleTarget = Vector3.one; }
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

            if (healthBarLocalScaleTarget == Vector3.zero)
            {
                KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                if (localPlayerKvp.Value)
                {
                    if (localPlayerKvp.Value.GetComponent<WeaponHandler>().CanAim) { healthBarLocalScaleTarget = Vector3.one; }
                }
            }
            healthBarParent.localScale = Vector3.Lerp(healthBarParent.localScale, healthBarLocalScaleTarget, Time.deltaTime * scalingSpeed);

            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, attributes.GetHP() / attributes.GetMaxHP(), Time.deltaTime * PlayerCard.fillSpeed);

            foreach (StatusIcon statusIcon in statusIcons)
            {
                statusIcon.gameObject.SetActive(attributes.GetActiveStatuses().Contains(statusIcon.Status));
            }
        }
    }
}