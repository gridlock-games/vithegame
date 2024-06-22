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
            if (!IsServer) { Debug.LogError("DamageCircle.Shrink() should only be called on the server"); return; }

            targetScale = Vector3.MoveTowards(targetScale, PlayerDataManager.Singleton.GetDamageCircleMinScale(), PlayerDataManager.Singleton.GetDamageCircleShrinkSize());
            ShrinkClientRpc();
        }

        [Rpc(SendTo.NotServer)]
        private void ShrinkClientRpc()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
            }
        }

        public void ResetDamageCircle()
        {
            if (!IsServer) { Debug.LogError("DamageCircle.ResetDamageCircle() should only be called on the server"); return; }

            transform.localScale = PlayerDataManager.Singleton.GetDamageCircleMaxScale();
            targetScale = transform.localScale;
            ResetDamageCircleClientRpc();
        }

        [Rpc(SendTo.NotServer)]
        private void ResetDamageCircleClientRpc()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }

        public bool IsPointInsideDamageCircleBounds(Vector3 point)
        {
            return Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(point.x, point.z)) < targetScale.x;
        }

        private Collider[] damageCircleColliders;
        private void Start()
        {
            transform.localScale = PlayerDataManager.Singleton.GetDamageCircleMaxScale();
            targetScale = transform.localScale;
            damageCircleColliders = GetComponentsInChildren<Collider>();

            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
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
    }
}