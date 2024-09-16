using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core.MovementHandlers;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkCollider : MonoBehaviour
    {
        public CombatAgent CombatAgent { get; private set; }
        public PhysicsMovementHandler MovementHandler { get; private set; }
        public Collider[] Colliders { get; private set; }

        private void Awake()
        {
            MovementHandler = GetComponentInParent<PhysicsMovementHandler>();
            CombatAgent = GetComponentInParent<CombatAgent>();
            CombatAgent.SetNetworkCollider(this);
            Colliders = GetComponentsInChildren<Collider>();

            List<Collider> networkPredictionLayerColliders = new List<Collider>();
            foreach (Collider col in Colliders)
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("NetworkPrediction"))
                {
                    networkPredictionLayerColliders.Add(col);
                }
            }
            Colliders = networkPredictionLayerColliders.ToArray();
        }

        private ActionClip.Ailment lastAilmentEvaluated;
        private void Update()
        {
            if (CombatAgent.GetAilment() != lastAilmentEvaluated)
            {
                foreach (Collider c in Colliders)
                {
                    c.enabled = CombatAgent.GetAilment() != ActionClip.Ailment.Death;
                }
            }
            lastAilmentEvaluated = CombatAgent.GetAilment();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!MovementHandler) { return; }
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionEnterMessage(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!MovementHandler) { return; }
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionStayMessage(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!MovementHandler) { return; }
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionExitMessage(collision);
        }

        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.green;
        //    if (Application.isPlaying) { Gizmos.color = CombatAgent.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death ? Color.red : Color.green; }

        //    CapsuleCollider capsuleCollider = GetComponentInChildren<CapsuleCollider>();
        //    ExtDebug.DrawWireCapsule(capsuleCollider.transform.position + capsuleCollider.transform.TransformPoint(capsuleCollider.center) - capsuleCollider.transform.up * (capsuleCollider.height / 2),
        //        capsuleCollider.transform.position + capsuleCollider.transform.TransformPoint(capsuleCollider.center) + capsuleCollider.transform.up * (capsuleCollider.height / 2),
        //        capsuleCollider.radius);
        //}
    }
}