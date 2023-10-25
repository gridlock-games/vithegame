using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core
{
    public class ColliderWeapon : RuntimeWeapon
    {
        private List<Attributes> hitsThisFrame = new List<Attributes>();

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (!parentWeaponHandler) { return; }
            if (!parentWeaponHandler.IsAttacking) { return; }
            if (parentWeaponHandler.currentActionClip.weaponBone != weaponBone) { return; }
            if (other.TryGetComponent(out Attributes attributes))
            {
                if (parentAttributes == attributes) { return; }
                if (hitCounter.ContainsKey(attributes))
                {
                    if (hitCounter[attributes] >= parentWeaponHandler.currentActionClip.maxHitLimit) { return; }
                }

                if (hitsThisFrame.Contains(attributes)) { return; }
                
                hitsThisFrame.Add(attributes);
                attributes.ProcessMeleeHit(parentAttributes,
                    parentWeaponHandler.currentActionClip,
                    this,
                    other.ClosestPointOnBounds(transform.position),
                    Vector3.SignedAngle(attributes.transform.forward, parentAttributes.transform.position - attributes.transform.position, Vector3.up)
                );
            }
        }

        private bool clearListNextUpdate;
        private void FixedUpdate()
        {
            if (clearListNextUpdate) { hitsThisFrame.Clear(); }
            clearListNextUpdate = hitsThisFrame.Count > 0;
        }

        private void OnDrawGizmos()
        {
            if (!parentWeaponHandler) { return; }

            if (TryGetComponent(out BoxCollider boxCollider))
            {
                if (parentWeaponHandler.currentActionClip)
                {
                    if (parentWeaponHandler.currentActionClip.weaponBone == weaponBone)
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
                
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCollider.transform.position, boxCollider.transform.rotation, boxCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (TryGetComponent(out SphereCollider sphereCollider))
            {
                if (parentWeaponHandler.currentActionClip)
                {
                    if (parentWeaponHandler.currentActionClip.weaponBone == weaponBone)
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

                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
        }
    }
}
