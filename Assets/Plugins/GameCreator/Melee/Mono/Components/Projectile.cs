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
        protected float projectileSpeed;
        protected bool initialized;

        public void Initialize(CharacterMelee attacker, MeleeClip attack, float projectileSpeed)
        {
            if (this.attacker) { Debug.LogError("BulletProjectile.Initialize() already called, why are you calling it again idiot?"); return; }

            this.attacker = attacker;
            this.attack = attack;
            this.projectileSpeed = projectileSpeed;
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