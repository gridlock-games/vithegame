using GameCreator.Melee;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Unity.Netcode;
using UnityEngine.Events;
using GameCreator.Core;
using GameCreator.Characters;
using GameCreator.Variables;
using GameCreator.Pool;
using static GameCreator.Melee.MeleeClip;

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
        defenseReductionMultiplier
    }

    // PRIVATE ------
    private Character character;
    private CharacterMelee melee;
    private NetworkVariable<CHARACTER_STATUS> characterStatusNetworked = new NetworkVariable<CHARACTER_STATUS>();


    // Lifecycle hooks ------
    private void Awake() {}

    void Update() {}
    // EVENT TRIGGERS ------
    public class StatusUpdateEvent : UnityEvent<CHARACTER_STATUS> { }
    public StatusUpdateEvent onStatusEvent = new StatusUpdateEvent();

    private void OnStatusChange(CHARACTER_STATUS prev, CHARACTER_STATUS current)
    {
        if (IsServer) { return; }

        StartCoroutine(ExcecuteStatusChange(current));
    }

    public override void OnNetworkSpawn()
    {
        this.character = this.GetComponent<Character>();
        this.melee = this.GetComponent<CharacterMelee>();

        characterStatusNetworked.OnValueChanged += OnStatusChange;
    }

    public override void OnNetworkDespawn()
    {
        characterStatusNetworked.OnValueChanged -= OnStatusChange;
    }

    private IEnumerator ExcecuteStatusChange(CHARACTER_STATUS current)
    {
        yield return null;

        switch (current)
        {
            case CHARACTER_STATUS.damageMultiplier:
                break;
            case CHARACTER_STATUS.damageReductionMultiplier:
                break;
            case CHARACTER_STATUS.defenseIncreaseMultiplier:
                break;
            case CHARACTER_STATUS.defenseReductionMultiplier:
                break;
        }
    }

    public void UpdateStatus(CHARACTER_STATUS status)
    {
        StartCoroutine(UpdateStatusCoroutine(status));
    }

    private IEnumerator UpdateStatusCoroutine(CHARACTER_STATUS status)
    {

        if (this.character.IsDead() & this.character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.Dead)
        {
            yield break;
        }

        CHARACTER_STATUS prevStatus = this.character.characterStatus;

        switch (status)
        {
            // All Ailments should end with reset except Stun which can be cancelled
            case CHARACTER_STATUS.damageMultiplier:
                break;
            case CHARACTER_STATUS.damageReductionMultiplier:
                break;
            case CHARACTER_STATUS.defenseIncreaseMultiplier:
                break;
            case CHARACTER_STATUS.defenseReductionMultiplier:
                break;
        }

        this.character.Status(status);
        if (IsServer) { characterStatusNetworked.Value = status; }
        onStatusEvent.Invoke(status);
    }

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