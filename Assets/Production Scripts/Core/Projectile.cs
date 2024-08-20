using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using Vi.Core.VFX;
using Vi.Core.CombatAgents;
using Unity.Netcode.Components;

namespace Vi.Core
{
    [RequireComponent(typeof(PooledObject))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public class Projectile : NetworkBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private int killDistance = 500;
        [SerializeField] private PooledObject[] VFXToPlayOnDestroy;
        [SerializeField] private AudioClip[] soundToPlayOnSpawn = new AudioClip[0];
        [SerializeField] private AudioClip[] whooshNearbySound = new AudioClip[0];
        [SerializeField] private AudioClip[] soundToPlayOnDespawn = new AudioClip[0];

        public CombatAgent GetAttacker() { return attacker; }

        private CombatAgent attacker;
        private ShooterWeapon shooterWeapon;
        private ActionClip attack;
        private Vector3 projectileForce;
        private float damageMultiplier;
        private Quaternion originalRotation;
        private bool initialized;

        public void Initialize(CombatAgent attacker, ShooterWeapon shooterWeapon, ActionClip attack, Vector3 projectileForce, float damageMultiplier)
        {
            if (!IsServer) { Debug.LogError("Projectile.Initialize() should only be called on the server!"); return; }
            if (initialized) { Debug.LogError("Projectile.Initialize() already called, why are you calling it again idiot?"); return; }

            this.attacker = attacker;
            this.shooterWeapon = shooterWeapon;
            this.attack = attack;
            this.projectileForce = projectileForce;
            this.damageMultiplier = damageMultiplier;
            originalRotation = transform.rotation;
            initialized = true;

            rb.AddForce(transform.rotation * projectileForce, ForceMode.VelocityChange);
        }

        private Rigidbody rb;
        private NetworkTransform networkTransform;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            networkTransform = GetComponent<NetworkTransform>();
        }

        private Vector3 startPosition;
        private PooledObject pooledObject;
        private void OnEnable()
        {
            startPosition = transform.position;

            if (!pooledObject) { pooledObject = GetComponent<PooledObject>(); }

            if (pooledObject.IsPrewarmObject()) { return; }

            if (soundToPlayOnSpawn.Length > 0)
            {
                AudioSource audioSource = AudioManager.Singleton.PlayClipAtPoint(PlayerDataManager.Singleton.gameObject, soundToPlayOnSpawn[Random.Range(0, soundToPlayOnSpawn.Length)], transform.position, Weapon.attackSoundEffectVolume);
                audioSource.maxDistance = Weapon.attackSoundEffectMaxDistance;
            }
        }

        public override void OnNetworkSpawn()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) { Debug.LogError("No collider attached to: " + this); }
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger) { Debug.LogError("Make sure all colliders on projectiles are triggers! " + this); }
                col.enabled = IsServer;
            }

            if (gameObject.layer != LayerMask.NameToLayer("Projectile")) { Debug.LogError("Make sure projectiles are in the Projectile Layer!"); }
        }

        private bool nearbyWhooshPlayed;
        private void Update()
        {
            if (IsClient)
            {
                if (whooshNearbySound.Length > 0)
                {
                    if (!nearbyWhooshPlayed)
                    {
                        KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                        if (localPlayerKvp.Value)
                        {
                            if (Vector3.Distance(localPlayerKvp.Value.transform.position, transform.position) < Weapon.projectileNearbyWhooshDistanceThreshold)
                            {
                                AudioSource audioSource = AudioManager.Singleton.PlayClipOnTransform(transform, whooshNearbySound[Random.Range(0, whooshNearbySound.Length)], false, Weapon.projectileNearbyWhooshVolume);
                                audioSource.maxDistance = 20;
                                nearbyWhooshPlayed = true;
                            }
                        }
                    }
                }
            }

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

        private void FixedUpdate()
        {
            if (!initialized) { return; }
            if (!IsServer) { return; }
            transform.rotation = rb.velocity == Vector3.zero ? originalRotation : Quaternion.LookRotation(rb.velocity);
        }

        [HideInInspector] public bool canHitPlayers = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized) { return; }
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (!canHitPlayers) { return; }

            bool shouldDestroy = false;
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent == attacker) { return; }
                bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(attacker, shooterWeapon, shooterWeapon.GetHitCounter(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce * 5, damageMultiplier);
                if (!hitSuccess & networkCollider.CombatAgent.GetAilment() == ActionClip.Ailment.Knockdown) { return; }
            }
            else if (other.transform.root.TryGetComponent(out GameInteractiveActionVFX actionVFX))
            {
                shouldDestroy = actionVFX.ShouldBlockProjectiles();
                actionVFX.ProcessProjectileHit(attacker, shooterWeapon, shooterWeapon.GetHitCounter(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce * 5, damageMultiplier);
            }
            else if (other.transform.root.TryGetComponent(out GameItem gameItem))
            {
                shouldDestroy = true;
                gameItem.ProcessProjectileHit(attacker, shooterWeapon, shooterWeapon.GetHitCounter(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce * 5, damageMultiplier);
            }
            else
            {
                // Dont despawn projectiles that come from the same attacker
                if (other.transform.root.TryGetComponent(out Projectile otherProjectile))
                {
                    if (otherProjectile.attacker == attacker) { return; }
                }
            }

            if (!other.isTrigger | shouldDestroy) { NetworkObject.Despawn(true); }
        }

        private void OnDisable()
        {
            attacker = null;
            shooterWeapon = null;
            attack = null;
            projectileForce = default;
            damageMultiplier = default;
            originalRotation = default;
            initialized = false;

            rb.velocity = Vector3.zero;

            nearbyWhooshPlayed = false;

            if (pooledObject.IsPrewarmObject()) { return; }

            foreach (PooledObject prefab in VFXToPlayOnDestroy)
            {
                if (prefab.GetComponent<FollowUpVFX>())
                {
                    NetworkObject netObj = Instantiate(prefab, transform.position, transform.rotation).GetComponent<NetworkObject>();
                    netObj.Spawn(true);
                    netObj.GetComponent<FollowUpVFX>().InitializeVFX(attacker, attack);
                }
                else
                {
                    PooledObject obj = ObjectPoolingManager.SpawnObject(prefab, transform.position, transform.rotation);
                    PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(obj));
                }
            }

            if (soundToPlayOnDespawn.Length > 0)
            {
                AudioSource audioSource = AudioManager.Singleton.PlayClipAtPoint(PlayerDataManager.Singleton.gameObject, soundToPlayOnDespawn[Random.Range(0, soundToPlayOnDespawn.Length)], transform.position, Weapon.attackSoundEffectVolume);
                audioSource.maxDistance = Weapon.attackSoundEffectMaxDistance;
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