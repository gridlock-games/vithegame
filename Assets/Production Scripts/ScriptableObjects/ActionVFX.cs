using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;
using Unity.Netcode;
using UnityEngine.VFX;
using Unity.Netcode.Components;

namespace Vi.ScriptableObjects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PooledObject))]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public class ActionVFX : NetworkBehaviour
    {
        public enum VFXSpawnType
        {
            OnActivate,
            OnHit
        }

        public enum TransformType
        {
            Stationary,
            ParentToOriginator,
            SpawnAtWeaponPoint,
            Projectile,
            ConformToGround,
            ParentToVictim,
            StationaryOnVictim,
            AimAtTarget
        }

        public Vector3 vfxPositionOffset = new Vector3(0, 0, 0);
        public bool baseRotationOnRoot;
        public Vector3 vfxRotationOffset = new Vector3(0, 0, 0);

        public VFXSpawnType vfxSpawnType = VFXSpawnType.OnActivate;
        public TransformType transformType = TransformType.Stationary;
        public bool offsetByTargetBodyHeight;

        // Only used for VFXSpawnType.OnActivate
        public float onActivateVFXSpawnNormalizedTime;

        // Only used for TransformType.ConformToGround
        public Vector3 raycastOffset = new Vector3(0, 2, 3);
        public Vector3 fartherRaycastOffset = new Vector3(0, 4, 5);
        public float raycastMaxDistance = 5;
        public Vector3 lookRotationUpDirection = new Vector3(0, 1, 0);

        // Only used for TransformType.SpawnAtWeaponPoint
        public Weapon.WeaponBone weaponBone = Weapon.WeaponBone.RightHand;

        [SerializeField] protected AudioClip audioClipToPlayOnAwake;
        [SerializeField] protected float awakeAudioClipDelay;
        [SerializeField] protected float awakeAudioClipStartTime;
        [SerializeField] protected AudioClip audioClipToPlayOnDestroy;

        public static readonly string[] layersToAccountForInRaycasting = new string[]
        {
            "Default",
            "ProjectileCollider"
        };

        protected ParticleSystem[] particleSystems { get { return _particleSystems; } }
        private ParticleSystem[] _particleSystems = new ParticleSystem[0];
        protected virtual void Awake()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>();
            colliders = GetComponentsInChildren<Collider>();
        }

        private PooledObject pooledObject;
        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            foreach (AudioSource audioSource in GetComponentsInChildren<AudioSource>())
            {
                Debug.LogError("Action VFX " + name + " should not have an audio source component!");
            }
#endif
            foreach (ParticleSystem ps in particleSystems)
            {
                NetworkPhysicsSimulation.AddParticleSystem(ps);
                ParticleSystem.MainModule main = ps.main;
                main.cullingMode = NetworkManager.Singleton.IsServer | ps.gameObject.CompareTag(ObjectPoolingManager.cullingOverrideTag) ? ParticleSystemCullingMode.AlwaysSimulate : ParticleSystemCullingMode.PauseAndCatchup;
                ps.Play(false);
            }

            if (TryGetComponent(out Rigidbody rb))
            {
                NetworkPhysicsSimulation.AddRigidbody(rb);
            }

            if (!pooledObject) { pooledObject = GetComponent<PooledObject>(); }
            if (pooledObject.IsPrewarmObject()) { return; }

            if (audioClipToPlayOnAwake) { StartCoroutine(PlayAwakeAudioClip()); }
        }

        protected Collider[] colliders = new Collider[0];
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                StartCoroutine(DespawnVFXAfterPlaying());
                foreach (Collider col in colliders)
                {
                    col.enabled = IsServer;
                }
            }
        }

        private IEnumerator DespawnVFXAfterPlaying()
        {
            yield return null;

            bool componentFound = false;

            ParticleSystem particleSystem = particleSystems.Length > 0 ? particleSystems[0] : null;
            if (particleSystem)
            {
                componentFound = true;
                while (true)
                {
                    yield return null;
                    if (!particleSystem.isPlaying) { break; }
                }
            }

            AudioSource audioSource = GetComponentInChildren<AudioSource>();
            if (audioSource)
            {
                componentFound = true;
                while (true)
                {
                    yield return null;
                    if (!audioSource.isPlaying) { break; }
                }
            }

            VisualEffect visualEffect = GetComponentInChildren<VisualEffect>();
            if (visualEffect)
            {
                componentFound = true;
                while (true)
                {
                    yield return null;
                    if (!visualEffect.HasAnySystemAwake()) { break; }
                }
            }

            if (!componentFound) { yield break; }

            if (IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected const float actionVFXSoundEffectVolume = 0.7f;

        private IEnumerator PlayAwakeAudioClip()
        {
            yield return new WaitForSeconds(awakeAudioClipDelay);
            AudioSource audioSource = AudioManager.Singleton.PlayClipOnTransform(transform, audioClipToPlayOnAwake, false, actionVFXSoundEffectVolume);
            if (audioSource)
            {
                audioSource.time = awakeAudioClipStartTime;
            }
        }

        [SerializeField] private PooledObject[] VFXToPlayOnDestroy = new PooledObject[0];

        protected virtual void OnDisable()
        {
            if (TryGetComponent(out Rigidbody rb))
            {
                NetworkPhysicsSimulation.RemoveRigidbody(rb);
            }

            foreach (ParticleSystem ps in particleSystems)
            {
                NetworkPhysicsSimulation.RemoveParticleSystem(ps);
            }

            if (pooledObject.IsPrewarmObject()) { return; }

            if (audioClipToPlayOnDestroy) { AudioManager.Singleton.PlayClipAtPoint(null, audioClipToPlayOnDestroy, transform.position, actionVFXSoundEffectVolume); }
            
            foreach (PooledObject prefab in VFXToPlayOnDestroy)
            {
                FasterPlayerPrefs.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(prefab, transform.position, transform.rotation)));
            }
        }

#if UNITY_EDITOR
        public void SetLayers()
        {
            bool shouldDirty = false;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.layer != LayerMask.NameToLayer("Projectile"))
                {
                    shouldDirty = true;
                    child.gameObject.layer = LayerMask.NameToLayer("Projectile");
                }
            }
            if (shouldDirty) { UnityEditor.EditorUtility.SetDirty(this); }
        }

        private void OnValidate()
        {
            if (Application.isPlaying) { return; }

            if (TryGetComponent(out NetworkTransform networkTransform))
            {
                bool shouldSetDirty = false;
                if (networkTransform.SyncScaleX)
                {
                    networkTransform.SyncScaleX = false;
                    shouldSetDirty = true;
                }

                if (networkTransform.SyncScaleY)
                {
                    networkTransform.SyncScaleY = false;
                    shouldSetDirty = true;
                }

                if (networkTransform.SyncScaleZ)
                {
                    networkTransform.SyncScaleZ = false;
                    shouldSetDirty = true;
                }

                if (shouldSetDirty) { UnityEditor.EditorUtility.SetDirty(networkTransform); }
            }
        }
#endif
    }
}