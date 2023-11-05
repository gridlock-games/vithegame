using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core
{
    public class ShooterWeapon : RuntimeWeapon
    {
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private Projectile projectile;
        [SerializeField] private Vector3 projectileForce = new Vector3(0, 0, 5);
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

        public override void ResetHitCounter()
        {
            projectileSpawnCount = 0;
            lastProjectileSpawnTime = Mathf.NegativeInfinity;
        }

        private float lastProjectileSpawnTime = Mathf.NegativeInfinity;
        private float projectileSpawnCount;

        private void Update()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (!parentWeaponHandler) { return; }
            if (!parentWeaponHandler.IsAiming(aimHand)) { return; }
            if (!parentWeaponHandler.IsAttacking) { return; }
            if (!parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(weaponBone)) { return; }

            if (projectileSpawnCount < parentWeaponHandler.CurrentActionClip.maxHitLimit)
            {
                if (Time.time - lastProjectileSpawnTime > parentWeaponHandler.CurrentActionClip.timeBetweenHits)
                {
                    GameObject projectileInstance = Instantiate(projectile.gameObject, projectileSpawnPoint.transform.position, projectileSpawnPoint.transform.rotation);
                    projectileInstance.GetComponent<NetworkObject>().Spawn();
                    projectileInstance.GetComponent<Projectile>().Initialize(parentAttributes, parentWeaponHandler.CurrentActionClip, projectileForce);
                    lastProjectileSpawnTime = Time.time;
                    projectileSpawnCount++;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!parentWeaponHandler) { return; }

            if (parentWeaponHandler.CurrentActionClip)
            {
                if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones != null)
                {
                    if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(weaponBone))
                    {
                        if (parentWeaponHandler.IsInAnticipation)
                            Gizmos.color = Color.yellow;
                        else if (parentWeaponHandler.IsAttacking)
                            Gizmos.color = Color.red;
                        else if (parentWeaponHandler.IsInRecovery)
                            Gizmos.color = Color.magenta;
                        else
                            Gizmos.color = Color.white;
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                    }
                }
                else
                {
                    Gizmos.color = Color.white;
                }
            }
            else
            {
                Gizmos.color = Color.white;
            }

            Gizmos.DrawRay(projectileSpawnPoint.position, projectileSpawnPoint.rotation * Vector3.forward * 10);
        }
    }
}