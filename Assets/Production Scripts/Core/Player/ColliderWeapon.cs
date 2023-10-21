using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.Player
{
    public class ColliderWeapon : MonoBehaviour
    {
        Attributes parentAttributes;

        private void Start()
        {
            parentAttributes = GetComponentInParent<Attributes>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Attributes attributes))
            {
                if (parentAttributes == attributes) { return; }

                //attributes.ProcessMeleeHit(parentAttributes, other.ClosestPointOnBounds(transform.position));
            }
        }
    }
}
