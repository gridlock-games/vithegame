using UnityEngine;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using Unity.Netcode;
using LightPat.Core;

public class AbilityManager : NetworkBehaviour
{
    [SerializeField] private List<Ability> abilityPrefabs = new List<Ability>();

    private readonly List<KeyCode> _hotKeys = new List<KeyCode>()
    {
        KeyCode.Q,
        KeyCode.E,
        KeyCode.R,
        KeyCode.F,
        KeyCode.T
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
        int abilityIndex = abilityInstances.IndexOf(ability);
        if (abilityIndex == -1 | abilitiesOnCooldown.Count == 0) { return true; }

        return abilitiesOnCooldown[abilityIndex];
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
            if (Input.GetKeyDown(key))
            {
                ActivateAbilityServerRpc(key);
            }
        }
    }
    
    [ServerRpc]
    private void ActivateAbilityServerRpc(KeyCode key)
    {
        melee.silenced.Value = Time.time < melee.silenceEndTime;
        if (melee.silenced.Value) { return; }

        Ability ability = abilityInstances.Find(ablty => ablty.skillKey == key);
        if (!ability) { return; }

        // Don't activate while dashing
        if (melee.Character.isCharacterDashing() && ability.abilityType != Ability.AbilityType.DashAttack) return;
        // Don't activate if ability is on cooldown
        if (ability.isOnCoolDownLocally == true) { return; }
        // Don't activate if poise is not high enough
        if (ability && ability.staminaCost > 0f && melee.GetPoise() < ability.staminaCost) { return; }
        // Don't activate if HP is not high enough
        if (ability && ability.hpCost > 0f && melee.GetHP() < ability.hpCost) { return; }
        // Don't activate if Rage is not high enough
        if (ability && ability.rageCost > 0f && melee.GetRage() < ability.rageCost) { return; }
        // Don't activate if Melee is attacking and cancelType is none
        if (ability && melee.IsAttacking && ability.canCncelAnimationType == Ability.AnimCancellingType.None) { return; }
        // Don't activate if Melee is currently playing a Heavy Attack
        if (ability && melee.IsAttacking && melee.currentMeleeClip.isHeavy && ability.canCncelAnimationType != Ability.AnimCancellingType.Cancel_HeavyAtk) { return; }
        // Don't activate if Melee is currently playing a previous abiity and ability is not allowed to cancel previous Ability
        if (ability && melee.IsCastingAbility.Value && ability.canCncelAnimationType != Ability.AnimCancellingType.Cancel_AbilityAtk) { return; }

        if (ability.meleeClip.attackType == MeleeClip.AttackType.Grab)
        {
            float raycastDistance = ability.meleeClip.grabDistance;
            bool bHit = false;
            RaycastHit[] allHits = Physics.RaycastAll(transform.position + Vector3.up, transform.forward, raycastDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            Debug.DrawRay(transform.position + Vector3.up, transform.forward * raycastDistance, Color.blue, 2);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform) { continue; }
                CharacterMelee targetMelee = hit.transform.GetComponentInParent<CharacterMelee>();
                if (!targetMelee) { return; }
                if (targetMelee == melee) { return; }

                bHit = true;

                if (ClientManager.Singleton)
                {
                    if (ClientManager.Singleton.GetClientDataDictionary().ContainsKey(melee.OwnerClientId) & ClientManager.Singleton.GetClientDataDictionary().ContainsKey(targetMelee.OwnerClientId))
                    {
                        Team attackerMeleeTeam = melee.NetworkObject.IsPlayerObject ? ClientManager.Singleton.GetClient(melee.OwnerClientId).team : Team.Environment;
                        Team targetMeleeTeam = targetMelee.NetworkObject.IsPlayerObject ? ClientManager.Singleton.GetClient(targetMelee.OwnerClientId).team : Team.Environment;

                        if (attackerMeleeTeam != Team.Competitor | targetMeleeTeam != Team.Competitor)
                        {
                            // If the attacker's team is the same as the victim's team, do not register this hit
                            if (attackerMeleeTeam == targetMeleeTeam) { return; }
                        }
                    }
                }

                targetMelee.Character.Grab(melee.Character, ability.meleeClip.grabDuration);
                break;
            }

            // Make sure that there is a detected target
            if (!bHit) { return; }
        }
        
        if (ability != null && melee != null)
        {
            activatedAbility = ability;
            melee.RevertAbilityCastingStatus();
            melee.AddPoise(-1 * activatedAbility.staminaCost);
            melee.AddHP(-1 * activatedAbility.hpCost);
            melee.AddRage(-1 * activatedAbility.rageCost);
            activatedAbility.ExecuteAbility(melee, key);
        }
    }

    public Ability GetActivatedAbility()
    {
        return activatedAbility;
    }
}
