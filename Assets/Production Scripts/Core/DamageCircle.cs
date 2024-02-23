using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class DamageCircle : NetworkBehaviour
    {
        public bool canShrink = true;
        public float shrinkSpeed = 5;
        public float healthDeductionRate = 3;

        private List<Attributes> attributesToDamage = new List<Attributes>();

        public void Shrink()
        {
            targetScale = Vector3.MoveTowards(targetScale, new Vector3(5, transform.localScale.y, 5), shrinkSpeed);
            Debug.Log(Time.time + " Shrink " + targetScale);
        }

        private void Start()
        {
            targetScale = transform.localScale;
        }

        private Vector3 targetScale;
        private void Update()
        {
            if (!IsServer) { return; }

            if (canShrink) { transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * shrinkSpeed); }
            
            foreach (Attributes attributes in attributesToDamage)
            {
                attributes.ProcessEnvironmentDamage(Time.deltaTime * -healthDeductionRate, NetworkObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) { return; }
            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                int index = attributesToDamage.IndexOf(networkCollider.Attributes);
                if (index >= 0) { attributesToDamage.RemoveAt(index); }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) { return; }
            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                int index = attributesToDamage.IndexOf(networkCollider.Attributes);
                if (index == -1) { attributesToDamage.Add(networkCollider.Attributes); }
            }
        }
    }
}