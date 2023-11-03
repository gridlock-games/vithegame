using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class ShooterWeapon : RuntimeWeapon
    {
        [SerializeField] private Transform projectileSpawnPoint;
        [Header("Hand IK Settings")]
        [SerializeField] private LimbReferences.Hand aimHand = LimbReferences.Hand.RightHand;
        [SerializeField] private Transform offHandGrip;

        public LimbReferences.Hand GetAimHand() { return aimHand; }
        public OffHandInfo GetOffHandInfo()
        {
            if (aimHand == LimbReferences.Hand.RightHand)
            {
                return new OffHandInfo(LimbReferences.Hand.LeftHand, offHandGrip);
            }
            return new OffHandInfo(LimbReferences.Hand.RightHand, offHandGrip);
        }

        public struct OffHandInfo
        {
            public LimbReferences.Hand offHand;
            public Transform offHandTarget;

            public OffHandInfo(LimbReferences.Hand offHand, Transform offHandTarget)
            {
                this.offHand = offHand;
                this.offHandTarget = offHandTarget;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawRay(transform.position, transform.forward * 10);
        }
    }
}