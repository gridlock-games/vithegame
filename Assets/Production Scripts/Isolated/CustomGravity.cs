using UnityEngine;

namespace Vi.Isolated
{
    [RequireComponent(typeof(Rigidbody))]
    public class CustomGravity : MonoBehaviour
    {
        [SerializeField] private float gravityScale = 1;

        Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
        }

        void FixedUpdate()
        {
            Vector3 gravity = Physics.gravity * gravityScale;
            rb.AddForce(gravity, ForceMode.Acceleration);
        }
    }
}