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

    public Sprite skillImageFill;
    public WeaponType weaponBind = WeaponType.BRAWLER;
    public MeleeClip meleeClip;
    public AbilityType abilityType = AbilityType.Active;
    public float coolDown = 0.00f;
    public float staminaCost = 0.00f;
    public KeyCode skillKey = KeyCode.Space;
    public bool canAnimCancel = false;

    public bool isOnCoolDown { get; private set; }

    CharacterAnimator animator;

    public void ResetAbility()
    {
        isOnCoolDown = false;
    }


    // We're only using this for UI cooldowns for now
    // TO DO: Move ability invoke to this
    public void ExecuteAbility(CharacterMelee melee, KeyCode key)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        this.animator = melee.Character.GetCharacterAnimator();

        if (this.abilityType != AbilityType.Passive)
        {
            if (meleeClip == null) return;
            isOnCoolDown = true;

            this.InvokeSkill(melee, key);
            
            // Disabling Ability invoke in CharacterMelee
            // melee.ExecuteAbility(meleeClip, CharacterMelee.ActionKey.A);
            melee.StartCoroutine(WaitForAbilityCooldown());
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
        }

        if(hasValidkey == false) return;

        if(melee.IsAttacking && canAnimCancel) {
            melee.StopAttack();
            melee.currentMeleeClip = null;
            animator.StopGesture(0.15f);
        }

        melee.Execute(actionKey);
    }

    private IEnumerator WaitForAbilityCooldown()
    {
        yield return new WaitForSeconds(coolDown);

        isOnCoolDown = false;
    }
}
