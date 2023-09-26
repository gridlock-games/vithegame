using GameCreator.Melee;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using System.Collections.Generic;

public class CharacterStatusManager : NetworkBehaviour
{
    public enum CHARACTER_STATUS
    {
        healingMultiplier,
        burning,
        poisoned,
        damageMultiplier,
        damageReductionMultiplier,
        slowedMovement,
        rooted,
        defenseIncreaseMultiplier,
        defenseReductionMultiplier,
        silenced,
        fear,
        healing,
        damageReceivedMultiplier
    }

    private struct CHARACTER_STATUS_NETWORKED : INetworkSerializable, System.IEquatable<CHARACTER_STATUS_NETWORKED>
    {
        public CHARACTER_STATUS charStatus;

        public CHARACTER_STATUS_NETWORKED(CHARACTER_STATUS charStatus)
        {
            this.charStatus = charStatus;
        }

        public bool Equals(CHARACTER_STATUS_NETWORKED other)
        {
            return true;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref charStatus);
        }
    }

    public bool TryAddStatus(CHARACTER_STATUS status)
    {
        if (!IsServer) { Debug.LogError("CharacterStatusManager.TryAddStatus() should only be called on the server"); return false; }

        CHARACTER_STATUS_NETWORKED charStatus = new(status);
        if (characterStatuses.Contains(charStatus))
        {
            return false;
        }
        else
        {
            characterStatuses.Add(charStatus);
        }
        return true;
    }

    public bool TryRemoveStatus(CHARACTER_STATUS status)
    {
        if (!IsServer) { Debug.LogError("CharacterStatusManager.TryRemoveStatus() should only be called on the server"); return false; }

        CHARACTER_STATUS_NETWORKED charStatus = new(status);
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
    private NetworkList<CHARACTER_STATUS_NETWORKED> characterStatuses;

    private void Awake()
    {
        melee = GetComponent<CharacterMelee>();
        meleeUI = GetComponentInChildren<CharacterMeleeUI>(true);
        characterStatuses = new NetworkList<CHARACTER_STATUS_NETWORKED>();
    }

    private float lastChangeTime;
    private bool add;
    private void Update()
    {
        if (Time.time - lastChangeTime > 3 & add)
        {
            Debug.Log("Adding value: " + TryAddStatus(CHARACTER_STATUS.burning));
            lastChangeTime = Time.time;
            add = !add;
        }
        else if (Time.time - lastChangeTime > 3 & !add)
        {
            Debug.Log("Removing value: " + TryRemoveStatus(CHARACTER_STATUS.burning));
            lastChangeTime = Time.time;
            add = !add;
        }
    }

    // EVENT TRIGGERS ------
    //public class StatusUpdateEvent : UnityEvent<CHARACTER_STATUS> { }
    //public StatusUpdateEvent onStatusEvent = new StatusUpdateEvent();

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
        Debug.Log(networkListEvent.Type + " " + networkListEvent.PreviousValue.charStatus + " " + networkListEvent.Value.charStatus);

        meleeUI.UpdateStatusUI();

        //if (IsServer) { return; }

        //StartCoroutine(ExcecuteStatusChange(current));
    }

    //private IEnumerator ExcecuteStatusChange(CHARACTER_STATUS current)
    //{
    //    yield return null;

    //    switch (current)
    //    {
    //        case CHARACTER_STATUS.damageMultiplier:
    //            break;
    //        case CHARACTER_STATUS.damageReductionMultiplier:
    //            break;
    //        case CHARACTER_STATUS.defenseIncreaseMultiplier:
    //            break;
    //        case CHARACTER_STATUS.defenseReductionMultiplier:
    //            break;
    //    }
    //}

    //public void UpdateStatus(CHARACTER_STATUS status)
    //{
    //    StartCoroutine(UpdateStatusCoroutine(status));
    //}

    //private IEnumerator UpdateStatusCoroutine(CHARACTER_STATUS status)
    //{

    //    if (this.character.IsDead() & this.character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.Dead)
    //    {
    //        yield break;
    //    }

    //    CHARACTER_STATUS prevStatus = this.character.characterStatus;

    //    switch (status)
    //    {
    //        // All Ailments should end with reset except Stun which can be cancelled
    //        case CHARACTER_STATUS.damageMultiplier:
    //            break;
    //        case CHARACTER_STATUS.damageReductionMultiplier:
    //            break;
    //        case CHARACTER_STATUS.defenseIncreaseMultiplier:
    //            break;
    //        case CHARACTER_STATUS.defenseReductionMultiplier:
    //            break;
    //    }

    //    this.character.Status(status);
    //    if (IsServer) { characterStatusNetworked.Value = status; }
    //    onStatusEvent.Invoke(status);
    //}

    private bool isCountingdamageMultiplier = false;
    public void damageMultiplierDuration(float multiplierDuration, float damageMultiplier)
    {
        if (!isCountingdamageMultiplier)
        {
            if (melee.IsCastingAbility)
            {
                Ability currentAbility = melee.abilityManager.GetActivatedAbility();

                if (currentAbility.abilityType == Ability.AbilityType.SelfBuff)
                {
                    StartCoroutine(CompleteAnimBeforeDamageMultiplier(currentAbility.meleeClip.animationClip.length, multiplierDuration, damageMultiplier));
                }

            }
            else
            {
                StartCoroutine(DamageMultiplierCoroutine(multiplierDuration, damageMultiplier));
            }
        }
    }

    private IEnumerator CompleteAnimBeforeDamageMultiplier(float animDuration, float multiplierDuration, float damageMultiplier)
    {
        yield return new WaitForSeconds(animDuration);
        StartCoroutine(DamageMultiplierCoroutine(multiplierDuration, damageMultiplier));
    }

    private IEnumerator DamageMultiplierCoroutine(float multiplierDuration, float damageMultiplier)
    {
        isCountingdamageMultiplier = true;
        float elapsedTime = 0;

        while (elapsedTime < multiplierDuration)
        {
            melee.baseDamageMultiplier = damageMultiplier;
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        melee.baseDamageMultiplier = 1.0f;
        isCountingdamageMultiplier = false;
    }

    private bool isCounting = false;

    public void DrainHP(float drainDuration)
    {
        if (!isCounting)
        {
            if (melee.IsCastingAbility)
            {
                Ability currentAbility = melee.abilityManager.GetActivatedAbility();

                if (currentAbility.abilityType == Ability.AbilityType.SelfBuff)
                {
                    StartCoroutine(CompleteAnimBeforeHPDrain(currentAbility.meleeClip.animationClip.length, drainDuration));
                }

            }
            else
            {
                StartCoroutine(HPReductionCoroutine(drainDuration));
            }
        }
    }

    private IEnumerator CompleteAnimBeforeHPDrain(float animDuration, float drainDuration)
    {
        yield return new WaitForSeconds(animDuration);
        StartCoroutine(HPReductionCoroutine(drainDuration));
    }

    private IEnumerator HPReductionCoroutine(float drainDuration)
    {
        isCounting = true;
        float elapsedTime = 0;
        float reductionAmount = 0;

        while (elapsedTime < drainDuration && melee.GetHP() > 1)
        {
            reductionAmount += melee.GetHP() * 0.1f * Time.deltaTime;
            if (reductionAmount >= 1 && melee.GetHP() > 1)
            {

                melee.AddHP(-1 * reductionAmount);
                melee.SetHP(Mathf.Max(melee.GetHP(), 1f));
                reductionAmount = 0;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isCounting = false;
    }

    private bool isSlowed = false;

    public void SlowMovement(float slowDuration)
    {
        if (!isSlowed)
        {
            if (melee.IsCastingAbility)
            {
                Ability currentAbility = melee.abilityManager.GetActivatedAbility();

                if (currentAbility.abilityType == Ability.AbilityType.SelfBuff)
                {
                    StartCoroutine(CompleteAnimBeforeSlowDuration(currentAbility.meleeClip.animationClip.length, slowDuration));
                }

            }
            else
            {
                StartCoroutine(SlowCoroutine(slowDuration));
            }
        }
    }

    private IEnumerator CompleteAnimBeforeSlowDuration(float animDuration, float slowDuration)
    {
        yield return new WaitForSeconds(animDuration);
        StartCoroutine(HPReductionCoroutine(slowDuration));
    }

    private IEnumerator SlowCoroutine(float slowDuration)
    {
        isSlowed = true;
        float elapsedTime = 0;
        float reductionAmount = 0;

        while (elapsedTime < slowDuration && melee.GetHP() > 1)
        {
            reductionAmount += melee.GetHP() * 0.1f * Time.deltaTime;
            if (reductionAmount >= 1 && melee.GetHP() > 1)
            {

                melee.AddHP(-1 * reductionAmount);
                melee.SetHP(Mathf.Max(melee.GetHP(), 1f));
                reductionAmount = 0;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isSlowed = false;
    }
}