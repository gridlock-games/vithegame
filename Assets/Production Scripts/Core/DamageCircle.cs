using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core.GameModeManagers;

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

            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
            }
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

        private CapsuleCollider capsuleCollider;
        private void Start()
        {
            transform.localScale = PlayerDataManager.Singleton.GetDamageCircleMaxScale();
            targetScale = transform.localScale;
            capsuleCollider = GetComponent<CapsuleCollider>();

            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }

        private Vector3 targetScale;
        private void Update()
        {
            if (!GameModeManager.Singleton) { Debug.LogError("Damage circle should only be present when there is a game mode manager!"); return; }
            if (!IsServer) { return; }
            
            if (canShrink) { transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * shrinkSpeed); }

            if (GameModeManager.Singleton.ShouldDisplayNextGameAction() | GameModeManager.Singleton.IsGameOver()) { return; }

            Vector3 bottom = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y - (capsuleCollider.height * transform.localScale.y / 2), capsuleCollider.center.z);
            Vector3 top = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y + (capsuleCollider.height * transform.localScale.y / 2), capsuleCollider.center.z);
            Collider[] collidersInside = Physics.OverlapCapsule(bottom, top, capsuleCollider.radius * transform.localScale.x, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

            List<CombatAgent> attributesInside = new List<CombatAgent>();
            foreach (Collider col in collidersInside)
            {
                if (col.TryGetComponent(out NetworkCollider networkCollider)) { attributesInside.Add(networkCollider.CombatAgent); }
            }

            foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            {
                if (!attributesInside.Contains(attributes)) { attributes.ProcessEnvironmentDamage(Time.deltaTime * -healthDeductionRate, NetworkObject); }
            }
        }
    }
}