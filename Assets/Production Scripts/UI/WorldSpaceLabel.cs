using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using Vi.Utility;
using UnityEngine.SceneManagement;
using Vi.Core.CombatAgents;
using Unity.Netcode;

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
        [SerializeField] private Image background;
        [SerializeField] private RectTransform healthBarParent;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image interimHealthFillImage;
        [SerializeField] private Text healthText;

        [Header("Status UI")]
        [SerializeField] private StatusIconLayoutGroup statusIconLayoutGroup;

        private PooledObject pooledObject;
        private Canvas canvas;
        private CombatAgent combatAgent;
        private CanvasGroup[] canvasGroups;

        private void Awake()
        {
            pooledObject = GetComponent<PooledObject>();
            canvas = GetComponent<Canvas>();
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);

            pooledObject.OnSpawnFromPool += OnSpawnFromPool;
            pooledObject.OnReturnToPool += OnReturnToPool;
        }

        private void OnEnable()
        {
            transform.localScale = Vector3.zero;
            healthBarParent.localScale = Vector3.zero;
            healthText.enabled = false;

            if (!combatAgent)
            {
                combatAgent = GetComponentInParent<CombatAgent>();

                transform.SetParent(null, true);

                SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName(ObjectPoolingManager.instantiationSceneName));

                statusIconLayoutGroup.Initialize(null);
            }

            if (combatAgent)
            {
                if (combatAgent is Mob)
                {
                    background.enabled = false;
                    nameDisplay.enabled = false;
                }
                else
                {
                    background.enabled = true;
                    nameDisplay.enabled = true;
                }

                UpdateNameTextAndColors();

                statusIconLayoutGroup.Initialize(combatAgent.StatusAgent);

                transform.position = combatAgent.transform.position;
                transform.rotation = combatAgent.transform.rotation;
                transform.localScale = Vector3.zero;
                healthBarParent.localScale = Vector3.zero;
            }
            RefreshStatus();
        }

        private void OnSpawnFromPool()
        {
            if (combatAgent)
            {
                transform.position = combatAgent.transform.position;
                transform.rotation = combatAgent.transform.rotation;
            }
            else
            {
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
            }
            transform.localScale = Vector3.zero;
            healthBarParent.localScale = Vector3.zero;
        }

        private void OnReturnToPool()
        {
            team = default;
            localCombatAgent = null;
            localSpectator = null;
            combatAgent = null;

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.zero;
            healthBarParent.localScale = Vector3.zero;

            lastHP = -1;
            lastMaxHP = -1;
        }

        private void RefreshStatus()
        {
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = FasterPlayerPrefs.Singleton.GetFloat("UIOpacity");
            }
            healthText.gameObject.SetActive(FasterPlayerPrefs.Singleton.GetBool("ShowHPTextInWorldSpaceLabels"));
        }

        private PlayerDataManager.Team team;
        private void UpdateNameTextAndColors()
        {
            nameDisplay.text = PlayerDataManager.Singleton.GetTeamPrefix(combatAgent.GetTeam()) + combatAgent.GetName();
            nameDisplay.color = Color.white;
            team = combatAgent.GetTeam();

            healthFillImage.color = PlayerDataManager.Singleton.GetRelativeHealthBarColor(combatAgent.GetTeam());
        }

        private CombatAgent localCombatAgent;
        private NetworkObject localSpectator;
        private void FindLocalWeaponHandlerOrSpectator()
        {
            if (PlayerDataManager.Singleton.LocalPlayerData.team == PlayerDataManager.Team.Spectator)
            {
                if (localSpectator)
                {
                    if (localSpectator.gameObject.activeInHierarchy) { localSpectator = null; }
                }

                if (localSpectator) { return; }

                KeyValuePair<ulong, NetworkObject> kvp = PlayerDataManager.Singleton.GetLocalSpectatorObject();
                localSpectator = kvp.Value;
            }
            else
            {
                if (localCombatAgent)
                {
                    if (localCombatAgent.gameObject.activeInHierarchy) { localCombatAgent = null; }
                }

                if (localCombatAgent) { return; }

                KeyValuePair<int, Attributes> kvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                if (kvp.Value)
                {
                    localCombatAgent = kvp.Value;
                }
            }
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
            FindLocalWeaponHandlerOrSpectator();
        }

        private float lastHP = -1;
        private float lastMaxHP = -1;

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

            AnimatorReference.WorldSpaceLabelTransformInfo worldSpaceLabelTransformInfo = combatAgent.AnimationHandler.GetWorldSpaceLabelTransformInfo();
            if (!worldSpaceLabelTransformInfo.rendererToFollow) { Debug.LogWarning("No renderer to follow " + combatAgent); return; }

            Vector3 localScaleTarget = Vector3.zero;
            Vector3 healthBarLocalScaleTarget = Vector3.zero;
            if ((localCombatAgent | localSpectator) & FindMainCamera.MainCamera)
            {
                Transform source = null;
                if (localCombatAgent)
                {
                    source = localCombatAgent.transform;
                }
                else if (localSpectator)
                {
                    source = localSpectator.transform;
                }

                Vector3 pos = worldSpaceLabelTransformInfo.rendererToFollow.bounds.center + worldSpaceLabelTransformInfo.rendererToFollow.transform.rotation * worldSpaceLabelTransformInfo.positionOffsetFromRenderer;
                transform.position = pos;

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(FindMainCamera.MainCamera.transform.position - transform.position), Time.deltaTime * rotationSpeed);

                float playerDistance = Vector3.Distance(source.transform.position, combatAgent.transform.position);
                if (playerDistance > viewDistance)
                {
                    if (Physics.Raycast(FindMainCamera.MainCamera.transform.position + FindMainCamera.MainCamera.transform.forward * viewDistance, FindMainCamera.MainCamera.transform.forward, out RaycastHit hit, 100, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.root == combatAgent.NetworkCollider.transform.root)
                        {
                            if (shouldUseFixedScale)
                                localScaleTarget = new Vector3(fixedScaleValue, fixedScaleValue, fixedScaleValue);
                            else
                                localScaleTarget = Vector3.one * (playerDistance / scalingDistanceDivisor);
                        }
                    }
                }
                else // Cam distance is less than view distance
                {
                    if (shouldUseFixedScale)
                        localScaleTarget = new Vector3(fixedScaleValue, fixedScaleValue, fixedScaleValue);
                    else
                        localScaleTarget = Vector3.one * (playerDistance / scalingDistanceDivisor);

                    if (playerDistance < healthBarViewDistance) { healthBarLocalScaleTarget = Vector3.one; }
                }
                localScaleTarget *= worldSpaceLabelTransformInfo.scaleMultiplier;
            }
            //else
            //{
            //    Debug.LogWarning("Can't find a main camera for world space labels!");
            //}
            transform.localScale = Vector3.Lerp(transform.localScale, localScaleTarget, Time.deltaTime * scalingSpeed);

            canvas.enabled = transform.localScale.magnitude > 0.01f;

            if (healthBarLocalScaleTarget == Vector3.zero)
            {
                if (localSpectator)
                {
                    healthBarLocalScaleTarget = Vector3.one;
                }
                else if(localCombatAgent)
                {
                    if (localCombatAgent.WeaponHandler.CanAim) { healthBarLocalScaleTarget = Vector3.one; }
                }
            }
            healthBarParent.localScale = Vector3.Lerp(healthBarParent.localScale, team == PlayerDataManager.Team.Peaceful ? Vector3.zero : healthBarLocalScaleTarget, Time.deltaTime * scalingSpeed);
            healthText.enabled = team != PlayerDataManager.Team.Peaceful;

            float HP = combatAgent.GetHP() + combatAgent.GetArmor();
            if (HP < 0.1f & HP > 0) { HP = 0.1f; }
            float maxHP = combatAgent.GetMaxHP() + combatAgent.GetMaxArmor();

            if (!Mathf.Approximately(lastHP, HP) | !Mathf.Approximately(lastMaxHP, maxHP))
            {
                healthText.text = "HP " + StringUtility.FormatDynamicFloatForUI(HP) + " / " + maxHP.ToString("F0");
                healthFillImage.fillAmount = HP / maxHP;
            }

            lastHP = HP;
            lastMaxHP = maxHP;

            float armor = combatAgent.GetArmor();
            if (armor < 0.1f & armor > 0) { armor = 0.1f; }
            float maxArmor = combatAgent.GetMaxArmor();

            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, HP / maxHP, Time.deltaTime * PlayerCard.fillSpeed);
        }
    }
}