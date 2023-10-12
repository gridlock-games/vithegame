using GameCreator.Melee;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using LightPat.UI;

public class CharacterStatusManager : NetworkBehaviour
{
    public enum CHARACTER_STATUS
    {
        damageMultiplier,
        damageReductionMultiplier,
        damageReceivedMultiplier,
        healingMultiplier,
        defenseIncreaseMultiplier,
        defenseReductionMultiplier,
        burning,
        poisoned,
        drain,
        slowedMovement,
        rooted,
        silenced,
        fear,
        healing
    }

    private struct CHARACTER_STATUS_NETWORKED : INetworkSerializable, System.IEquatable<CHARACTER_STATUS_NETWORKED>
    {
        public CHARACTER_STATUS charStatus;
        public float value;
        public float duration;
        public float delay;

        public CHARACTER_STATUS_NETWORKED(CHARACTER_STATUS charStatus, float value, float duration, float delay)
        {
            this.charStatus = charStatus;
            this.value = value;
            this.duration = duration;
            this.delay = delay;
        }

        public bool Equals(CHARACTER_STATUS_NETWORKED other)
        {
            return charStatus == other.charStatus;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref charStatus);
            serializer.SerializeValue(ref value);
            serializer.SerializeValue(ref duration);
            serializer.SerializeValue(ref delay);
        }
    }

    public bool TryAddStatus(CHARACTER_STATUS status, float value, float duration, float delay)
    {
        if (!IsServer) { Debug.LogError("CharacterStatusManager.TryAddStatus() should only be called on the server"); return false; }

        CHARACTER_STATUS_NETWORKED charStatus = new(status, value, duration, delay);
        if (characterStatuses.Contains(charStatus))
        {
            int index = characterStatuses.IndexOf(charStatus);
            characterStatuses[index] = charStatus;
        }
        else
        {
            characterStatuses.Add(charStatus);
        }
        return true;
    }

    private bool TryRemoveStatus(CHARACTER_STATUS status)
    {
        if (!IsServer) { Debug.LogError("CharacterStatusManager.TryRemoveStatus() should only be called on the server"); return false; }

        CHARACTER_STATUS_NETWORKED charStatus = new(status, 0, 0, 0);
        if (!characterStatuses.Contains(charStatus))
        {
            return false;
        }
        else
        {
            characterStatuses.Remove(charStatus);
        }
        return true;
    }

    public List<CHARACTER_STATUS> GetCharacterStatusList()
    {
        List<CHARACTER_STATUS> statuses = new List<CHARACTER_STATUS>();
        foreach (var status in characterStatuses)
        {
            statuses.Add(status.charStatus);
        }
        return statuses;
    }

    private CharacterMelee melee;
    private CharacterMeleeUI meleeUI;
    private WorldSpaceLabel worldSpaceLabel;
    private NetworkList<CHARACTER_STATUS_NETWORKED> characterStatuses;

    private void Awake()
    {
        melee = GetComponent<CharacterMelee>();
        meleeUI = GetComponentInChildren<CharacterMeleeUI>(true);
        worldSpaceLabel = GetComponentInChildren<WorldSpaceLabel>(true);
        characterStatuses = new NetworkList<CHARACTER_STATUS_NETWORKED>();
    }

    private float lastChangeTime;
    private bool add;
    private void LateUpdate()
    {
        if (!IsServer) { return; }

        //if (Time.time - lastChangeTime > 3 & add)
        //{
        //    TryAddStatus(CHARACTER_STATUS.healing, 25, 3, 0);
        //    TryAddStatus(CHARACTER_STATUS.healingMultiplier, 2, 3, 0);
        //    lastChangeTime = Time.time;
        //    add = !add;
        //}
        //else if (Time.time - lastChangeTime > 3 & !add)
        //{
        //    TryAddStatus(CHARACTER_STATUS.burning, 50, 4, 0);
        //    lastChangeTime = Time.time;
        //    add = !add;
        //}

        RemoveStatusCheck();
    }

    public override void OnNetworkSpawn()
    {
        characterStatuses.OnListChanged += OnStatusChange;
    }

    public override void OnNetworkDespawn()
    {
        characterStatuses.OnListChanged -= OnStatusChange;
    }

    private void OnStatusChange(NetworkListEvent<CHARACTER_STATUS_NETWORKED> networkListEvent)
    {
        if (meleeUI)
        {
            meleeUI.UpdateStatusUI();
        }

        if (worldSpaceLabel)
        {
            worldSpaceLabel.UpdateStatusUI();
        }

        if (!IsServer) { return; }

        if (networkListEvent.Type == NetworkListEvent<CHARACTER_STATUS_NETWORKED>.EventType.Add | networkListEvent.Type == NetworkListEvent<CHARACTER_STATUS_NETWORKED>.EventType.Value)
        {
            switch (networkListEvent.Value.charStatus)
            {
                case CHARACTER_STATUS.damageMultiplier:
                    melee.SetDamageMultiplier(networkListEvent.Value.value, networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.damageReductionMultiplier:
                    melee.SetDamageReductionMultiplier(networkListEvent.Value.value, networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.damageReceivedMultiplier:
                    melee.SetDamageReceivedMultiplier(networkListEvent.Value.value, networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.healingMultiplier:
                    melee.SetHealingMultiplier(networkListEvent.Value.value, networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.defenseIncreaseMultiplier:
                    melee.SetDefenseIncreaseMultiplier(networkListEvent.Value.value, networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.defenseReductionMultiplier:
                    melee.SetDefenseReductionMultiplier(networkListEvent.Value.value, networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.burning:
                    melee.DrainHPOverTime(networkListEvent.Value.value, networkListEvent.Value.duration, networkListEvent.Value.delay);
                    break;
                case CHARACTER_STATUS.poisoned:
                    melee.DrainHPOverTime(networkListEvent.Value.value, networkListEvent.Value.duration, networkListEvent.Value.delay);
                    break;
                case CHARACTER_STATUS.drain:
                    melee.DrainHPOverTime(networkListEvent.Value.value, networkListEvent.Value.duration, networkListEvent.Value.delay);
                    break;
                case CHARACTER_STATUS.slowedMovement:
                    melee.SlowMovement(networkListEvent.Value.value, networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.rooted:
                    melee.Root(networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.silenced:
                    melee.Silence(networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.fear:
                    melee.Fear(networkListEvent.Value.duration);
                    break;
                case CHARACTER_STATUS.healing:
                    melee.HealHPOverTime(networkListEvent.Value.value, networkListEvent.Value.duration, networkListEvent.Value.delay);
                    break;
                default:
                    Debug.Log(networkListEvent.Value.charStatus + " has not been implemented for status add or value change");
                    break;
            }
        }
    }

    private void RemoveStatusCheck()
    {
        foreach (var status in GetCharacterStatusList())
        {
            switch (status)
            {
                case CHARACTER_STATUS.damageMultiplier:
                    if (melee.damageMultiplier.Value == 1)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.damageMultiplier);
                    }
                    break;
                case CHARACTER_STATUS.damageReductionMultiplier:
                    if (melee.damageReductionMultiplier.Value == 1)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.damageReductionMultiplier);
                    }
                    break;
                case CHARACTER_STATUS.damageReceivedMultiplier:
                    if (melee.damageReceivedMultiplier.Value == 1)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.damageReceivedMultiplier);
                    }
                    break;
                case CHARACTER_STATUS.healingMultiplier:
                    if (melee.healingMultiplier.Value == 1)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.healingMultiplier);
                    }
                    break;
                case CHARACTER_STATUS.defenseIncreaseMultiplier:
                    if (melee.defenseIncreaseMultiplier.Value == 1)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.defenseIncreaseMultiplier);
                    }
                    break;
                case CHARACTER_STATUS.defenseReductionMultiplier:
                    if (melee.defenseReductionMultiplier.Value == 1)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.defenseReductionMultiplier);
                    }
                    break;
                case CHARACTER_STATUS.burning:
                    if (!melee.drainActive.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.burning);
                    }
                    break;
                case CHARACTER_STATUS.poisoned:
                    if (!melee.drainActive.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.poisoned);
                    }
                    break;
                case CHARACTER_STATUS.drain:
                    if (!melee.drainActive.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.drain);
                    }
                    break;
                case CHARACTER_STATUS.slowedMovement:
                    if (!melee.slowed.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.slowedMovement);
                    }
                    break;
                case CHARACTER_STATUS.rooted:
                    if (!melee.rooted.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.rooted);
                    }
                    break;
                case CHARACTER_STATUS.silenced:
                    if (!melee.silenced.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.silenced);
                    }
                    break;
                case CHARACTER_STATUS.fear:
                    if (!melee.fearing.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.fear);
                    }
                    break;
                case CHARACTER_STATUS.healing:
                    if (!melee.healActive.Value)
                    {
                        TryRemoveStatus(CHARACTER_STATUS.healing);
                    }
                    break;
                default:
                    Debug.Log(status + " has not been implemented for removal");
                    break;
            }
        }
    }
}