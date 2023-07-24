using UnityEngine;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using Unity.Netcode;

public class AbilityManager : NetworkBehaviour
{
    public List<Ability> abilities = new List<Ability>();

    private readonly List<KeyCode> _hotKeys = new List<KeyCode>()
    {
        KeyCode.Q,
        KeyCode.E,
        KeyCode.R,
        KeyCode.F,
    };

    private CharacterMelee melee;
    private NetworkList<bool> abilitiesOnCooldown;

    public bool IsAbilityOnCooldown(Ability ability)
    {
        return abilitiesOnCooldown[abilities.IndexOf(ability)];
    }

    private void Awake()
    {
        abilitiesOnCooldown = new NetworkList<bool>();

        melee = GetComponentInParent<CharacterMelee>();

        foreach (Ability ability in abilities)
        {
            ability.ResetAbility();
        }
    }

    void Update()
    {
        if (IsServer)
        {
            // Update cooldown status over the network
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilitiesOnCooldown.Count < i + 1)
                {
                    abilitiesOnCooldown.Add(abilities[i].isOnCoolDownLocally);
                }
                else
                {
                    abilitiesOnCooldown[i] = abilities[i].isOnCoolDownLocally;
                }
            }
        }

        if (OwnerClientId == 1)
        {
            string debug = "";
            for (int i = 0; i < abilities.Count; i++)
            {
                debug += abilities[i].name + " " + abilitiesOnCooldown[i] + " - ";
            }
            Debug.Log(debug);
        }
        
        if (!IsOwner) return;
        if (abilities.Count <= 0) return;
        if (!Input.anyKeyDown) return;
        if (melee == null) return;
        if (melee.IsBlocking) return;
        if (melee.IsStaggered) return;
        if (melee.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) return;

        foreach (KeyCode key in _hotKeys)
        {
            if (Input.GetKeyDown(key)) { ActivateAbilityServerRpc(key); }
        }
    }

    [ServerRpc]
    private void ActivateAbilityServerRpc(KeyCode key)
    {
        Ability ability = abilities.Find(ablty => ablty.skillKey == key);

        // Don't activate while dashing
        if (melee.Character.isCharacterDashing() && ability.abilityType != Ability.AbilityType.DashAttack) return;
        // Don't activate if ability can't cancel attack animation
        if (melee.IsAttacking && !ability.canAnimCancel) { return; }
        // Don't activate if ability is on cooldown
        if (ability.isOnCoolDownLocally == true) { return; }
        // Don't activate if poise is not high enough
        if (ability && melee.GetPoise() < ability.staminaCost) { return; }

        if (ability != null && melee != null)
        {
            melee.AddPoise(-1 * ability.staminaCost);
            ability.ExecuteAbility(melee, key);
        }
    }
}
