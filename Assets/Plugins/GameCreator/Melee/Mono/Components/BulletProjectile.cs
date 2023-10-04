using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace GameCreator.Melee
{
    [RequireComponent(typeof(Rigidbody))]
    public class BulletProjectile : Projectile
    {
        [SerializeField] private float healTeammateAmount;

        public override void OnNetworkSpawn()
        {
            if (IsServer) { GetComponent<Rigidbody>().AddForce(transform.forward * projectileSpeed, ForceMode.VelocityChange); }
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
                attacker.ProcessProjectileHit(attacker, otherMelee, other.ClosestPointOnBounds(transform.position), attack, healTeammateAmount);
            }
            NetworkObject.Despawn(true);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5);
        }
    }
}