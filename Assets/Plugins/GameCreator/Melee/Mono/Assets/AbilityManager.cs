using UnityEngine;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using Unity.Netcode;

public class AbilityManager : NetworkBehaviour
{
    [SerializeField] private List<Ability> abilityPrefabs = new List<Ability>();

    private readonly List<KeyCode> _hotKeys = new List<KeyCode>()
    {
        KeyCode.Q,
        KeyCode.E,
        KeyCode.R,
        KeyCode.F,
    };

    private CharacterMelee melee;
    private List<Ability> abilityInstances = new List<Ability>();

    private Ability activatedAbility;

    private NetworkList<bool> abilitiesOnCooldown;

    public List<Ability> GetAbilityInstanceList()
    {
        return abilityInstances;
    }

    public bool IsAbilityOnCooldown(Ability ability)
    {
        return abilitiesOnCooldown[abilityInstances.IndexOf(ability)];
    }

    private void Awake()
    {
        abilitiesOnCooldown = new NetworkList<bool>();

        melee = GetComponentInParent<CharacterMelee>();

        List<GameObject> abilityObjects = new List<GameObject>();
        foreach (Ability ability in abilityPrefabs)
        {
            abilityObjects.Add(Instantiate(ability.gameObject, transform));
        }

        foreach (GameObject abilityInstance in abilityObjects)
        {
            Ability ability = abilityInstance.GetComponent<Ability>();
            ability.ResetAbility();
            abilityInstances.Add(ability);
        }
    }

    void Update()
    {
        if (IsServer)
        {
            // Update cooldown status over the network
            for (int i = 0; i < abilityInstances.Count; i++)
            {
                if (abilitiesOnCooldown.Count < i + 1)
                {
                    abilitiesOnCooldown.Add(abilityInstances[i].isOnCoolDownLocally);
                }
                else
                {
                    abilitiesOnCooldown[i] = abilityInstances[i].isOnCoolDownLocally;
                }
            }
        }

        if (!IsOwner) return;
        if (abilityInstances.Count <= 0) return;
        if (!Input.anyKeyDown) return;
        if (melee == null) return;
        if (melee.IsBlocking) return;
        if (melee.IsStaggered) return;
        if (melee.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) return;

        foreach (KeyCode key in _hotKeys)
        {
            if (Input.GetKeyDown(key)) { 
                ActivateAbilityServerRpc(key);
            }
        }
    }

    [ServerRpc]
    private void ActivateAbilityServerRpc(KeyCode key)
    {
        Ability ability = abilityInstances.Find(ablty => ablty.skillKey == key);

        // Don't activate while dashing
        if (melee.Character.isCharacterDashing() && ability.abilityType != Ability.AbilityType.DashAttack) return;
        // Don't activate if ability can't cancel attack animation
        if (ability && melee.IsAttacking && !ability.canAnimCancel) { return; }
        // Don't activate if an ability is already casting
        if (ability && ability.canAnimCancel) { if( melee.isCastingAbility ) { return; } }
        // Don't activate if an existing ability is active and requires player to commit
        if (ability && melee.isCastingAbility  && activatedAbility.hasAnimCommit) { return; }
        // Don't activate if ability is on cooldown
        if (ability.isOnCoolDownLocally == true) { return; }
        // Don't activate if poise is not high enough
        if (ability && melee.GetPoise() < ability.staminaCost) { return; }
        

        if (ability != null && melee != null)
        {
            activatedAbility = ability;
            melee.AddPoise(-1 * activatedAbility.staminaCost);
            activatedAbility.ExecuteAbility(melee, key);
        }
    }
}
