using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace Vi.ScriptableObjects
{
    [DisallowMultipleComponent]
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

        private IEnumerator PlayAwakeAudioClip()
        {
            yield return new WaitForSeconds(awakeAudioClipDelay);
            AudioManager.Singleton.PlayClipOnTransform(transform, audioClipToPlayOnAwake, false);
        }

        [SerializeField] private GameObject[] VFXToPlayOnDestroy = new GameObject[0];

        protected void OnDisable()
        {
            if (audioClipToPlayOnDestroy) { AudioManager.Singleton.PlayClipAtPoint(null, audioClipToPlayOnAwake, transform.position); }
            
            foreach (GameObject prefab in VFXToPlayOnDestroy)
            {
                FasterPlayerPrefs.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(prefab, transform.position, transform.rotation)));
            }
        }
    }
}