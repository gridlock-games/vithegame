using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using Vi.Utility;
using UnityEngine.SceneManagement;

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
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image interimHealthFillImage;

        [Header("Status UI")]
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;

        private Canvas canvas;
        private Attributes attributes;
        private Renderer rendererToFollow;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();
        private CanvasGroup[] canvasGroups;

        private void Awake()
        {
            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, statusImageParent).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
            }
        }

        private void OnEnable()
        {
            if (!attributes)
            {
                attributes = GetComponentInParent<Attributes>();

                transform.SetParent(null, true);

                transform.localScale = Vector3.zero;
                healthBarParent.localScale = Vector3.zero;

                rendererToFollow = null;

                SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName(ObjectPoolingManager.instantiationSceneName));
            }

            if (attributes)
            {
                RefreshRendererToFollow();

                UpdateNameTextAndColors();

                List<ActionClip.Status> activeStatuses = attributes.GetActiveStatuses();
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    if (activeStatuses.Contains(statusIcon.Status))
                    {
                        statusIcon.SetActive(true);
                        statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                    }
                    else
                    {
                        statusIcon.SetActive(false);
                    }
                }
            }
        }

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            }
        }

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera) { return; }
            mainCamera = Camera.main;
            canvas.worldCamera = mainCamera;
        }

        private PlayerDataManager.Team team;
        private void UpdateNameTextAndColors()
        {
            nameDisplay.text = PlayerDataManager.Singleton.GetTeamPrefix(attributes.CachedPlayerData.team) + attributes.CachedPlayerData.character.name.ToString();

            Color relativeTeamColor = attributes.GetRelativeTeamColor();
            nameDisplay.color = Color.white;
            team = attributes.GetTeam();
            healthFillImage.color = relativeTeamColor == Color.black | team == PlayerDataManager.Team.Competitor ? Color.red : relativeTeamColor;
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

        private WeaponHandler localWeaponHandler;
        private void FindLocalWeaponHandler()
        {
            if (localWeaponHandler) { return; }

            if (PlayerDataManager.Singleton.LocalPlayerData.team != PlayerDataManager.Team.Spectator)
            {
                KeyValuePair<int, Attributes> kvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                if (kvp.Value)
                {
                    localWeaponHandler = kvp.Value.GetComponent<WeaponHandler>();
                }
            }
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            FindLocalWeaponHandler();
        }

        private void LateUpdate()
        {
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            }

            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }

            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame | PlayerDataManager.Singleton.TeamNameOverridesUpdatedThisFrame) { UpdateNameTextAndColors(); }

            if (!rendererToFollow) { RefreshRendererToFollow(); }
            if (!rendererToFollow) { Debug.LogWarning("No renderer to follow"); return; }

            FindMainCamera();

            Vector3 localScaleTarget = Vector3.zero;
            Vector3 healthBarLocalScaleTarget = Vector3.zero;
            if (mainCamera)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mainCamera.transform.position - transform.position), Time.deltaTime * rotationSpeed);

                float camDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
                if (camDistance > viewDistance)
                {
                    Quaternion mouseOverRotation = Quaternion.LookRotation(mainCamera.transform.position - rendererToFollow.bounds.center);
                    float upAngle = Vector3.SignedAngle(mainCamera.transform.up, mouseOverRotation * Vector3.up, mainCamera.transform.right);
                    float horizontalAngle = Quaternion.Angle(mainCamera.transform.rotation, mouseOverRotation);

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
            canvas.enabled = transform.localScale.magnitude > 0.01f;

            if (healthBarLocalScaleTarget == Vector3.zero)
            {
                if (localWeaponHandler)
                {
                    if (localWeaponHandler.CanAim) { healthBarLocalScaleTarget = Vector3.one; }
                }
            }
            healthBarParent.localScale = Vector3.Lerp(healthBarParent.localScale, team == PlayerDataManager.Team.Peaceful ? Vector3.zero : healthBarLocalScaleTarget, Time.deltaTime * scalingSpeed);

            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, attributes.GetHP() / attributes.GetMaxHP(), Time.deltaTime * PlayerCard.fillSpeed);
            
            if (attributes.ActiveStatusesWasUpdatedThisFrame)
            {
                List<ActionClip.Status> activeStatuses = attributes.GetActiveStatuses();
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    if (activeStatuses.Contains(statusIcon.Status))
                    {
                        statusIcon.SetActive(true);
                        statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                    }
                    else
                    {
                        statusIcon.SetActive(false);
                    }
                }
            }
        }
    }
}