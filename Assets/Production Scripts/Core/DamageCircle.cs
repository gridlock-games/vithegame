using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class DamageCircle : NetworkBehaviour
    {
        public bool canShrink = true;
        public float shrinkSpeed = 10;
        public float healthDeductionRate = 3;

        public void Shrink()
        {
            targetScale = Vector3.MoveTowards(targetScale, PlayerDataManager.Singleton.GetDamageCircleMinScale(), PlayerDataManager.Singleton.GetDamageCircleShrinkSize());
        }

        public void ResetDamageCircle()
        {
            transform.localScale = PlayerDataManager.Singleton.GetDamageCircleMaxScale();
            targetScale = transform.localScale;
        }

        private Collider[] damageCircleColliders;
        private void Start()
        {
            transform.localScale = PlayerDataManager.Singleton.GetDamageCircleMaxScale();
            targetScale = transform.localScale;
            damageCircleColliders = GetComponentsInChildren<Collider>();
        }

        private Vector3 targetScale;
        private void Update()
        {
            if (!IsServer) { return; }
            
            if (canShrink) { transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * shrinkSpeed); }

            List<Attributes> attributesToDamage = new List<Attributes>();
            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            {
                foreach (Collider col in damageCircleColliders)
                {
                    if (!col.bounds.Contains(attributes.transform.position))
                    {
                        attributesToDamage.Add(attributes);
                        break;
                    }
                }
            }

            foreach (Attributes attributes in attributesToDamage)
            {
                attributes.ProcessEnvironmentDamage(Time.deltaTime * -healthDeductionRate, NetworkObject);
            }
        }

        //private void OnTriggerEnter(Collider other)
        //{
        //    if (!IsServer) { return; }
        //    if (other.TryGetComponent(out NetworkCollider networkCollider))
        //    {
        //        int index = attributesToDamage.IndexOf(networkCollider.Attributes);
        //        if (index >= 0) { attributesToDamage.RemoveAt(index); }
        //    }
        //}

        //private void OnTriggerExit(Collider other)
        //{
        //    if (!IsServer) { return; }
        //    if (other.TryGetComponent(out NetworkCollider networkCollider))
        //    {
        //        int index = attributesToDamage.IndexOf(networkCollider.Attributes);
        //        if (index == -1) { attributesToDamage.Add(networkCollider.Attributes); }
        //    }
        //}
    }
}