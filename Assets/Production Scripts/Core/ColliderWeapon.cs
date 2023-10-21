using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.Player
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
                    GetHitReaction(Vector3.SignedAngle(transform.forward, attributes.transform.position - transform.position, Vector3.up))
                );
            }
        }
    }
}
