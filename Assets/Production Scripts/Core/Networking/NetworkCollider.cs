using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class NetworkCollider : MonoBehaviour
    {
        public Attributes Attributes { get; private set; }
        public MovementHandler MovementHandler { get; private set; }

        private Collider[] colliders;
        private void Awake()
        {
            MovementHandler = GetComponentInParent<MovementHandler>();
            Attributes = GetComponentInParent<Attributes>();
            colliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            foreach (Collider c in colliders)
            {
                c.enabled = Attributes.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death;
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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            if (Application.isPlaying) { Gizmos.color = Attributes.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death ? Color.red : Color.green; }

            ExtDebug.DrawWireCapsule(transform.position + GetComponent<CapsuleCollider>().center - new Vector3(0, GetComponent<CapsuleCollider>().height / 8, 0),
                transform.position + GetComponent<CapsuleCollider>().center + new Vector3(0, GetComponent<CapsuleCollider>().height / 8, 0),
                GetComponent<CapsuleCollider>().radius);
        }
    }
}