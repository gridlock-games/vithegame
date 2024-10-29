using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.ProceduralAnimations;

namespace Vi.Core.Weapons
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
        public Vector3 GetOffHandIKOffset(CharacterReference.RaceAndGender raceAndGender) { return IKData.Find(item => item.raceAndGender == raceAndGender).offHandIKOffset; }
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
            public Vector3 offHandIKOffset;
        }

        public override void ResetHitCounter()
        {
            base.ResetHitCounter();
            projectileSpawnCount = 0;
            lastProjectileSpawnTime = Mathf.NegativeInfinity;
        }

        private float lastProjectileSpawnTime = Mathf.NegativeInfinity;
        private int projectileSpawnCount;

        public Quaternion GetProjectileSpawnRotation()
        {
            int hitCount = Physics.RaycastNonAlloc(parentCombatAgent.AnimationHandler.GetCameraPivotPoint(), parentCombatAgent.AnimationHandler.GetCameraForwardDirection().normalized,
                        projectileRotationRaycastingResults, 50, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

            Vector3 targetPoint = parentCombatAgent.AnimationHandler.GetAimPoint();
            float minDistance = 0;
            bool minDistanceInitialized = false;
            for (int i = 0; i < hitCount; i++)
            {
                if (projectileRotationRaycastingResults[i].distance > minDistance & minDistanceInitialized) { continue; }
                RaycastHit hit = projectileRotationRaycastingResults[i];
                if (Mathf.Abs(hit.normal.y) >= 0.9f) { continue; }
                if (hit.transform.root == parentCombatAgent.WeaponHandler.transform.root) { continue; }
                if (hit.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent == parentCombatAgent) { continue; }
                }
                else // No Network Collider
                {
                    continue;
                }
                if (hit.distance > 1.5f) { targetPoint = hit.point; }
                minDistance = hit.distance;
                minDistanceInitialized = true;
            }

            return Quaternion.LookRotation(targetPoint - projectileSpawnPoint.transform.position);
        }

        private RaycastHit[] projectileRotationRaycastingResults = new RaycastHit[10];
        private void LateUpdate()
        {
            if (isStowed) { return; }
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (!parentCombatAgent) { return; }
            if (!parentCombatAgent.WeaponHandler.IsAiming(aimHand)) { return; }
            if (!parentCombatAgent.WeaponHandler.IsAttacking) { return; }
            if (!parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone)) { return; }

            bool shouldUseAmmo = parentCombatAgent.WeaponHandler.ShouldUseAmmo();
            if (shouldUseAmmo)
            {
                if (parentCombatAgent.WeaponHandler.GetAmmoCount() <= 0 | !parentCombatAgent.WeaponHandler.CurrentActionClip.requireAmmo) { return; }
            }

            if (projectileSpawnCount < parentCombatAgent.WeaponHandler.CurrentActionClip.maxHitLimit)
            {
                if (Time.time - lastProjectileSpawnTime > parentCombatAgent.WeaponHandler.CurrentActionClip.GetTimeBetweenHits(parentCombatAgent.AnimationHandler.Animator.speed))
                {
                    Projectile projectileInstance = ObjectPoolingManager.SpawnObject(projectile.GetComponent<PooledObject>(), projectileSpawnPoint.transform.position,
                        GetProjectileSpawnRotation()).GetComponent<Projectile>();

                    NetworkObject netObj = projectileInstance.GetComponent<NetworkObject>();
                    netObj.Spawn(true);
                    lastProjectileSpawnTime = Time.time;
                    projectileSpawnCount++;
                    if (shouldUseAmmo)
                    {
                        int damageMultiplerIndex = parentCombatAgent.WeaponHandler.GetMaxAmmoCount() - parentCombatAgent.WeaponHandler.GetAmmoCount();
                        projectileInstance.Initialize(parentCombatAgent, this, parentCombatAgent.WeaponHandler.CurrentActionClip, projectileForce,
                            ammoCountDamageMultipliers.Length > damageMultiplerIndex ? ammoCountDamageMultipliers[damageMultiplerIndex] : 1);

                        parentCombatAgent.WeaponHandler.UseAmmo();
                    }
                    else
                    {
                        projectileInstance.Initialize(parentCombatAgent, this, parentCombatAgent.WeaponHandler.CurrentActionClip, projectileForce, 1);
                    }
                    //StartCoroutine(SetProjectileNetworkVisibility(netObj));
                }
            }
        }

        //private IEnumerator SetProjectileNetworkVisibility(NetworkObject netObj)
        //{
        //    yield return null;
        //    if (!netObj.IsSpawned) { yield return new WaitUntil(() => netObj.IsSpawned); }

        //    if (!netObj.IsNetworkVisibleTo(parentCombatAgent.WeaponHandler.OwnerClientId)) { netObj.NetworkShow(parentCombatAgent.WeaponHandler.OwnerClientId); }
        //    foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators())
        //    {
        //        ulong networkId = playerData.id >= 0 ? (ulong)playerData.id : 0;
        //        if (networkId == 0) { continue; }
        //        if (networkId == parentCombatAgent.WeaponHandler.OwnerClientId) { continue; }

        //        if (playerData.channel == parentCombatAgent.CachedPlayerData.channel)
        //        {
        //            if (!netObj.IsNetworkVisibleTo(networkId))
        //            {
        //                netObj.NetworkShow(networkId);
        //            }
        //        }
        //        else
        //        {
        //            if (parentCombatAgent.WeaponHandler.NetworkObject.IsNetworkVisibleTo(networkId))
        //            {
        //                netObj.NetworkHide(networkId);
        //            }
        //        }
        //    }
        //}

        public float GetNextDamageMultiplier()
        {
            if (!parentCombatAgent) { return 1; }
            int damageMultiplerIndex = parentCombatAgent.WeaponHandler.GetMaxAmmoCount() - parentCombatAgent.WeaponHandler.GetAmmoCount();
            if (damageMultiplerIndex < 0) { return 1; }
            if (damageMultiplerIndex == 0 & ammoCountDamageMultipliers.Length == 0) { return 1; }
            return ammoCountDamageMultipliers.Length > damageMultiplerIndex ? ammoCountDamageMultipliers[damageMultiplerIndex] : 1;
        }

        private void OnDrawGizmos()
        {
            if (!parentCombatAgent) { return; }

            if (parentCombatAgent.WeaponHandler.CurrentActionClip)
            {
                if (parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones != null)
                {
                    if (parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone))
                    {
                        if (parentCombatAgent.WeaponHandler.IsInAnticipation)
                            Gizmos.color = Color.yellow;
                        else if (parentCombatAgent.WeaponHandler.IsAttacking)
                            Gizmos.color = Color.red;
                        else if (parentCombatAgent.WeaponHandler.IsInRecovery)
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