using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameCreator.Melee
{
    public abstract class Projectile : NetworkBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private int killDistance = 500;

        protected CharacterMelee attacker;
        protected MeleeClip attack;
        protected Vector3 projectileForce;
        protected bool initialized;

        public void Initialize(CharacterMelee attacker, MeleeClip attack, Vector3 projectileForce)
        {
            if (this.attacker) { Debug.LogError("BulletProjectile.Initialize() already called, why are you calling it again idiot?"); return; }

            this.attacker = attacker;
            this.attack = attack;
            this.projectileForce = projectileForce;
            initialized = true;
        }

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

            if (Vector3.Distance(transform.position, startPosition) > killDistance)
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