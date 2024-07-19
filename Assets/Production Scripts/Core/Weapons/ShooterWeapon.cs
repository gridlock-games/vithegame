using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class ShooterWeapon : RuntimeWeapon
    {
        [Header("Shooter Settings")]
        [SerializeField] private bool canADS = true;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private Projectile projectile;
        [SerializeField] private Vector3 projectileForce = new Vector3(0, 0, 5);
        [SerializeField] private float[] ammoCountDamageMultipliers = new float[0];
        [Header("IK Settings")]
        [SerializeField] private LimbReferences.Hand aimHand = LimbReferences.Hand.RightHand;
        [SerializeField] private Transform offHandGrip;
        [SerializeField] private LimbReferences.BodyAimType bodyAimType = LimbReferences.BodyAimType.Normal;
        [SerializeField] private List<InverseKinematicsData> IKData = new List<InverseKinematicsData>();

        public bool CanADS() { return canADS; }
        public Transform GetProjectileSpawnPoint() { return projectileSpawnPoint; }
        public LimbReferences.Hand GetAimHand() { return aimHand; }
        public Vector3 GetAimHandIKOffset(CharacterReference.RaceAndGender raceAndGender) { return IKData.Find(item => item.raceAndGender == raceAndGender).aimHandIKOffset; }
        public Vector3 GetBodyAimIKOffset(CharacterReference.RaceAndGender raceAndGender) { return IKData.Find(item => item.raceAndGender == raceAndGender).bodyAimIKOffset; }
        public LimbReferences.BodyAimType GetBodyAimType() { return bodyAimType; }
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

        [System.Serializable]
        private struct InverseKinematicsData
        {
            public CharacterReference.RaceAndGender raceAndGender;
            public Vector3 aimHandIKOffset;
            public Vector3 bodyAimIKOffset;
        }

        public override void ResetHitCounter()
        {
            projectileSpawnCount = 0;
            lastProjectileSpawnTime = Mathf.NegativeInfinity;
        }

        private float lastProjectileSpawnTime = Mathf.NegativeInfinity;
        private int projectileSpawnCount;

        private void LateUpdate()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (!parentWeaponHandler) { return; }
            if (!parentWeaponHandler.IsAiming(aimHand)) { return; }
            if (!parentWeaponHandler.IsAttacking) { return; }
            if (!parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone)) { return; }

            bool shouldUseAmmo = parentWeaponHandler.ShouldUseAmmo();
            if (shouldUseAmmo)
            {
                if (parentWeaponHandler.GetAmmoCount() <= 0 | !parentWeaponHandler.CurrentActionClip.requireAmmo) { return; }
            }

            if (projectileSpawnCount < parentWeaponHandler.CurrentActionClip.maxHitLimit)
            {
                if (Time.time - lastProjectileSpawnTime > parentWeaponHandler.CurrentActionClip.GetTimeBetweenHits(parentAnimationHandler.Animator.speed))
                {
                    GameObject projectileInstance = Instantiate(projectile.gameObject, projectileSpawnPoint.transform.position, projectileSpawnPoint.transform.rotation);
                    NetworkObject netObj = projectileInstance.GetComponent<NetworkObject>();
                    netObj.Spawn();
                    lastProjectileSpawnTime = Time.time;
                    projectileSpawnCount++;
                    if (shouldUseAmmo)
                    {
                        int damageMultiplerIndex = parentWeaponHandler.GetMaxAmmoCount() - parentWeaponHandler.GetAmmoCount();
                        projectileInstance.GetComponent<Projectile>().Initialize(parentAttributes, this, parentWeaponHandler.CurrentActionClip, projectileForce,
                            ammoCountDamageMultipliers.Length > damageMultiplerIndex ? ammoCountDamageMultipliers[damageMultiplerIndex] : 1);

                        parentWeaponHandler.UseAmmo();
                    }
                    else
                    {
                        projectileInstance.GetComponent<Projectile>().Initialize(parentAttributes, this, parentWeaponHandler.CurrentActionClip, projectileForce, 1);
                    }
                    StartCoroutine(SetProjectileNetworkVisibility(netObj));
                }
            }
        }

        private IEnumerator SetProjectileNetworkVisibility(NetworkObject netObj)
        {
            yield return null;
            if (!netObj.IsSpawned) { yield return new WaitUntil(() => netObj.IsSpawned); }

            if (!netObj.IsNetworkVisibleTo(parentWeaponHandler.OwnerClientId)) { netObj.NetworkShow(parentWeaponHandler.OwnerClientId); }
            foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators())
            {
                ulong networkId = playerData.id >= 0 ? (ulong)playerData.id : 0;
                if (networkId == 0) { continue; }
                if (networkId == parentWeaponHandler.OwnerClientId) { continue; }

                if (playerData.channel == parentAttributes.CachedPlayerData.channel)
                {
                    if (!netObj.IsNetworkVisibleTo(networkId))
                    {
                        netObj.NetworkShow(networkId);
                    }
                }
                else
                {
                    if (parentWeaponHandler.NetworkObject.IsNetworkVisibleTo(networkId))
                    {
                        netObj.NetworkHide(networkId);
                    }
                }
            }
        }

        public float GetNextDamageMultiplier()
        {
            if (!parentWeaponHandler) { return 1; }
            int damageMultiplerIndex = parentWeaponHandler.GetMaxAmmoCount() - parentWeaponHandler.GetAmmoCount();
            return ammoCountDamageMultipliers.Length > damageMultiplerIndex ? ammoCountDamageMultipliers[damageMultiplerIndex] : 1;
        }

        private void OnDrawGizmos()
        {
            if (!parentWeaponHandler) { return; }

            if (parentWeaponHandler.CurrentActionClip)
            {
                if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones != null)
                {
                    if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone))
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

            Gizmos.DrawRay(projectileSpawnPoint.position, projectileSpawnPoint.rotation * projectileForce * 10);
        }
    }
}