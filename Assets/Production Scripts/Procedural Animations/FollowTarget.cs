using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ProceduralAnimations
{
    public class FollowTarget : MonoBehaviour
    {
        [SerializeField] private bool move = true;
        [SerializeField] private bool rotate = true;
        [SerializeField] private bool lerp;
        [SerializeField] private float lerpSpeed = 5;

        public Transform target;

        private Rigidbody rb;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!rb) { return; }
            if (!rb.isKinematic)
            {
                Debug.LogWarning("Dynamic rigidbody is unsupported " + name + " " + transform.root.name);
                return;
            }

            if (target)
            {
                if (lerp)
                {
                    if (move) { rb.MovePosition(Vector3.MoveTowards(rb.position, target.position, lerpSpeed * Time.deltaTime)); }
                    if (rotate) { rb.MoveRotation(Quaternion.Slerp(rb.rotation, target.rotation, lerpSpeed * Time.deltaTime)); }
                }
                else
                {
                    if (move) { rb.MovePosition(target.position); }
                    if (rotate) { rb.MoveRotation(target.rotation); }
                }
            }
        }

        private void LateUpdate()
        {
            if (rb) { return; }

            if (target)
            {
                if (lerp)
                {
                    if (move) { transform.position = Vector3.MoveTowards(transform.position, target.position, lerpSpeed * Time.deltaTime); }
                    if (rotate) { transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, lerpSpeed * Time.deltaTime); }
                }
                else
                {
                    if (move) { transform.position = target.position; }
                    if (rotate) { transform.rotation = target.rotation; }
                }
            }
        }
    }
}