using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.VFX;

namespace Vi.ScriptableObjects
{
    [DisallowMultipleComponent]
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
        public Vector3 vfxRotationOffset = new Vector3(0, 0, 0);

        public VFXSpawnType vfxSpawnType = VFXSpawnType.OnActivate;
        public TransformType transformType = TransformType.Stationary;

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
        [SerializeField] protected AudioClip audioClipToPlayOnDestroy;

        public static readonly string[] layersToAccountForInRaycasting = new string[]
        {
            "Default",
            "ProjectileCollider"
        };

        protected void OnEnable()
        {
            if (Application.isEditor)
            {
                foreach (AudioSource audioSource in GetComponentsInChildren<AudioSource>())
                {
                    Debug.LogError("Action VFX " + name + " should not have an audio source component!");
                }
            }

            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = ps.main;
                main.cullingMode = NetworkManager.Singleton.IsServer | ps.gameObject.CompareTag(ObjectPoolingManager.cullingOverrideTag) ? ParticleSystemCullingMode.AlwaysSimulate : ParticleSystemCullingMode.PauseAndCatchup;
            }

            if (audioClipToPlayOnAwake) { StartCoroutine(PlayAwakeAudioClip()); }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) { StartCoroutine(DespawnVFXAfterPlaying()); }
        }

        private IEnumerator DespawnVFXAfterPlaying()
        {
            yield return null;

            bool componentFound = false;

            ParticleSystem particleSystem = GetComponentInChildren<ParticleSystem>();
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
            AudioManager.Singleton.PlayClipOnTransform(transform, audioClipToPlayOnAwake, false, actionVFXSoundEffectVolume);
        }

        [SerializeField] private PooledObject[] VFXToPlayOnDestroy = new PooledObject[0];

        protected void OnDisable()
        {
            if (audioClipToPlayOnDestroy) { AudioManager.Singleton.PlayClipAtPoint(null, audioClipToPlayOnDestroy, transform.position, actionVFXSoundEffectVolume); }
            
            foreach (PooledObject prefab in VFXToPlayOnDestroy)
            {
                FasterPlayerPrefs.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(prefab, transform.position, transform.rotation)));
            }
        }
    }
}