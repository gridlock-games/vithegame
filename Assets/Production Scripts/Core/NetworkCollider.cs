using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace Vi.Core
{
    public class NetworkCollider : MonoBehaviour
    {
        public CombatAgent CombatAgent { get; private set; }
        public MovementHandler MovementHandler { get; private set; }
        public Collider[] Colliders { get; private set; }

        private void Awake()
        {
            MovementHandler = GetComponentInParent<MovementHandler>();
            CombatAgent = GetComponentInParent<CombatAgent>();
            CombatAgent.SetNetworkCollider(this);
            Colliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            foreach (Collider c in Colliders)
            {
                c.enabled = CombatAgent.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionEnterMessage(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionStayMessage(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
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