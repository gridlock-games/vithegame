using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ProceduralAnimations;

namespace Vi.Core
{
    public class LimbReferences : MonoBehaviour
    {
        [Header("Character Bound Settings")]
        public float characterHeight = 1.5f;
        public float characterRadius = 0.5f;
        public Vector3 bottomPointOfCapsuleOffset = new Vector3(0, 0.5f, 0);

        public enum Hand
        {
            RightHand,
            LeftHand
        }

        public bool IsAiming(Hand hand) { return aimingDictionary[hand]; }

        private Dictionary<Hand, bool> aimingDictionary = new Dictionary<Hand, bool>();
        public void AimHand(Hand hand, bool isAiming, bool instantAim)
        {
            float weight = isAiming ? 1 : 0;
            if (hand == Hand.RightHand)
            {
                if (!rightHandAimRig.GetRig()) { return; }
                rightHandAimRig.weight = weight;
                if (instantAim) { rightHandAimRig.GetRig().weight = weight; }
            }
            else if (hand == Hand.LeftHand)
            {
                if (!leftHandAimRig.GetRig()) { return; }
                leftHandAimRig.weight = weight;
                if (instantAim) { leftHandAimRig.GetRig().weight = weight; }
            }
            animator.SetBool("Aiming", isAiming);
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), weight);
            StartCoroutine(SetHandAimingBool(hand, isAiming));
        }

        private IEnumerator SetHandAimingBool(Hand hand, bool isAiming)
        {
            yield return null;
            aimingDictionary[hand] = isAiming;
        }

        public void ReachHand(Hand hand, Transform reachTarget, bool isReaching, bool instantReach)
        {
            float weight = isReaching ? 1 : 0;
            if (hand == Hand.RightHand)
            {
                if (!rightHandReachRig.GetRig()) { return; }
                rightHandReachRig.weight = weight;
                RightHandFollowTarget.target = reachTarget;
                if (instantReach) { rightHandReachRig.GetRig().weight = weight; }
            }
            else if (hand == Hand.LeftHand)
            {
                if (!leftHandReachRig.GetRig()) { return; }
                leftHandReachRig.weight = weight;
                LeftHandFollowTarget.target = reachTarget;
                if (instantReach) { leftHandReachRig.GetRig().weight = weight; }
            }
        }

        private Animator animator;
        public FollowTarget RightHandFollowTarget { get; private set; }
        public FollowTarget LeftHandFollowTarget { get; private set; }

        private void Start()
        {
            animator = GetComponent<Animator>();

            if (rightHandReachRig) { RightHandFollowTarget = rightHandReachRig.GetComponentInChildren<FollowTarget>(); }
            if (leftHandReachRig) { LeftHandFollowTarget = leftHandReachRig.GetComponentInChildren<FollowTarget>(); }

            aimingDictionary.Add(Hand.RightHand, false);
            aimingDictionary.Add(Hand.LeftHand, false);

            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.gameObject.AddComponent<CollisionListener>();
            }
        }

        public RigWeightTarget GetRightHandReachRig() { return rightHandReachRig; }
        public RigWeightTarget GetLeftHandReachRig() { return leftHandReachRig; }

        [Header("IK Settings")]
        public AimTargetIKSolver aimTargetIKSolver;
        [SerializeField] private RigWeightTarget rightHandAimRig;
        [SerializeField] private RigWeightTarget leftHandAimRig;
        [SerializeField] private RigWeightTarget rightHandReachRig;
        [SerializeField] private RigWeightTarget leftHandReachRig;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            DrawWireCapsule(transform.position + transform.rotation * bottomPointOfCapsuleOffset, transform.position + transform.up * characterHeight, characterRadius);
        }

        public static void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
        {
            #if UNITY_EDITOR
            // Special case when both points are in the same position
            if (p1 == p2)
            {
                // DrawWireSphere works only in gizmo methods
                Gizmos.DrawWireSphere(p1, radius);
                return;
            }
            using (new UnityEditor.Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                Quaternion p1Rotation = Quaternion.LookRotation(p1 - p2);
                Quaternion p2Rotation = Quaternion.LookRotation(p2 - p1);
                // Check if capsule direction is collinear to Vector.up
                float c = Vector3.Dot((p1 - p2).normalized, Vector3.up);
                if (c == 1f || c == -1f)
                {
                    // Fix rotation
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
                }
                // First side
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.left, p1Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.up, p1Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
                // Second side
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.left, p2Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.up, p2Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);
                // Lines
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.down * radius, p2 + p2Rotation * Vector3.down * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.left * radius, p2 + p2Rotation * Vector3.right * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.up * radius, p2 + p2Rotation * Vector3.up * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.right * radius, p2 + p2Rotation * Vector3.left * radius);
            }
            #endif
        }
    }
}