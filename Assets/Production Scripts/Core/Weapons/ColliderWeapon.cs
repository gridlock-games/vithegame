using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.VFX;
using Vi.Core.GameModeManagers;
using Vi.Core.VFX;

namespace Vi.Core
{
    public class ColliderWeapon : RuntimeWeapon
    {
        [SerializeField] private VisualEffect weaponTrailVFX;
        private const float weaponTrailDeactivateDuration = 0.2f;
        private float lastWeaponTrailActiveTime = Mathf.NegativeInfinity;
        private void Update()
        {
            if (!weaponTrailVFX) { return; }
            if (!parentCombatAgent) { return; }

            if (parentCombatAgent.WeaponHandler.IsAttacking & parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone) & !isStowed)
            {
                weaponTrailVFX.gameObject.SetActive(true);
                lastWeaponTrailActiveTime = Time.time;
            }
            else if (Time.time - lastWeaponTrailActiveTime > weaponTrailDeactivateDuration)
            {
                weaponTrailVFX.gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other) { ProcessTriggerEvent(other); }
        private void OnTriggerStay(Collider other) { ProcessTriggerEvent(other); }

        private List<IHittable> hitsOnThisPhysicsUpdate = new List<IHittable>();
        private void ProcessTriggerEvent(Collider other)
        {
            if (isStowed) { return; }
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (!parentCombatAgent) { return; }
            if (!parentCombatAgent.WeaponHandler.IsAttacking) { return; }
            if (parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones == null) { return; }
            if (!parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone)) { return; }

            // Don't evaluate grab attacks here, it's evaluated in the animation handler script
            if (parentCombatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ScriptableObjects.ActionClip.ClipType.GrabAttack) { return; }

            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (parentCombatAgent == networkCollider.CombatAgent) { return; }
                if (!CanHit(networkCollider.CombatAgent)) { return; }

                if (hitsOnThisPhysicsUpdate.Contains(networkCollider.CombatAgent)) { return; }

                bool bHit = networkCollider.CombatAgent.ProcessMeleeHit(parentCombatAgent,
                    parentCombatAgent.WeaponHandler.CurrentActionClip,
                    this,
                    other.ClosestPointOnBounds(transform.position),
                    parentCombatAgent.transform.position
                );

                if (bHit)
                {
                    hitsOnThisPhysicsUpdate.Add(networkCollider.CombatAgent);
                }
            }
            else if (other.transform.root.TryGetComponent(out IHittable hittable))
            {
                if ((Object)hittable == parentCombatAgent) { return; }
                if (!CanHit(hittable)) { return; }

                if (hitsOnThisPhysicsUpdate.Contains(hittable)) { return; }

                bool bHit = hittable.ProcessMeleeHit(parentCombatAgent,
                    parentCombatAgent.WeaponHandler.CurrentActionClip,
                    this,
                    other.ClosestPointOnBounds(transform.position),
                    parentCombatAgent.transform.position
                );

                if (bHit)
                {
                    hitsOnThisPhysicsUpdate.Add(hittable);
                }
            }
        }

        private bool clearListNextUpdate;
        private void FixedUpdate()
        {
            if (clearListNextUpdate) { hitsOnThisPhysicsUpdate.Clear(); }
            clearListNextUpdate = hitsOnThisPhysicsUpdate.Count > 0;
        }

        private void OnDrawGizmos()
        {
            if (!parentCombatAgent) { return; }

            if (TryGetComponent(out BoxCollider boxCollider))
            {
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

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCollider.transform.position, boxCollider.transform.rotation, boxCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (TryGetComponent(out SphereCollider sphereCollider))
            {
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

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(sphereCollider.transform.position, sphereCollider.transform.rotation, sphereCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
        }
    }
}
