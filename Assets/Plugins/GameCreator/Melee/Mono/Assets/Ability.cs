using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
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
    }

    public Sprite skillImageFill;
    public WeaponType weaponBind = WeaponType.BRAWLER;
    public MeleeClip meleeClip;
    public AbilityType abilityType = AbilityType.Active;
    public float coolDown = 0.00f;
    public float staminaCost = 0.00f;
    public KeyCode skillKey = KeyCode.Space;

    public bool isOnCoolDown { get; private set; }

    public void ResetAbility()
    {
        isOnCoolDown = false;
    }

    public void ExecuteAbility(CharacterMelee melee)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (this.abilityType != AbilityType.Passive)
        {
            if (meleeClip == null) return;
            // Invoke in CharacterMelee
            isOnCoolDown = true;
            melee.ExecuteAbility(meleeClip, CharacterMelee.ActionKey.A);
            melee.StartCoroutine(WaitForAbilityCooldown());
        }
    }

    private IEnumerator WaitForAbilityCooldown()
    {
        yield return new WaitForSeconds(coolDown);

        isOnCoolDown = false;
    }
}
