using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;

namespace Vi.ScriptableObjects
{
    [DisallowMultipleComponent]
    public class ActionVFX : MonoBehaviour
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
        public float raycastMaxDistance = 5;
        public Vector3 crossProductDirection = new Vector3(1, 0, 0);
        public Vector3 lookRotationUpDirection = new Vector3(0, 1, 0);

        // Only used for TransformType.SpawnAtWeaponPoint
        public Weapon.WeaponBone weaponBone = Weapon.WeaponBone.RightHand;

        [SerializeField] private AudioClip audioClipToPlayOnAwake;

        [SerializeField] private AudioClip audioClipToPlayOnDestroy;

        protected void Awake()
        {
            if (audioClipToPlayOnAwake) { AudioManager.Singleton.PlayClipOnTransform(transform, audioClipToPlayOnAwake); }
        }

        protected void OnDestroy()
        {
            if (audioClipToPlayOnDestroy) { AudioManager.Singleton.PlayClipAtPoint(null, audioClipToPlayOnAwake, transform.position); }
        }
    }
}