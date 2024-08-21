using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core.GameModeManagers;
using Vi.Core.CombatAgents;

namespace Vi.Core.DynamicEnvironmentElements
{
    public class DamageCircle : NetworkBehaviour
    {
        public bool canShrink = true;
        public float shrinkSpeed = 10;
        public float healthDeductionRate = 3;

        public void Shrink()
        {
            if (!IsServer) { Debug.LogError("DamageCircle.Shrink() should only be called on the server"); return; }

            targetScale.Value = Vector3.MoveTowards(targetScale.Value, PlayerDataManager.Singleton.GetDamageCircleMinScale(), PlayerDataManager.Singleton.GetDamageCircleShrinkSize());

            shouldRender.Value = true;
        }

        public void ResetDamageCircle()
        {
            if (!IsServer) { Debug.LogError("DamageCircle.ResetDamageCircle() should only be called on the server"); return; }

            targetScale.Value = PlayerDataManager.Singleton.GetDamageCircleMaxScale();
            transform.localScale = PlayerDataManager.Singleton.GetDamageCircleMaxScale();

            shouldRender.Value = false;
        }

        public bool IsPointInsideDamageCircleBounds(Vector3 point)
        {
            return Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(point.x, point.z)) < targetScale.Value.x;
        }

        public override void OnNetworkSpawn()
        {
            shouldRender.OnValueChanged += OnShouldRenderChanged;
            if (IsServer)
            {
                transform.localScale = PlayerDataManager.Singleton.GetDamageCircleMaxScale();
                targetScale.Value = transform.localScale;
            }
        }

        public override void OnNetworkDespawn()
        {
            shouldRender.OnValueChanged -= OnShouldRenderChanged;
        }

        private void OnShouldRenderChanged(bool prev, bool current)
        {
            foreach (Renderer r in renderers)
            {
                r.enabled = current;
            }
        }

        private CapsuleCollider capsuleCollider;
        private Renderer[] renderers;
        private void Awake()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }
        }

        private NetworkVariable<Vector3> targetScale = new NetworkVariable<Vector3>();
        private NetworkVariable<bool> shouldRender = new NetworkVariable<bool>();
        private void Update()
        {
            if (!GameModeManager.Singleton) { Debug.LogError("Damage circle should only be present when there is a game mode manager!"); return; }
            if (!IsServer) { return; }
            
            if (canShrink) { transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale.Value, Time.deltaTime * shrinkSpeed); }

            if (GameModeManager.Singleton.ShouldDisplayNextGameAction() | GameModeManager.Singleton.IsGameOver()) { return; }

            Vector3 bottom = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y - (capsuleCollider.height * transform.localScale.y / 2), capsuleCollider.center.z);
            Vector3 top = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y + (capsuleCollider.height * transform.localScale.y / 2), capsuleCollider.center.z);
            Collider[] collidersInside = Physics.OverlapCapsule(bottom, top, capsuleCollider.radius * transform.localScale.x, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

            List<CombatAgent> attributesInside = new List<CombatAgent>();
            foreach (Collider col in collidersInside)
            {
                if (col.TryGetComponent(out NetworkCollider networkCollider)) { attributesInside.Add(networkCollider.CombatAgent); }
            }

            foreach (CombatAgent attributes in PlayerDataManager.Singleton.GetActiveCombatAgents())
            {
                if (!attributesInside.Contains(attributes)) { attributes.ProcessEnvironmentDamage(Time.deltaTime * -healthDeductionRate, NetworkObject); }
            }
        }
    }
}