using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class ColliderWeapon : RuntimeWeapon
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Attributes attributes))
            {
                if (parentAttributes == attributes) { return; }

                attributes.ProcessMeleeHit(parentAttributes,
                    other.ClosestPointOnBounds(transform.position),
                    GetHitReaction(Vector3.SignedAngle(parentAttributes.transform.forward, attributes.transform.position - parentAttributes.transform.position, Vector3.up))
                );
            }
        }
    }
}
