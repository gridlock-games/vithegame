using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Melee;
using GameCreator.Core;
using Unity.Netcode;
public class Abilities : MonoBehaviour
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
    }

    // Update is called once per frame
    void Update()
    {
        if(melee == null) return;
        if (!Input.anyKeyDown) return;
        if (melee.IsBlocking) return;
        if (melee.IsStaggered) return;
        if (!NetworkManager.Singleton.IsServer) return;
        if (abilities.Count <= 0) return;

        foreach (KeyCode key in _hotKeys) {
            if (Input.GetKey(key)) {
                var ability = this.abilities.Find(ablty => ablty.skillKey == key);

                float PoiseValue = melee.GetPoise();

                if(ability.isOnCoolDown == true) return;

                if(ability && PoiseValue >= ability.staminaCost) {
                    melee.AddPoise(-1 * ability.staminaCost);
                    ability.ExecuteSkill(melee);
                    ability.isOnCoolDown = true;
                } else {
                    return;
                }
            }
        }
    }

    private void DisableSkillSlot(Ability ability) {
        switch(ability.skillKey) {
        case KeyCode.Q:
            meleeUI.abilityAImageFill.sprite = null;
            break;
        case KeyCode.E:
            meleeUI.abilityBImageFill.sprite = null;
            break;
        case KeyCode.R:
            meleeUI.abilityCImageFill.sprite = null;
            break;
        }
    }
}
