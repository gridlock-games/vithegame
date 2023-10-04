using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameCreator.Melee
{
    public class Projectile : NetworkBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private int destroyDistance = 500;

        protected CharacterMelee attacker;
        protected MeleeClip attack;

        public CharacterMelee GetAttacker()
        {
            return attacker;
        }

        private Vector3 startPosition;
        protected void Start()
        {
            startPosition = transform.position;
        }

        protected void Update()
        {
            if (!IsServer) { return; }

            if (Vector3.Distance(transform.position, startPosition) > destroyDistance)
            {
                if (IsSpawned)
                {
                    NetworkObject.Despawn(true);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}