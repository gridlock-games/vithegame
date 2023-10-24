using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class BotController : NetworkBehaviour
    {
        [SerializeField] private bool lightAttack;
        [SerializeField] private bool isBlocking;
        [SerializeField] private bool attackPlayer;

        private CharacterController characterController;
        private AnimationHandler animationHandler;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
        }

        private void Update()
        {
            characterController.Move(animationHandler.ApplyLocalRootMotion());

            if (lightAttack)
            {
                SendMessage("OnLightAttack");
            }
        }
    }
}