using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace GameCreator.Melee
{
    public class Projectile : NetworkBehaviour
    {
        private CharacterMelee attacker;
        private MeleeClip meleeClip;
        private bool initialized;

        public void Initialize(CharacterMelee attacker, MeleeClip meleeClip)
        {
            this.attacker = attacker;
            this.meleeClip = meleeClip;
            initialized = true;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) { GetComponent<Rigidbody>().AddForce(transform.rotation * new Vector3(0, 0, 5), ForceMode.VelocityChange); }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized) { return; }
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            CharacterMelee otherMelee = other.GetComponentInParent<CharacterMelee>();
            if (otherMelee == attacker) { return; }

            if (otherMelee)
            {
                attacker.ProcessProjectileHit(attacker, otherMelee, other.ClosestPointOnBounds(transform.position), meleeClip);
            }
            NetworkObject.Despawn(true);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.DrawLine(transform.position, transform.forward * 5);
        }
    }
}