using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.VFX;
using Vi.ScriptableObjects;

namespace Vi.Core.Weapons
{
    public class ColliderWeapon : RuntimeWeapon
    {
        [SerializeField] private VisualEffect weaponTrailVFX;
        private const float weaponTrailDeactivateDuration = 0.2f;
        private float lastWeaponTrailActiveTime = Mathf.NegativeInfinity;

        private BoxCollider[] boxColliders;
        private Dictionary<BoxCollider, Vector3> boxColliderOriginalSizes = new Dictionary<BoxCollider, Vector3>();
        protected override void Awake()
        {
            base.Awake();
            boxColliders = GetComponentsInChildren<BoxCollider>();
            foreach (BoxCollider box in boxColliders)
            {
                boxColliderOriginalSizes.Add(box, box.size);
            }
        }

        public override void SetColliderMultiplier(Vector3 multiplier)
        {
            foreach (BoxCollider box in boxColliders)
            {
                box.size = Vector3.Scale(boxColliderOriginalSizes[box], multiplier);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!weaponTrailVFX) { return; }
            if (!parentCombatAgent) { return; }

            if (parentCombatAgent.WeaponHandler.IsAttacking & parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone) & !IsStowed)
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

        private void ProcessTriggerEvent(Collider other)
        {
            if (IsStowed) { return; }
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (!parentCombatAgent) { return; }
            if (parentCombatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }
            if (!parentCombatAgent.WeaponHandler.IsAttacking) { return; }
            if (parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones == null) { return; }
            if (!parentCombatAgent.WeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(WeaponBone)) { return; }

            // Don't evaluate grab attacks here, it's evaluated in the animation handler script
            if (parentCombatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.GrabAttack) { return; }

            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (other.isTrigger) { return; }
                if (parentCombatAgent == networkCollider.CombatAgent) { return; }
                if (!CanHit(networkCollider.CombatAgent)) { return; }

                bool bHit = networkCollider.CombatAgent.ProcessMeleeHit(parentCombatAgent,
                    parentCombatAgent.WeaponHandler.CurrentActionClip,
                    this,
                    other.ClosestPointOnBounds(transform.position),
                    parentCombatAgent.transform.position
                );
            }
            else if (other.transform.root.TryGetComponent(out IHittable hittable))
            {
                if ((Object)hittable == parentCombatAgent) { return; }
                if (!CanHit(hittable)) { return; }

                bool bHit = hittable.ProcessMeleeHit(parentCombatAgent,
                    parentCombatAgent.WeaponHandler.CurrentActionClip,
                    this,
                    other.ClosestPointOnBounds(transform.position),
                    parentCombatAgent.transform.position
                );
            }
        }

        private void OnDrawGizmos()
        {
            if (!parentCombatAgent) { return; }

            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                if (col is BoxCollider boxCollider)
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
                else if (col is SphereCollider sphereCollider)
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
}
