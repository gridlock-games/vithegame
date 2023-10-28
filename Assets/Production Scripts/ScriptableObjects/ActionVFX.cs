using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ScriptableObjects
{
    public class ActionVFX : MonoBehaviour
    {
        public enum VFXSpawnType
        {
            OnExecute,
            OnActivate,
            OnRecovery,
            OnHit
        }

        public enum TransformType
        {
            Stationary,
            ParentToOriginator,
            OriginatorAndTarget,
            Projectile,
            ConformToGround
        }

        public void SetOriginatorAndTarget(Transform originator, Transform target)
        {

        }

        public Vector3 vfxPositionOffset = new Vector3(0, 0, 0);
        public Vector3 vfxRotationOffset = new Vector3(0, 0, 0);

        public VFXSpawnType vfxSpawnType = VFXSpawnType.OnExecute;
        public TransformType transformType = TransformType.Stationary;

        // Only used for TransformType.ConformToGround
        public Vector3 raycastOffset = new Vector3(0, 2, 0);
        public Vector3 crossProductDirection = new Vector3(1, 0, 0);
        public Vector3 lookRotationUpDirection = new Vector3(0, 1, 0);
    }
}