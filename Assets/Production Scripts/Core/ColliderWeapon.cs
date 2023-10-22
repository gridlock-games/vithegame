using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class ColliderWeapon : RuntimeWeapon
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!parentWeaponHandler.IsAttacking) { return; }
            if (parentWeaponHandler.currentActionClip.weaponBone != weaponBone) { return; }
            if (other.TryGetComponent(out Attributes attributes))
            {
                if (parentAttributes == attributes) { return; }
                if (hitCounter.ContainsKey(attributes))
                {
                    if (hitCounter[attributes] > parentWeaponHandler.currentActionClip.maxHitLimit) { return; }
                }
                
                attributes.ProcessMeleeHit(parentAttributes,
                    other.ClosestPointOnBounds(transform.position),
                    GetHitReaction(Vector3.SignedAngle(attributes.transform.forward, parentAttributes.transform.position - attributes.transform.position, Vector3.up))
                );
            }
        }

        private void OnDrawGizmos()
        {
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
