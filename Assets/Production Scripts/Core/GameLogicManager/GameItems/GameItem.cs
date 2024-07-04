using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core.GameModeManagers
{
    public class GameItem : NetworkBehaviour
    {
        protected const float gameItemVolume = 1;

        public virtual void OnHit(Attributes attacker)
        {
            
        }
    }
}