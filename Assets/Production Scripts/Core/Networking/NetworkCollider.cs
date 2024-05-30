using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace Vi.Core
{
    public class NetworkCollider : MonoBehaviour
    {
        public Attributes Attributes { get; private set; }
        public MovementHandler MovementHandler { get; private set; }

        private static Dictionary<int, NetworkCollider> instanceIDTable = new Dictionary<int, NetworkCollider>();

        private Collider[] colliders;
        private void Awake()
        {
            MovementHandler = GetComponentInParent<MovementHandler>();
            Attributes = GetComponentInParent<Attributes>();
            colliders = GetComponentsInChildren<Collider>();

            foreach (Collider c in colliders)
            {
                instanceIDTable.Add(c.GetInstanceID(), this);
                c.hasModifiableContacts = true;
            }
        }

        private void OnDestroy()
        {
            foreach (Collider c in colliders)
            {
                instanceIDTable.Remove(c.GetInstanceID());
            }
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

        //private void OnEnable()
        //{
        //    Physics.ContactModifyEvent += ModificationEvent;
        //}

        //public void OnDisable()
        //{
        //    Physics.ContactModifyEvent -= ModificationEvent;
        //}

        //public void ModificationEvent(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
        //{
        //    // For each contact pair, ignore the contact points that are close to origin
        //    foreach (var pair in pairs)
        //    {
        //        if (instanceIDTable.ContainsKey(pair.colliderInstanceID))
        //        {
        //            Debug.Log(instanceIDTable[pair.colliderInstanceID]);
        //            //Debug.Log("First " + instanceIDTable[pair.colliderInstanceID].ToString());
        //        }

        //        //if (instanceIDTable.ContainsKey(pair.otherColliderInstanceID))
        //        //{
        //        //    Debug.Log("Second " + instanceIDTable[pair.otherColliderInstanceID].ToString());
        //        //}

        //        for (int i = 0; i < pair.contactCount; ++i)
        //        {

        //        }
        //    }
        //}

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