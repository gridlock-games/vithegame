using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core
{
    public class ColliderWeapon : RuntimeWeapon
    {
        private List<Attributes> hitsOnThisPhysicsUpdate = new List<Attributes>();

        private void Awake()
        {
            foreach (Collider col in GetComponents<Collider>())
            {
                col.enabled = NetworkManager.Singleton.IsServer;
            }
        }

        private void OnTriggerEnter(Collider other) { ProcessTriggerEvent(other); }
        private void OnTriggerStay(Collider other) { ProcessTriggerEvent(other); }

        private void ProcessTriggerEvent(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (other.isTrigger) { return; }
            if (!parentWeaponHandler) { return; }
            if (!parentWeaponHandler.IsAttacking) { return; }
            if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones == null) { return; }
            if (!parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(weaponBone)) { return; }

            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (parentAttributes == networkCollider.Attributes) { return; }
                if (!CanHit(networkCollider.Attributes)) { return; }

                if (hitsOnThisPhysicsUpdate.Contains(networkCollider.Attributes)) { return; }

                bool bHit = networkCollider.Attributes.ProcessMeleeHit(parentAttributes,
                    parentWeaponHandler.CurrentActionClip,
                    this,
                    other.ClosestPointOnBounds(transform.position),
                    parentAttributes.transform.position
                );

                if (bHit)
                {
                    hitsOnThisPhysicsUpdate.Add(networkCollider.Attributes);
                    parentWeaponHandler.lastMeleeHitTime = Time.time;
                }
            }
        }

        private bool clearListNextUpdate;
        private void FixedUpdate()
        {
            if (clearListNextUpdate) { hitsOnThisPhysicsUpdate.Clear(); }
            clearListNextUpdate = hitsOnThisPhysicsUpdate.Count > 0;
        }

        //private void Update()
        //{
        //    if (weaponTrail == null) { return; }

        //    if (parentWeaponHandler.IsAttacking & parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(weaponBone) & !isStowed)
        //    {
        //        weaponTrail.Activate();
        //    }
        //    else
        //    {
        //        weaponTrail.Deactivate(weaponTrailFadeTime);
        //    }
        //}

        private void OnDrawGizmos()
        {
            if (!parentWeaponHandler) { return; }

            if (TryGetComponent(out BoxCollider boxCollider))
            {
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

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCollider.transform.position, boxCollider.transform.rotation, boxCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (TryGetComponent(out SphereCollider sphereCollider))
            {
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

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(sphereCollider.transform.position, sphereCollider.transform.rotation, sphereCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
        }
    }
}
