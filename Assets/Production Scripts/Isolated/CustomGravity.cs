using UnityEngine;
using Unity.Netcode;

namespace Vi.Isolated
{
    [RequireComponent(typeof(Rigidbody))]
    public class CustomGravity : MonoBehaviour
    {
        [SerializeField] private float gravityScale = 1;

        private Rigidbody rb;
        private NetworkObject networkObject;

        private void Awake()
        {
            networkObject = GetComponentInParent<NetworkObject>();
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
        }

        void FixedUpdate()
        {
            if (!networkObject.IsSpawned) { return; }
            Vector3 gravity = Physics.gravity * gravityScale;
            rb.AddForce(gravity, ForceMode.Acceleration);
        }
    }
}