using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using GameCreator.Core;
using Unity.Netcode;

public class Ability : MonoBehaviour
{
    public enum AbilityType
    {
        Active,
        Passive,
        TeamBuff,
        TeamDebuff,
        SelfBuff,
        Debuff,
        DashAttack
    }

    public enum AnimCancellingType {
        None,
        Cancel_NormalAtk,
        Cancel_AbilityAtk,
        Cancel_HeavyAtk,
        Cancel_Dodge
    }

    public enum DodgeLockOnPhase {
        None,
        Activate,
        Recovery
    }

    public Sprite skillImageFill;
    public WeaponType weaponBind = WeaponType.BRAWLER;
    public MeleeClip meleeClip;
    public AbilityType abilityType = AbilityType.Active;
    public DodgeLockOnPhase dodgeLockOnhase = DodgeLockOnPhase.None;
    public float coolDown = 0.00f;
    public float staminaCost = 0.00f;
    public float hpCost = 0.00f;
    public KeyCode skillKey = KeyCode.Space;

    public AnimCancellingType canCncelAnimationType = AnimCancellingType.Cancel_NormalAtk;
    public bool canCancelAnimation = false;

    public IActionsList actionsOnExecute;
    public IActionsList actionOnActivate;
    public IActionsList actionsOnHit;

    public bool isOnCoolDownLocally { get; private set; }

    public void ResetAbility()
    {
        isOnCoolDownLocally = false;
    }

    public void ExecuteActionsOnStart(Vector3 position, GameObject target)
    {
        if (this.actionsOnExecute)
        {
            GameObject actionsInstance = Instantiate<GameObject>(
                this.actionsOnExecute.gameObject,
                position,
                Quaternion.identity
            );

            actionsInstance.hideFlags = HideFlags.HideInHierarchy;
            Actions actions = actionsInstance.GetComponent<Actions>();

            if (!actions) return;
            actions.Execute(target, null);
        }
    }

    public void ExecuteActionsOnActivate(Vector3 position, GameObject target)
        {
            if (this.actionOnActivate)
            {
                GameObject actionsInstance = Instantiate<GameObject>(
                    this.actionOnActivate.gameObject,
                    position,
                    Quaternion.identity
                );

                actionsInstance.hideFlags = HideFlags.HideInHierarchy;
                Actions actions = actionsInstance.GetComponent<Actions>();

                if (!actions) return;
                actions.Execute(target, null);
            }
        }

    // We're only using this for UI cooldowns for now
    // TO DO: Move ability invoke to this
    public void ExecuteAbility(CharacterMelee melee, KeyCode key)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (abilityType != AbilityType.Passive)
        {
            if (meleeClip == null) return;
            isOnCoolDownLocally = true;

            InvokeSkill(melee, key);

            // Disabling Ability invoke in CharacterMelee
            melee.StartCoroutine(WaitForAbilityCooldown());
            this.ExecuteActionsOnStart(melee.transform.position, melee.gameObject);
        }
    }

    private void InvokeSkill(CharacterMelee melee, KeyCode key)
    {
        CharacterMelee.ActionKey actionKey = CharacterMelee.ActionKey.A;
        bool hasValidkey = false;
        switch (key)
        {
            case KeyCode.Q:
                actionKey = CharacterMelee.ActionKey.C;
                hasValidkey = true;
                break;
            case KeyCode.E:
                actionKey = CharacterMelee.ActionKey.D;
                hasValidkey = true;
                break;
            case KeyCode.R:
                actionKey = CharacterMelee.ActionKey.E;
                hasValidkey = true;
                break;
            case KeyCode.T:
                actionKey = CharacterMelee.ActionKey.F;
                hasValidkey = true;
                break;
        }

        if (hasValidkey == false) return;

        if (melee.IsAttacking)
        {
            melee.StopAttack();
            melee.currentMeleeClip = null;
            melee.Character.GetCharacterAnimator().StopGesture(0.15f);
        }

        melee.ExecuteAbility(actionKey);
    }

    private IEnumerator WaitForAbilityCooldown()
    {
        yield return new WaitForSeconds(coolDown);

        isOnCoolDownLocally = false;
    }
}
