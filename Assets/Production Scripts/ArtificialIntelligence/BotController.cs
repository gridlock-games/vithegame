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

        private void Update()
        {
            if (lightAttack)
            {
                SendMessage("OnLightAttack");
            }
        }
    }
}