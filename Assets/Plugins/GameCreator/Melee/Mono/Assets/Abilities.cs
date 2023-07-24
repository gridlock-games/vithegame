using UnityEngine;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using Unity.Netcode;

public class Abilities : NetworkBehaviour
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
    private CharacterMeleeUI meleeUI;

    // Start is called before the first frame update
    void Start()
    {
        melee = GetComponentInParent<CharacterMelee>();
        meleeUI = melee.GetComponentInChildren<CharacterMeleeUI>();

        foreach (Ability ability in abilities)
        {
            ability.ResetAbility();
        }
    }

    // Update is called once per frame
    void Update()
    {
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
        var ability = this.abilities.Find(ablty => ablty.skillKey == key);



        float PoiseValue = melee.GetPoise();

        if(melee.Character.isCharacterDashing() && ability.abilityType != Ability.AbilityType.DashAttack) {
            Debug.Log("Cannot activate ability during Dash: " + ability.skillKey);
            return;
        }

        if (melee.IsAttacking && !ability.canAnimCancel) {
            Debug.Log("Animation cancel not available for: " + ability.skillKey);
            return;
        }

        if (ability.isOnCoolDown == true) {
            Debug.Log("Ability is still in cooldown: " + ability.skillKey);
            return;
        }

        if (ability && PoiseValue < ability.staminaCost) {
            Debug.Log("Not enough poise for: " + ability.skillKey);
            return;
        }

        if (ability != null && melee != null)
        {
            Debug.Log("Executing ability: " + ability.skillKey);
            Debug.Log("IsAttacking: " + melee.IsAttacking);
            melee.AddPoise(-1 * ability.staminaCost);
            ability.ExecuteAbility(melee, key);
        }
    }
}
