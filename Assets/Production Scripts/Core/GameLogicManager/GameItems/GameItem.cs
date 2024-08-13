using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.Core.GameModeManagers
{
    [RequireComponent(typeof(PooledObject))]
    public class GameItem : NetworkBehaviour
    {
        protected const float gameItemVolume = 1;

        public virtual void OnHit(CombatAgent attacker)
        {
            
        }
    }
}