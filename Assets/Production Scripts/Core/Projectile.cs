using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : NetworkBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private int killDistance = 500;
        [SerializeField] private GameObject[] VFXToPlayOnDestroy;
        [SerializeField] private AudioClip soundToPlayOnSpawn;

        private Attributes attacker;
        private ShooterWeapon shooterWeapon;
        private ActionClip attack;
        private Vector3 projectileForce;
        private float damageMultiplier;
        private bool initialized;

        public void Initialize(Attributes attacker, ShooterWeapon shooterWeapon, ActionClip attack, Vector3 projectileForce, float damageMultiplier)
        {
            if (!IsServer) { Debug.LogError("Projectile.Initialize() should only be called on the server!"); return; }
            if (initialized) { Debug.LogError("Projectile.Initialize() already called, why are you calling it again idiot?"); return; }

            this.attacker = attacker;
            this.shooterWeapon = shooterWeapon;
            this.attack = attack;
            this.projectileForce = projectileForce;
            this.damageMultiplier = damageMultiplier;
            initialized = true;

            GetComponent<Rigidbody>().AddForce(transform.rotation * projectileForce, ForceMode.VelocityChange);
        }

        private Vector3 startPosition;
        private void Start()
        {
            AudioManager.Singleton.PlayClipAtPoint(PlayerDataManager.Singleton.gameObject, soundToPlayOnSpawn, transform.position);

            startPosition = transform.position;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) { Debug.LogError("No collider attached to: " + this); }
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger) { Debug.LogError("Make sure all colliders on projectiles are triggers! " + this); }
            }

            if (gameObject.layer != LayerMask.NameToLayer("Projectile")) { Debug.LogError("Make sure projectiles are in the Projectile Layer!"); }
        }

        private void Update()
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

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized) { return; }
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (other.isTrigger) { return; }

            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.Attributes == attacker) { return; }
                networkCollider.Attributes.ProcessProjectileHit(attacker, shooterWeapon, shooterWeapon.GetHitCounter(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce, damageMultiplier);
            }
            else
            {
                // Dont despawn projectiles that come from the same attacker
                Projectile otherProjectile = other.GetComponentInParent<Projectile>();
                if (otherProjectile)
                {
                    if (otherProjectile.attacker == attacker) { return; }
                }
            }
            NetworkObject.Despawn(true);
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            foreach (GameObject prefab in VFXToPlayOnDestroy)
            {
                GameObject g = ObjectPoolingManager.SpawnObject(prefab, transform.position, transform.rotation);
                if (g.TryGetComponent(out FollowUpVFX vfx)) { vfx.Initialize(attacker, attack); }
                PlayerDataManager.Singleton.StartCoroutine(WeaponHandler.ReturnVFXToPoolWhenFinishedPlaying(g));
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) { return; }
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.DrawLine(transform.position, transform.position + transform.rotation * projectileForce);
        }
    }
}