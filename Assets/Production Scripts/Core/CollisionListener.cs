using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class CollisionListener : MonoBehaviour
    {
        private MovementHandler movementHandler;
        private void Awake()
        {
            movementHandler = GetComponentInParent<MovementHandler>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.root == transform.root) { return; }
            movementHandler.ReceiveOnCollisionEnterMessage(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.transform.root == transform.root) { return; }
            movementHandler.ReceiveOnCollisionStayMessage(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.transform.root == transform.root) { return; }
            movementHandler.ReceiveOnCollisionExitMessage(collision);
        }
    }
}