namespace GameCreator.Melee
{
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


    [RequireComponent(typeof(Character))]
    [AddComponentMenu("Game Creator/Character Status Manager")]
    public class CharacterStatusManager : NetworkBehaviour {
        public enum CHARACTER_STATUS {
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

        private IEnumerator ExcecuteStatusChange(CHARACTER_STATUS current) {
            yield return null;


            switch(current)
            {

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

    }
}