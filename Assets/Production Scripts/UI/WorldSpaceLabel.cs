using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using Vi.Utility;
using UnityEngine.SceneManagement;
using Vi.Core.CombatAgents;
using System.Linq;

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

        private PooledObject pooledObject;
        private Canvas canvas;
        private CombatAgent combatAgent;
        private Renderer rendererToFollow;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();
        private CanvasGroup[] canvasGroups;

        private void Awake()
        {
            pooledObject = GetComponent<PooledObject>();
            canvas = GetComponent<Canvas>();
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);

            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, statusImageParent).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
            }

            pooledObject.OnReturnToPool += OnReturnToPool;
        }

        private void OnEnable()
        {
            if (!combatAgent)
            {
                combatAgent = GetComponentInParent<CombatAgent>();

                transform.SetParent(null, true);

                transform.localScale = Vector3.zero;
                healthBarParent.localScale = Vector3.zero;

                rendererToFollow = null;

                SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName(ObjectPoolingManager.instantiationSceneName));
            }

            if (combatAgent)
            {
                RefreshRendererToFollow();

                UpdateNameTextAndColors();

                List<ActionClip.Status> activeStatuses = combatAgent.StatusAgent.GetActiveStatuses();
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
                transform.position = combatAgent.transform.position;
            }
            RefreshStatus();
        }

        private void OnReturnToPool()
        {
            team = default;
            localWeaponHandler = null;
            combatAgent = null;
            rendererToFollow = null;
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
            nameDisplay.text = PlayerDataManager.Singleton.GetTeamPrefix(combatAgent.GetTeam()) + combatAgent.GetName();
            nameDisplay.color = Color.white;
            team = combatAgent.GetTeam();

            if (PlayerDataManager.Singleton.GetGameModeInfo().possibleTeams.Contains(team))
            {
                healthFillImage.color = team == PlayerDataManager.Team.Competitor ? combatAgent.EnemyColor : combatAgent.GetRelativeTeamColor();
            }
            else
            {
                healthFillImage.color = PlayerDataManager.GetTeamColor(team);
            }
        }

        private void RefreshRendererToFollow()
        {
            if (!combatAgent) { return; }
            Renderer[] renderers = combatAgent.GetComponentsInChildren<Renderer>();
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
                    localWeaponHandler = kvp.Value.WeaponHandler;
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

            if (PlayerDataManager.Singleton.DataListWasUpdatedThisFrame
                | PlayerDataManager.Singleton.TeamNameOverridesUpdatedThisFrame
                | FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame)
            { UpdateNameTextAndColors(); }

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

            healthFillImage.fillAmount = combatAgent.GetHP() / combatAgent.GetMaxHP();
            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, combatAgent.GetHP() / combatAgent.GetMaxHP(), Time.deltaTime * PlayerCard.fillSpeed);
            
            if (combatAgent.StatusAgent.ActiveStatusesWasUpdatedThisFrame)
            {
                List<ActionClip.Status> activeStatuses = combatAgent.StatusAgent.GetActiveStatuses();
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