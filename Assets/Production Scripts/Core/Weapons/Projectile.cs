using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;
using Vi.Core.VFX;
using Vi.Core.CombatAgents;
using Unity.Netcode.Components;
using Vi.Core.GameModeManagers;

namespace Vi.Core.Weapons
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
        private Renderer[] renderers;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        private Vector3 startPosition;
        private PooledObject pooledObject;
        private void OnEnable()
        {
            startPosition = transform.position;

            foreach (Renderer renderer in renderers)
            {
                renderer.forceRenderingOff = true;
            }

            if (!pooledObject) { pooledObject = GetComponent<PooledObject>(); }

            NetworkPhysicsSimulation.AddRigidbody(rb);

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

            rb.interpolation = IsClient ? RigidbodyInterpolation.Extrapolate : RigidbodyInterpolation.None;
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;

            StartCoroutine(ShowRenderers());
        }

        private IEnumerator ShowRenderers()
        {
            yield return new WaitForFixedUpdate();
            foreach (Renderer renderer in renderers)
            {
                renderer.forceRenderingOff = false;
            }
        }

        private bool nearbyWhooshPlayed;
        private void Update()
        {
            if (!IsSpawned) { return; }

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

            if (Vector3.Distance(transform.position, startPosition) > killDistance | (GameModeManager.Singleton.ShouldDisplayNextGameAction() & GameModeManager.Singleton.DespawnProjectilesInBetweenRounds))
            {
                if (IsSpawned)
                {
                    NetworkObject.Despawn(true);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!initialized) { return; }
            if (!IsServer) { return; }
            rb.MoveRotation(rb.linearVelocity == Vector3.zero ? originalRotation : Quaternion.LookRotation(rb.linearVelocity));
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
                if (other.isTrigger) { return; }
                if (networkCollider.CombatAgent == attacker) { return; }

                Vector3 hitSourcePos = Vector3.Distance(attacker.MovementHandler.GetPosition(), networkCollider.MovementHandler.GetPosition()) > 1 ? (transform.position - transform.rotation * projectileForce * 5) : attacker.MovementHandler.GetPosition();

                bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(attacker, shooterWeapon, shooterWeapon.GetHitCounter(), attack,
                    other.ClosestPointOnBounds(transform.position), hitSourcePos, damageMultiplier);
                if (!hitSuccess) { return; }
            }
            else if (other.transform.root.TryGetComponent(out IHittable hittable))
            {
                if (other.transform.root == attacker) { return; }

                Vector3 hitSourcePos = Vector3.Distance(attacker.MovementHandler.GetPosition(), other.transform.position) > 1 ? (transform.position - transform.rotation * projectileForce * 5) : attacker.MovementHandler.GetPosition();

                shouldDestroy = hittable.ShouldBlockProjectiles();
                hittable.ProcessProjectileHit(attacker, shooterWeapon, shooterWeapon.GetHitCounter(), attack, other.ClosestPointOnBounds(transform.position),
                    hitSourcePos, damageMultiplier);
            }
            else if (other.transform.root.TryGetComponent(out Projectile otherProjectile))
            {
                // Dont despawn projectiles that come from the same attacker
                if (otherProjectile.attacker == attacker) { return; }
            }
            else if (other.attachedRigidbody)
            {
                other.attachedRigidbody.AddForceAtPosition(rb.linearVelocity, other.ClosestPointOnBounds(transform.position), ForceMode.VelocityChange);
            }

            if (!other.isTrigger | shouldDestroy) { NetworkObject.Despawn(true); }
        }

        private void OnDisable()
        {
            canHitPlayers = true;

            attacker = null;
            shooterWeapon = null;
            attack = null;
            projectileForce = default;
            damageMultiplier = default;
            originalRotation = default;
            initialized = false;

            rb.Sleep();

            nearbyWhooshPlayed = false;

            NetworkPhysicsSimulation.RemoveRigidbody(rb);

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