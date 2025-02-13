using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Vi.ScriptableObjects;
using System.Linq;
using Vi.Utility;

namespace Vi.Core
{
    public class StatusAgent : NetworkBehaviour
    {
        private HittableAgent hittableAgent;
        private CombatAgent combatAgent;
        private void Awake()
        {
            activeStatuses = new NetworkList<int>();
            hittableAgent = GetComponent<HittableAgent>();
            combatAgent = GetComponent<CombatAgent>();

            if (TryGetComponent(out PooledObject pooledObject))
            {
                pooledObject.OnReturnToPool += OnReturnToPool;
            }
        }

        private void OnEnable()
        {
            statusEvents.Add(this, new Dictionary<int, ActionClip.StatusPayload>());
        }

        private void OnDisable()
        {
            stopAllStatuses = default;
            stopAllStatusesAssociatedWithWeapon = default;

            DamageMultiplier = 1;
            DamageReductionMultiplier = 1;
            DamageReceivedMultiplier = 1;
            HealingMultiplier = 1;
            ArmorIncreaseMultiplier = 1;
            ArmorReductionMultiplier = 1;

            ActiveStatusesWasUpdatedThisFrame = default;

            statusEvents.Remove(this);
            statusEventId = default;
        }

        private void OnReturnToPool()
        {
            foreach (KeyValuePair<ActionClip.Status, PooledObject> kvp in statusTracker)
            {
                ObjectPoolingManager.ReturnObjectToPool(kvp.Value);
            }
            statusTracker.Clear();
        }

        public override void OnNetworkSpawn()
        {
            activeStatuses.OnListChanged += OnActiveStatusChange;

            if (IsServer)
            {
                activeStatuses.Clear();
            }
        }

        public override void OnNetworkDespawn()
        {
            activeStatuses.OnListChanged -= OnActiveStatusChange;
        }

        public List<ActionClip.Status> GetActiveStatuses()
        {
            List<ActionClip.Status> statusList = new List<ActionClip.Status>();
            for (int i = 0; i < activeStatuses.Count; i++)
            {
                statusList.Add((ActionClip.Status)activeStatuses[i]);
            }
            return statusList;
        }

        private NetworkList<int> activeStatuses;

        private static readonly List<ActionClip.Status> negativeStatuses = new List<ActionClip.Status>()
        {
            ActionClip.Status.damageReceivedMultiplier,
            ActionClip.Status.armorReductionMultiplier,
            ActionClip.Status.burning,
            ActionClip.Status.poisoned,
            ActionClip.Status.drain,
            ActionClip.Status.movementSpeedDecrease,
            ActionClip.Status.rooted,
            ActionClip.Status.silenced,
            ActionClip.Status.fear,
            ActionClip.Status.attackSpeedDecrease,
            ActionClip.Status.abilityCooldownIncrease
        };

        private static Dictionary<StatusAgent, Dictionary<int, ActionClip.StatusPayload>> statusEvents = new Dictionary<StatusAgent, Dictionary<int, ActionClip.StatusPayload>>();

        private Dictionary<int, ActionClip.StatusPayload> StatusEventsForThisObject
        {
            get
            {
                if (statusEvents.TryGetValue(this, out Dictionary<int, ActionClip.StatusPayload> result))
                {
                    return result;
                }
                else
                {
                    Debug.LogWarning("Could not find status events for this object! " + this);
                    return new Dictionary<int, ActionClip.StatusPayload>();
                }
            }
        }

        private int statusEventId;
        public (bool, int) AddConditionalStatus(ActionClip.StatusPayload statusPayload, float maxDuration = Mathf.Infinity)
        {
            if (!IsSpawned) { Debug.LogError("StatusAgent.AddConditionalStatus() should onyl be called when we're spawned"); return default; }
            if (!IsServer) { Debug.LogError("StatusAgent.AddConditionalStatus() should only be called on the server"); return default; }

            if (blacklistedStatuses.Contains(statusPayload.status)) { return default; }

            statusPayload.duration = maxDuration;

            statusEventId++;
            StartCoroutine(ProcessStatusChange(statusEventId, statusPayload));
            return (true, statusEventId);
        }

        public void RemoveConditionalStatus(int statusEventIdToRemove)
        {
            if (!IsSpawned) { Debug.LogError("StatusAgent.RemoveConditionalStatus() should onyl be called when we're spawned"); return; }
            if (!IsServer) { Debug.LogError("StatusAgent.RemoveConditionalStatus() should only be called on the server"); return; }

            if (StatusEventsForThisObject.ContainsKey(statusEventIdToRemove))
            {
                ActionClip.StatusPayload change = StatusEventsForThisObject[statusEventIdToRemove];
                change.duration = 0;
                StatusEventsForThisObject[statusEventIdToRemove] = change;
            }
            else
            {
                Debug.LogWarning("Status event id not found for removal! " + statusEventIdToRemove);
            }
        }

        [SerializeField] private ActionClip.Status[] blacklistedStatuses = new ActionClip.Status[0];

        public bool TryAddStatus(ActionClip.StatusPayload statusPayload)
        {
            if (!IsSpawned) { Debug.LogError("StatusAgent.TryAddStatus() should onyl be called when we're spawned"); return false; }
            if (!IsServer) { Debug.LogError("StatusAgent.TryAddStatus() should only be called on the server"); return false; }

            if (blacklistedStatuses.Contains(statusPayload.status)) { return false; }

            if (negativeStatuses.Contains(statusPayload.status))
            {
                if (GetActiveStatuses().Contains(ActionClip.Status.immuneToNegativeStatuses))
                {
                    statusPayload.duration = 0;
                }
            }

            statusEventId++;
            StartCoroutine(ProcessStatusChange(statusEventId, statusPayload));
            return true;
        }

        private bool stopAllStatuses;
        public void RemoveAllStatuses()
        {
            if (!IsSpawned) { Debug.LogError("StatusAgent.RemoveAddStatus() should onyl be called when we're spawned"); return; }
            if (!IsServer) { Debug.LogError("StatusAgent.RemoveAllStatuses() should only be called on the server"); return; }

            if (stopAllStatusesCoroutine != null) { StopCoroutine(stopAllStatusesCoroutine); }

            if (gameObject.activeInHierarchy)
            {
                stopAllStatuses = true;
                stopAllStatusesCoroutine = StartCoroutine(ResetStopAllStatusesBool());
            }
            else
            {
                Debug.LogWarning("Why are you calling StatusAgent.RemoveAllStatuses() when the object is inactive in hierarchy");
            }
        }

        private Coroutine stopAllStatusesCoroutine;
        private IEnumerator ResetStopAllStatusesBool()
        {
            yield return null;
            yield return null;
            stopAllStatuses = false;
        }

        private bool stopAllStatusesAssociatedWithWeapon;
        public void RemoveAllStatusesAssociatedWithWeapon()
        {
            if (!IsServer) { Debug.LogError("Attributes.RemoveAllStatusesAssociatedWithWeapon() should only be called on the server"); return; }

            if (stopAllStatusesAssociatedWithWeaponCoroutine != null) { StopCoroutine(stopAllStatusesAssociatedWithWeaponCoroutine); }

            if (gameObject.activeInHierarchy)
            {
                stopAllStatusesAssociatedWithWeapon = true;
                stopAllStatusesAssociatedWithWeaponCoroutine = StartCoroutine(ResetStopAllStatusesAssociatedWithWeaponBool());
            }
            else
            {
                Debug.LogWarning("Why are you calling StatusAgent.RemoveAllStatusesAssociatedWithWeapon() when the object is inactive in hierarchy");
            }
        }

        private Coroutine stopAllStatusesAssociatedWithWeaponCoroutine;
        private IEnumerator ResetStopAllStatusesAssociatedWithWeaponBool()
        {
            yield return null;
            yield return null;
            stopAllStatusesAssociatedWithWeapon = false;
        }

        public float DamageMultiplier { get; private set; } = 1;
        public float DamageReductionMultiplier { get; private set; } = 1;
        public float DamageReceivedMultiplier { get; private set; } = 1;
        public float HealingMultiplier { get; private set; } = 1;
        public float ArmorIncreaseMultiplier { get; private set; } = 1;
        public float ArmorReductionMultiplier { get; private set; } = 1;

        private NetworkVariable<float> movementSpeedDecreaseAmount = new NetworkVariable<float>();
        private NetworkVariable<float> movementSpeedDecreasePercentage = new NetworkVariable<float>();

        public float GetMovementSpeedDecreaseAmount(float baseRunSpeed)
        {
            float decrease = movementSpeedDecreaseAmount.Value;
            decrease += baseRunSpeed * movementSpeedDecreasePercentage.Value;
            return Mathf.Max(0, decrease);
        }

        private NetworkVariable<float> movementSpeedIncreaseAmount = new NetworkVariable<float>();
        private NetworkVariable<float> movementSpeedIncreasePercentage = new NetworkVariable<float>();

        public float GetMovementSpeedIncreaseAmount(float baseRunSpeed)
        {
            float increase = movementSpeedIncreaseAmount.Value;
            increase += baseRunSpeed * movementSpeedIncreasePercentage.Value;
            return Mathf.Max(0, increase);
        }

        private NetworkVariable<float> attackSpeedDecreaseAmount = new NetworkVariable<float>();
        private NetworkVariable<float> attackSpeedDecreasePercentage = new NetworkVariable<float>();

        public float GetAttackSpeedDecreaseAmount(float baseAttackSpeed)
        {
            float decrease = attackSpeedDecreaseAmount.Value;
            decrease += baseAttackSpeed * attackSpeedDecreasePercentage.Value;
            return Mathf.Max(0, decrease);
        }

        private NetworkVariable<float> attackSpeedIncreaseAmount = new NetworkVariable<float>();
        private NetworkVariable<float> attackSpeedIncreasePercentage = new NetworkVariable<float>();

        public float GetAttackSpeedIncreaseAmount(float baseAttackSpeed)
        {
            float increase = attackSpeedIncreaseAmount.Value;
            increase += baseAttackSpeed * attackSpeedIncreasePercentage.Value;
            return Mathf.Max(0, increase);
        }

        private NetworkVariable<float> abilityCooldownDecreaseAmount = new NetworkVariable<float>();
        private NetworkVariable<float> abilityCooldownDecreasePercentage = new NetworkVariable<float>();

        private float GetAbilityCooldownDecreaseAmount()
        {
            float decrease = abilityCooldownDecreaseAmount.Value;
            decrease += abilityCooldownDecreasePercentage.Value;
            return Mathf.Max(0, decrease);
        }

        private NetworkVariable<float> abilityCooldownIncreaseAmount = new NetworkVariable<float>();
        private NetworkVariable<float> abilityCooldownIncreasePercentage = new NetworkVariable<float>();

        private float GetAbilityCooldownIncreaseAmount()
        {
            float increase = abilityCooldownIncreaseAmount.Value;
            increase += abilityCooldownIncreasePercentage.Value;
            return Mathf.Max(0, increase);
        }

        public float GetAbilityCooldownMultiplier()
        {
            return Mathf.Max(0, 1 - GetAbilityCooldownDecreaseAmount() + GetAbilityCooldownIncreaseAmount());
        }

        public bool IsRooted() { return activeStatuses.Contains((int)ActionClip.Status.rooted); }
        public bool IsSilenced() { return activeStatuses.Contains((int)ActionClip.Status.silenced); }
        public bool IsFeared() { return activeStatuses.Contains((int)ActionClip.Status.fear); }
        public bool IsImmuneToGroundSpells() { return activeStatuses.Contains((int)ActionClip.Status.immuneToGroundSpells); }

        public bool ActiveStatusesWasUpdatedThisFrame { get; private set; }
        private Dictionary<ActionClip.Status, PooledObject> statusTracker = new Dictionary<ActionClip.Status, PooledObject>();
        private void OnActiveStatusChange(NetworkListEvent<int> networkListEvent)
        {
            ActiveStatusesWasUpdatedThisFrame = true;
            if (resetActiveStatusesBoolCoroutine != null) { StopCoroutine(resetActiveStatusesBoolCoroutine); }
            resetActiveStatusesBoolCoroutine = StartCoroutine(ResetActiveStatusesWasUpdatedBool());

            if (networkListEvent.Type == NetworkListEvent<int>.EventType.Add)
            {
                if (!statusTracker.ContainsKey((ActionClip.Status)networkListEvent.Value))
                {
                    int index = System.Array.FindIndex(statusVFXDefinitions, item => item.status == (ActionClip.Status)networkListEvent.Value);
                    if (index != -1)
                    {
                        PooledObject pooledObject = ObjectPoolingManager.SpawnObject(statusVFXDefinitions[index].statusVFX.GetComponent<PooledObject>(), transform);
                        statusTracker.Add((ActionClip.Status)networkListEvent.Value, pooledObject);
                    }
                }
            }
            else if (networkListEvent.Type == NetworkListEvent<int>.EventType.Remove | networkListEvent.Type == NetworkListEvent<int>.EventType.RemoveAt)
            {
                if (statusTracker.TryGetValue((ActionClip.Status)networkListEvent.Value, out PooledObject value))
                {
                    StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(value));
                    statusTracker.Remove((ActionClip.Status)networkListEvent.Value);
                }
            }
        }

        [System.Serializable]
        private class StatusVFXDefinition
        {
            public ActionClip.Status status;
            public StatusVFX statusVFX;
        }

        [SerializeField] private StatusVFXDefinition[] statusVFXDefinitions = new StatusVFXDefinition[0];

        private Coroutine resetActiveStatusesBoolCoroutine;
        private IEnumerator ResetActiveStatusesWasUpdatedBool()
        {
            yield return null;
            ActiveStatusesWasUpdatedThisFrame = false;
        }

        private float GetHPChangeAmount(ActionClip.StatusPayload statusPayload)
        {
            float changeAmount;
            if (statusPayload.valueIsPercentage)
            {
                changeAmount = hittableAgent.GetMaxHP() * statusPayload.value;
            }
            else
            {
                changeAmount = statusPayload.value;
            }
            return changeAmount;
        }

        private float GetArmorChangeAmount(ActionClip.StatusPayload statusPayload)
        {
            if (!combatAgent) { return 0; }

            float changeAmount;
            if (statusPayload.valueIsPercentage)
            {
                changeAmount = combatAgent.GetMaxPhysicalArmor() * statusPayload.value;
            }
            else
            {
                changeAmount = statusPayload.value;
            }
            return changeAmount;
        }

        private float GetStaminaChangeAmount(ActionClip.StatusPayload statusPayload)
        {
            if (!combatAgent) { return 0; }

            float changeAmount;
            if (statusPayload.valueIsPercentage)
            {
                changeAmount = combatAgent.GetMaxStamina() * statusPayload.value;
            }
            else
            {
                changeAmount = statusPayload.value;
            }
            return changeAmount;
        }

        private IEnumerator ProcessStatusChange(int statusEventId, ActionClip.StatusPayload statusPayload)
        {
            if (StatusEventsForThisObject.ContainsKey(statusEventId))
            {
                Debug.LogWarning("Status event id is already in dictionary!");
                yield break;
            }

            StatusEventsForThisObject.Add(statusEventId, statusPayload);
            
            yield return new WaitForSeconds(StatusEventsForThisObject[statusEventId].delay);
            activeStatuses.Add((int)StatusEventsForThisObject[statusEventId].status);
            switch (StatusEventsForThisObject[statusEventId].status)
            {
                case ActionClip.Status.damageMultiplier:
                    DamageMultiplier *= StatusEventsForThisObject[statusEventId].value;

                    float elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageMultiplier /= StatusEventsForThisObject[statusEventId].value;
                    break;
                case ActionClip.Status.damageReductionMultiplier:
                    DamageReductionMultiplier *= StatusEventsForThisObject[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageReductionMultiplier /= StatusEventsForThisObject[statusEventId].value;
                    break;
                case ActionClip.Status.damageReceivedMultiplier:
                    DamageReceivedMultiplier *= StatusEventsForThisObject[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageReceivedMultiplier /= StatusEventsForThisObject[statusEventId].value;
                    break;
                case ActionClip.Status.healingMultiplier:
                    HealingMultiplier *= StatusEventsForThisObject[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    HealingMultiplier /= StatusEventsForThisObject[statusEventId].value;
                    break;
                case ActionClip.Status.armorIncreaseMultiplier:
                    ArmorIncreaseMultiplier *= StatusEventsForThisObject[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    ArmorIncreaseMultiplier /= StatusEventsForThisObject[statusEventId].value;
                    break;
                case ActionClip.Status.armorReductionMultiplier:
                    ArmorReductionMultiplier *= StatusEventsForThisObject[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    ArmorReductionMultiplier /= StatusEventsForThisObject[statusEventId].value;
                    break;
                case ActionClip.Status.burning:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage(-GetHPChangeAmount(StatusEventsForThisObject[statusEventId]) * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.poisoned:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage(-GetHPChangeAmount(StatusEventsForThisObject[statusEventId]) * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.drain:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage(-GetHPChangeAmount(StatusEventsForThisObject[statusEventId]) * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.movementSpeedDecrease:
                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        movementSpeedDecreasePercentage.Value += StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        movementSpeedDecreaseAmount.Value += StatusEventsForThisObject[statusEventId].value;
                    }

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        movementSpeedDecreasePercentage.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        movementSpeedDecreaseAmount.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    break;
                case ActionClip.Status.movementSpeedIncrease:
                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        movementSpeedIncreasePercentage.Value += StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        movementSpeedIncreaseAmount.Value += StatusEventsForThisObject[statusEventId].value;
                    }

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        movementSpeedIncreasePercentage.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        movementSpeedIncreaseAmount.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    break;
                case ActionClip.Status.rooted:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.silenced:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.fear:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.healing:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.AddHP(GetHPChangeAmount(StatusEventsForThisObject[statusEventId]) * Time.deltaTime);
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.immuneToGroundSpells:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.immuneToAilments:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.immuneToNegativeStatuses:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.physicalArmorRegeneration:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        if (combatAgent)
                        {
                            combatAgent.AddPhysicalArmor(GetArmorChangeAmount(StatusEventsForThisObject[statusEventId]) * Time.deltaTime);
                        }

                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.staminaRegeneration:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        if (combatAgent)
                        {
                            combatAgent.AddStamina(GetStaminaChangeAmount(StatusEventsForThisObject[statusEventId]) * Time.deltaTime);
                        }

                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.attackSpeedDecrease:
                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        attackSpeedDecreasePercentage.Value += StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        attackSpeedDecreaseAmount.Value += StatusEventsForThisObject[statusEventId].value;
                    }

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        attackSpeedDecreasePercentage.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        attackSpeedDecreaseAmount.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    break;
                case ActionClip.Status.attackSpeedIncrease:
                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        attackSpeedIncreasePercentage.Value += StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        attackSpeedIncreaseAmount.Value += StatusEventsForThisObject[statusEventId].value;
                    }

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        attackSpeedIncreasePercentage.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        attackSpeedIncreaseAmount.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    break;
                case ActionClip.Status.abilityCooldownDecrease:
                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        abilityCooldownDecreasePercentage.Value += StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        abilityCooldownDecreaseAmount.Value += StatusEventsForThisObject[statusEventId].value;
                    }

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        abilityCooldownDecreasePercentage.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        abilityCooldownDecreaseAmount.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    break;
                case ActionClip.Status.abilityCooldownIncrease:
                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        abilityCooldownIncreasePercentage.Value += StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        abilityCooldownIncreaseAmount.Value += StatusEventsForThisObject[statusEventId].value;
                    }

                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    if (StatusEventsForThisObject[statusEventId].valueIsPercentage)
                    {
                        abilityCooldownIncreasePercentage.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    else
                    {
                        abilityCooldownIncreaseAmount.Value -= StatusEventsForThisObject[statusEventId].value;
                    }
                    break;
                case ActionClip.Status.bleed:
                    elapsedTime = 0;
                    while (elapsedTime < StatusEventsForThisObject[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage(-GetHPChangeAmount(StatusEventsForThisObject[statusEventId]) * Time.deltaTime, NetworkObject, true);
                        elapsedTime += Time.deltaTime;
                        if (StatusEventsForThisObject[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                default:
                    Debug.LogError(StatusEventsForThisObject[statusEventId].status + " has not been implemented!");
                    break;
            }
            activeStatuses.Remove((int)StatusEventsForThisObject[statusEventId].status);
            StatusEventsForThisObject.Remove(statusEventId);
        }
    }
}