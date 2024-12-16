using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class StatusAgent : NetworkBehaviour
    {
        private HittableAgent hittableAgent;
        private void Awake()
        {
            activeStatuses = new NetworkList<int>();
            hittableAgent = GetComponent<HittableAgent>();
        }

        private void OnDisable()
        {
            stopAllStatuses = default;
            stopAllStatusesAssociatedWithWeapon = default;

            DamageMultiplier = 1;
            DamageReductionMultiplier = 1;
            DamageReceivedMultiplier = 1;
            HealingMultiplier = 1;
            SpiritIncreaseMultiplier = 1;
            SpiritReductionMultiplier = 1;

            ActiveStatusesWasUpdatedThisFrame = default;

            statusRoutines.Clear();
            statusEventId = default;
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
            ActionClip.Status.burning,
            ActionClip.Status.damageReceivedMultiplier,
            ActionClip.Status.drain,
            ActionClip.Status.movementSpeedDecrease,
            ActionClip.Status.poisoned,
            ActionClip.Status.rooted,
            ActionClip.Status.silenced,
            ActionClip.Status.spiritIncreaseMultiplier
        };

        private int statusEventId;
        private Dictionary<int, ActionClip.StatusPayload> statusRoutines = new Dictionary<int, ActionClip.StatusPayload>();
        public int AddConditionalStatus(ActionClip.StatusPayload statusPayload)
        {
            if (!IsSpawned) { Debug.LogError("StatusAgent.AddConditionalStatus() should onyl be called when we're spawned"); return 0; }
            if (!IsServer) { Debug.LogError("StatusAgent.AddConditionalStatus() should only be called on the server"); return 0; }

            statusPayload.duration = Mathf.Infinity;

            statusEventId++;
            StartCoroutine(ProcessStatusChange(statusEventId, statusPayload));
            return statusEventId;
        }

        public void RemoveConditionalStatus(int statusEventIdToRemove)
        {
            if (!IsSpawned) { Debug.LogError("StatusAgent.RemoveConditionalStatus() should onyl be called when we're spawned"); return; }
            if (!IsServer) { Debug.LogError("StatusAgent.RemoveConditionalStatus() should only be called on the server"); return; }

            if (statusRoutines.ContainsKey(statusEventIdToRemove))
            {
                ActionClip.StatusPayload change = statusRoutines[statusEventIdToRemove];
                change.duration = 0;
                statusRoutines[statusEventIdToRemove] = change;
            }
            else
            {
                Debug.LogWarning("Status event id not found for removal! " + statusEventIdToRemove);
            }
        }

        public bool TryAddStatus(ActionClip.StatusPayload statusPayload)
        {
            if (!IsSpawned) { Debug.LogError("StatusAgent.TryAddStatus() should onyl be called when we're spawned"); return false; }
            if (!IsServer) { Debug.LogError("StatusAgent.TryAddStatus() should only be called on the server"); return false; }

            if (negativeStatuses.Contains(statusPayload.status))
            {
                if (GetActiveStatuses().Contains(ActionClip.Status.immuneToNegativeStatuses))
                {
                    return false;
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
        public float SpiritIncreaseMultiplier { get; private set; } = 1;
        public float SpiritReductionMultiplier { get; private set; } = 1;

        public float GetMovementSpeedDecreaseAmount() { return movementSpeedDecrease.Value; }
        private NetworkVariable<float> movementSpeedDecrease = new NetworkVariable<float>();

        public float GetMovementSpeedIncreaseAmount() { return movementSpeedIncrease.Value; }
        private NetworkVariable<float> movementSpeedIncrease = new NetworkVariable<float>();

        public bool IsRooted() { return activeStatuses.Contains((int)ActionClip.Status.rooted); }
        public bool IsSilenced() { return activeStatuses.Contains((int)ActionClip.Status.silenced); }
        public bool IsFeared() { return activeStatuses.Contains((int)ActionClip.Status.fear); }
        public bool IsImmuneToGroundSpells() { return activeStatuses.Contains((int)ActionClip.Status.immuneToGroundSpells); }

        public bool ActiveStatusesWasUpdatedThisFrame { get; private set; }
        private void OnActiveStatusChange(NetworkListEvent<int> networkListEvent)
        {
            ActiveStatusesWasUpdatedThisFrame = true;
            if (resetActiveStatusesBoolCoroutine != null) { StopCoroutine(resetActiveStatusesBoolCoroutine); }
            resetActiveStatusesBoolCoroutine = StartCoroutine(ResetActiveStatusesWasUpdatedBool());
        }

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

        private IEnumerator ProcessStatusChange(int statusEventId, ActionClip.StatusPayload statusPayload)
        {
            if (statusRoutines.ContainsKey(statusEventId))
            {
                Debug.LogWarning("Status event id is already in dictionary!");
                yield break;
            }

            statusRoutines.Add(statusEventId, statusPayload);
            
            yield return new WaitForSeconds(statusRoutines[statusEventId].delay);
            activeStatuses.Add((int)statusRoutines[statusEventId].status);
            switch (statusRoutines[statusEventId].status)
            {
                case ActionClip.Status.damageMultiplier:
                    DamageMultiplier *= statusRoutines[statusEventId].value;

                    float elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageMultiplier /= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.damageReductionMultiplier:
                    DamageReductionMultiplier *= statusRoutines[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageReductionMultiplier /= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.damageReceivedMultiplier:
                    DamageReceivedMultiplier *= statusRoutines[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageReceivedMultiplier /= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.healingMultiplier:
                    HealingMultiplier *= statusRoutines[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    HealingMultiplier /= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.spiritIncreaseMultiplier:
                    SpiritIncreaseMultiplier *= statusRoutines[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    SpiritIncreaseMultiplier /= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.spiritReductionMultiplier:
                    SpiritReductionMultiplier *= statusRoutines[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    SpiritReductionMultiplier /= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.burning:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage(-GetHPChangeAmount(statusRoutines[statusEventId]) * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.poisoned:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage(-GetHPChangeAmount(statusRoutines[statusEventId]) * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.drain:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage(-GetHPChangeAmount(statusRoutines[statusEventId]) * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.movementSpeedDecrease:
                    movementSpeedDecrease.Value += statusRoutines[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    movementSpeedDecrease.Value -= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.movementSpeedIncrease:
                    movementSpeedIncrease.Value += statusRoutines[statusEventId].value;

                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    movementSpeedIncrease.Value -= statusRoutines[statusEventId].value;
                    break;
                case ActionClip.Status.rooted:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.silenced:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.fear:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.healing:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        hittableAgent.AddHP(GetHPChangeAmount(statusRoutines[statusEventId]) * Time.deltaTime);
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.immuneToGroundSpells:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.immuneToAilments:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.immuneToNegativeStatuses:
                    elapsedTime = 0;
                    while (elapsedTime < statusRoutines[statusEventId].duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusRoutines[statusEventId].associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                default:
                    Debug.LogError(statusRoutines[statusEventId].status + " has not been implemented!");
                    break;
            }
            statusRoutines.Remove(statusEventId);
        }
    }
}