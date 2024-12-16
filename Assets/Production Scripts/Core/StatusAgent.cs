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

            StartCoroutine(ProcessStatusChange(statusPayload));
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

        private const bool useConstantRateForHPChangeStatuses = true;
        private const float HPChangeMultiplier = 1;
        private IEnumerator ProcessStatusChange(ActionClip.StatusPayload statusPayload)
        {
            yield return new WaitForSeconds(statusPayload.delay);
            activeStatuses.Add((int)statusPayload.status);
            switch (statusPayload.status)
            {
                case ActionClip.Status.damageMultiplier:
                    DamageMultiplier *= statusPayload.value;

                    float elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageMultiplier /= statusPayload.value;
                    break;
                case ActionClip.Status.damageReductionMultiplier:
                    DamageReductionMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageReductionMultiplier /= statusPayload.value;
                    break;
                case ActionClip.Status.damageReceivedMultiplier:
                    DamageReceivedMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    DamageReceivedMultiplier /= statusPayload.value;
                    break;
                case ActionClip.Status.healingMultiplier:
                    HealingMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    HealingMultiplier /= statusPayload.value;
                    break;
                case ActionClip.Status.spiritIncreaseMultiplier:
                    SpiritIncreaseMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    SpiritIncreaseMultiplier /= statusPayload.value;
                    break;
                case ActionClip.Status.spiritReductionMultiplier:
                    SpiritReductionMultiplier *= statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    SpiritReductionMultiplier /= statusPayload.value;
                    break;
                case ActionClip.Status.burning:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage((useConstantRateForHPChangeStatuses ? HPChangeMultiplier : hittableAgent.GetHP()) * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.poisoned:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage((useConstantRateForHPChangeStatuses ? HPChangeMultiplier : hittableAgent.GetHP()) * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.drain:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        hittableAgent.ProcessEnvironmentDamage((useConstantRateForHPChangeStatuses ? HPChangeMultiplier : hittableAgent.GetHP()) * -statusPayload.value * Time.deltaTime, NetworkObject);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.movementSpeedDecrease:
                    movementSpeedDecrease.Value += statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    movementSpeedDecrease.Value -= statusPayload.value;
                    break;
                case ActionClip.Status.movementSpeedIncrease:
                    movementSpeedIncrease.Value += statusPayload.value;

                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    movementSpeedIncrease.Value -= statusPayload.value;
                    break;
                case ActionClip.Status.rooted:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.silenced:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.fear:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.healing:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        hittableAgent.AddHP((useConstantRateForHPChangeStatuses ? HPChangeMultiplier : hittableAgent.GetMaxHP() / hittableAgent.GetHP() * 10) * statusPayload.value * Time.deltaTime);
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }
                    break;
                case ActionClip.Status.immuneToGroundSpells:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.immuneToAilments:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                case ActionClip.Status.immuneToNegativeStatuses:
                    elapsedTime = 0;
                    while (elapsedTime < statusPayload.duration & !stopAllStatuses)
                    {
                        elapsedTime += Time.deltaTime;
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            if (stopAllStatusesAssociatedWithWeapon) { break; }
                        }
                        yield return null;
                    }

                    break;
                default:
                    Debug.LogError(statusPayload.status + " has not been implemented!");
                    break;
            }
        }
    }
}