using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using Unity.Netcode;

public class VoiceActorManager : NetworkBehaviour
{
    [SerializeField] private List<AudioClip> attackVO = new List<AudioClip>();
    [SerializeField] private List<AudioClip> damageVO = new List<AudioClip>();
    [SerializeField] private List<AudioClip> deathVO = new List<AudioClip>();
    
    private CharacterMelee melee;

    private void Awake() {
        melee = GetComponentInParent<CharacterMelee>();
    }

    public AudioClip GetAttackVO() {
        var index = UnityEngine.Random.Range(0, this.attackVO.Count - 1);
        return this.attackVO[index];
    }
}
