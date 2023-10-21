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

            if (other.TryGetComponent(out Attributes attributes))
            {
                if (parentAttributes == attributes) { return; }

                attributes.ProcessMeleeHit(parentAttributes,
                    other.ClosestPointOnBounds(transform.position),
                    GetHitReaction(Vector3.SignedAngle(parentAttributes.transform.forward, attributes.transform.position - parentAttributes.transform.position, Vector3.up))
                );
            }
        }

        private void Update()
        {
            //Debug.Log(parentWeaponHandler.IsInAnticipation + " " + parentWeaponHandler.IsAttacking + " " + parentWeaponHandler.IsInRecovery);
        }

        private void OnDrawGizmos()
        {
            if (TryGetComponent(out BoxCollider boxCollider))
            {
                if (parentWeaponHandler.IsInAnticipation)
                    Gizmos.color = Color.yellow;
                else if (parentWeaponHandler.IsAttacking)
                    Gizmos.color = Color.red;
                else if (parentWeaponHandler.IsInRecovery)
                    Gizmos.color = Color.magenta;
                else
                    Gizmos.color = Color.white;
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCollider.transform.position, boxCollider.transform.rotation, boxCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (TryGetComponent(out SphereCollider sphereCollider))
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
        }
    }
}
